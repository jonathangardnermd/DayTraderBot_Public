namespace DayTradeBot.domains.appOrchestration;

using DayTradeBot.contracts.application;
using DayTradeBot.common.constants;
using DayTradeBot.common.settings;
using DayTradeBot.common.util.exceptionUtil;
using DayTradeBot.common.util.loggingUtil;
using DayTradeBot.common.util.managedResourceUtil;
using DayTradeBot.domains.app;
using DayTradeBot.domains.tradeEngine.core;

/*
The AppRunner has a single public method (defined in the IApplicationRunner interface): Run().

You pass an instantiated App into AppRunner.Run(app), and it will 
1) subscribe to events published by the trade engine as it processes throughout the day
2) persist the data from the tradeEngine at the end of each day
3) catch and log any exceptions that occur
4) properly dispose of all resources if no exceptions occur (otherwise, Program.cs should dispose of them)

The subscribed functions mentioned in #1 above predominately perform validation and logging. To help with 
these tasks, there are the associated AppValidator and the AppReporter classes with many useful static methods. 
*/
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
#pragma warning disable CS1998 // method lacks await operators...
#pragma warning disable CS8602 // Dereference of a possibly null reference
public abstract class AppRunnerBase : IAppRunner
{
    protected App App { get; set; }
    protected float FreeUsdBalance { get; set; } = 100 * 1000f;
    protected OrderActions OrderActions { get; set; } = new OrderActions();
    protected PositionRenewalActions PositionRenewalActions { get; set; } = new PositionRenewalActions();
    protected Dictionary<string, Position> PersistedPositionsBySymbol { get; set; }
    protected float? MinFreeUsdBalance { get; set; } = null;

    public AppRunnerBase()
    {
        if (AppSettings.RuntimeEnv == null)
        {
            throw new Exception("Must set a runtime environment in AppSettings before running the app.");
        }
    }

    public async Task Run(App app)
    {
        App = app;

        /*
        These subscriptions allow us to hook into the tradeEngine at crucial points during trading. 

        For example, any time an OrderAction occurs (e.g. an order is placed or filled or cancelled, etc), 
        the OnOrderAction() function is called. Since App.Engine is publicly available from this AppRunner, 
        we essentially have access to SYNCHRONOUSLY perform any extra work we want at these points in the code. 

        For example, we could look for certain conditions to be met and then cancel all orders and place a 
        market sell for our current holdings, all from this AppRunner. However, these hooks are probably most 
        useful for validation, logging, and debugging purposes (e.g. these hooks are great places for a breakpoint :)
        */
        app.Subscribe(SubscribableTopic.AfterStartOfDayLoad, this.AfterStartOfDayLoad);
        app.Subscribe(SubscribableTopic.BeforeEndOfDaySave, this.BeforeEndOfDaySave);
        app.Subscribe(SubscribableTopic.OrderAction, this.OnOrderAction);
        app.Subscribe(SubscribableTopic.PositionRenewalAction, this.OnPositionRenewalAction);
        app.Subscribe(SubscribableTopic.EndOfPriceUpdateRound, this.OnEndOfPriceUpdateRound);
        app.Subscribe(SubscribableTopic.AccountBalanceUpdate, this.OnAccountBalanceUpdate);

        // this allows us to define any other methods in our derived AppRunner class and subscribe them separately.
        this.SubscribeOptional(app);

        // Run the trader for a day!
        await app.RunAsync();

        // Among other tasks, data persistence occurs here
        await app.PerformEndOfDayTasks();

        if (ExceptionUtil.HasThrownExceptions())
        {
            ExceptionUtil.LogExceptions();
            return;
        }
        ManagedResourceUtil.DisposeAll();
    }

    protected abstract void SubscribeOptional(App app);

    protected virtual async Task AfterStartOfDayLoad(object? obj)
    {
        // compare PersistedPositions to the positions that were loaded
        var loadedPositionsBySymbol = App.Engine.Positions.ToDictionary();
        if (PersistedPositionsBySymbol != null)
        {
            foreach (var symbol in PersistedPositionsBySymbol.Keys)
            {
                if (!loadedPositionsBySymbol.ContainsKey(symbol))
                {
                    throw new Exception($"Position for symbol {symbol} failed to load");
                }
                var persistedPos = PersistedPositionsBySymbol[symbol];
                var loadedPos = loadedPositionsBySymbol[symbol];
            }
        }
        App.Engine.Positions.Sort();

        if (AppSettings.IsLogTypeOn(LogType.Debug))
        {
            AppReporter.LogTableOfPositions(GetPositions(), LogType.Debug);
            foreach (var pos in GetPositions())
            {
                AppReporter.LogTableOfAllPrimaryOrders(pos, LogType.Debug);
            }
        }
    }

    protected virtual async Task BeforeEndOfDaySave(object? obj)
    {
        PersistedPositionsBySymbol = App.Engine.Positions.ToDictionary();

        // the current positions from App.Engine will not include any of these "old" positions, so we will need to filter them out
        var oldPositionGuids = PositionRenewalActions.GetGuidLookupDict();

        if (AppSettings.IsLogTypeOn(LogType.DailySummary))
        {
            LoggingUtil.LogTo(LogType.DailySummary, $"Primary order groups:\n");
            foreach (var pos in GetPositions())
            {
                var strToLog = $"{AppReporter.GetTableOfPrimaryOrderGroups(pos).ToString()}\n";
                LoggingUtil.LogTo(LogType.DailySummary, strToLog);
            }
            LoggingUtil.LogTo(LogType.DailySummary, "\n---------------------------------------------\n");
        }

        // check if PersistedPositions are consistent with OrderActions
        if (!AppValidator.IsSelfConsistentData(this.OrderActions.GetOrderActionsSortedBySeqNum(), this.PersistedPositionsBySymbol, oldPositionGuids))
        {
            throw new Exception("why not self-consistent? How are orderActions and positions out-of-whack with one-another?");
        }
    }

    protected virtual async Task OnAccountBalanceUpdate(object? obj)
    {
        if (obj is not AccountBalanceUpdate)
        {
            throw new Exception("Object not of the correct type");
        }
        var accountBalanceUpdate = (AccountBalanceUpdate)obj;

        FreeUsdBalance = accountBalanceUpdate.FreeUsdBalance;
        if (MinFreeUsdBalance == null || FreeUsdBalance < MinFreeUsdBalance)
        {
            MinFreeUsdBalance = FreeUsdBalance;
        }
    }

    protected virtual async Task OnOrderAction(object? obj)
    {
        if (obj is not OrderAction)
        {
            throw new Exception("Object not of the correct type");
        }

        var orderAction = (OrderAction)obj;
        OrderActions.Add(orderAction);

        if (AppSettings.IsLogTypeOn(LogType.OrderAction))
        {
            LoggingUtil.LogTo(LogType.OrderAction, orderAction.ToLogRow());
        }
    }

    protected virtual async Task OnPositionRenewalAction(object? obj)
    {
        if (obj is not PositionRenewalAction)
        {
            throw new Exception("Object not of the correct type");
        }

        var positionRenewal = (PositionRenewalAction)obj;
        PositionRenewalActions.Add(positionRenewal);

        if (AppSettings.IsLogTypeOn(LogType.PositionRenewalAction))
        {
            LoggingUtil.LogTo(LogType.PositionRenewalAction, positionRenewal.ToLogRow());
        }
    }

    protected virtual async Task OnEndOfPriceUpdateRound(object? obj)
    {
        if (obj is not PriceUpdateRound)
        {
            throw new Exception("Object not of the correct type");
        }
        var priceUpdateRound = (PriceUpdateRound)obj;

        if (AppSettings.IsLogTypeOn(LogType.PriceUpdateRound))
        {
            LoggingUtil.LogTo(LogType.PriceUpdateRound, priceUpdateRound.ToLogRow());
        }

        if (AppSettings.IsLogTypeOn(LogType.Debug))
        {
            AppReporter.LogTableOfPriceUpdateRound(priceUpdateRound, LogType.Debug);
            AppReporter.LogTableOfPositions(GetPositions(), LogType.Debug);
            AppReporter.LogTableOfModifiedOrdersForPriceRound(priceUpdateRound, LogType.Debug);

            foreach (var pos in GetPositions())
            {
                AppReporter.LogTableOfAllPrimaryOrders(pos, LogType.Debug);
            }
        }

        if (AppSettings.IsLogTypeOn(LogType.OrderChanges))
        {
            var modifiedOrders = priceUpdateRound.GetAllModifiedOrders();
            if (modifiedOrders.Count() > 0)
            {
                AppReporter.LogTableOfPriceUpdateRound(priceUpdateRound, LogType.OrderChanges);
                AppReporter.LogTableOfPositions(GetPositions(), LogType.OrderChanges);
                AppReporter.LogTableOfModifiedOrdersForPriceRound(priceUpdateRound, LogType.OrderChanges);
                foreach (var pos in GetPositions())
                {
                    AppReporter.LogTableOfAllPrimaryOrders(pos, LogType.OrderChanges);
                }

                LoggingUtil.LogTo(LogType.OrderChanges, $"Primary order groups:\n");
                foreach (var pos in GetPositions())
                {
                    var strToLog = $"{AppReporter.GetTableOfPrimaryOrderGroups(pos).ToString()}\n";
                    LoggingUtil.LogTo(LogType.OrderChanges, strToLog);
                }
                LoggingUtil.LogTo(LogType.OrderChanges, "\n---------------------------------------------\n");
            }
        }

        AppValidator.ValidatePriceUpdateRound(App.Engine, priceUpdateRound);
    }

    private IEnumerable<Position> GetPositions()
    {
        return App.Engine.Positions.GetEnumerable().OrderBy(pos => pos.Symbol);
    }
}


