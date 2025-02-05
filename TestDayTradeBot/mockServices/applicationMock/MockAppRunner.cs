namespace TestTradeBot.mockServices.applicationMock;

using ConsoleTables;

using DayTradeBot.common.constants;
using DayTradeBot.common.settings;
using DayTradeBot.common.util.exceptionUtil;
using DayTradeBot.common.util.loggingUtil;
using DayTradeBot.domains.app;
using DayTradeBot.domains.appOrchestration;
using DayTradeBot.domains.externalApis.brokerageApi;
using DayTradeBot.domains.tradeEngine.algo;
using DayTradeBot.domains.tradeEngine.core;

using TestTradeBot.mockServices.externalApiMocks.brokerageApiMock;
using TestTradeBot.mockServices.mockDataServices.common;

#pragma warning disable CS1998
public class MockAppRunner : AppRunnerBase
{
    private DateOnly CurrDte { get; set; }
    private DateOnly LastTradingDte { get; set; }
    private int ForceSellCt { get; set; } = 0;
    public MockAppRunner() : base()
    {
    }

    public MockBrokerageApiService GetMockBrokerageApi()
    {
        var api = this.App.Engine.BrokerageApi;
        return (MockBrokerageApiService)api;
    }
    public async Task RunMockMultiDaySimulation(List<string> symbolsToTrade, string dataset, DateOnly startDte, DateOnly endDte, Dictionary<string, AlgoParams> algoParamsBySymbol)
    {
        CurrDte = startDte;
        var appFactory = new MockAppFactory(dataset, symbolsToTrade);

        LastTradingDte = appFactory.GetLastTradingDteBeforeOrEqualTo(endDte);
        while (CurrDte <= endDte)
        {
            var app = appFactory.CreateNewApplicationService(CurrDte, FreeUsdBalance, algoParamsBySymbol);

            if (app != null)
            {
                // This line runs the trader for a day (CurrentDte)
                await this.Run(app);
            }

            CurrDte = CurrDte.AddDays(1);

            if (ExceptionUtil.HasThrownExceptions())
            {
                ExceptionUtil.LogExceptions();
                return;
            }
        }

        if (ForceSellCt != 1)
        {
            throw new Exception("Force sell ct is not equal to one.");
        }
        await OnEndOfSimulation(appFactory, startDte, endDte);
    }

    protected override async Task BeforeEndOfDaySave(object? obj)
    {

        bool shouldForceSell = CurrDte == LastTradingDte;

        if (shouldForceSell)
        {
            LoggingUtil.LogTo(LogType.Main, $"\nForce selling all open sells!");
            await App.Engine.FakeForceCompleteAllOpenSells();
            ForceSellCt++;
        }

        await base.BeforeEndOfDaySave(obj);
    }

    private async Task OnAccountBalanceUpdateMock(object? obj)
    {
        if (obj is not AccountBalanceUpdate)
        {
            throw new Exception("Object not of the correct type");
        }
        var accountBalanceUpdate = (AccountBalanceUpdate)obj;

        var acctData = new AccountData
        {
            FreeUsdBalance = accountBalanceUpdate.FreeUsdBalance
        };

        var brokerageApi = GetMockBrokerageApi();
        brokerageApi.SetAccountData(acctData);
    }

    private async Task OnImmediateFillBuyPlacement(object? obj)
    {
        if (obj is not BuyOrder)
        {
            throw new Exception("Object not of the correct type");
        }
        var order = (BuyOrder)obj;
        var brokerageApi = GetMockBrokerageApi();
        brokerageApi.SetFilledPrice(order, order.Position.Fi.CurrentBidPrice);
    }

    protected override async Task OnEndOfPriceUpdateRound(object? obj)
    {
        if (obj is not PriceUpdateRound)
        {
            throw new Exception("Object not of the correct type");
        }
        var priceUpdateRound = (PriceUpdateRound)obj;
        await base.OnEndOfPriceUpdateRound(obj);

        /*
        This is an example of how we can hook into events in the trade engine and extend the default event handler WITHOUT MODIFYING THE EXISTING CODE. 
        So it's an extra-safe way to add logging, add validation, or add some sleep time during complex debug sessions so that you can manually inspect 
        the logging while the engine is running. 

        You can do more than that, because you can directly edit App.Engine (e.g. change the BasisPrice on a Position), but I wouldn't recommend it 
        unless you're sure you want to open that can of worms.
        */
        if (priceUpdateRound.DidPlaceBreakEvenSell)
        {
            if (AppSettings.IsLogTypeOn(LogType.Debug))
            {
                LoggingUtil.LogTo(LogType.Debug, "BREAK EVEN");
            }
        }
    }

    protected async Task OnEndOfSimulation(MockAppFactory appFactory, DateOnly startDte, DateOnly endDte)
    {
        var (startClosePriceEntries, endClosePriceEntries) = appFactory.GetStartAndEndClosePriceEntries(startDte, endDte);

        if (AppSettings.IsLogTypeOn(LogType.SimulationResults))
        {
            var filledOrderAggregations = AppReporter.CalcFilledOrderData(OrderActions);
            var runSummaryTable = GetTableOfPositionRunSummaries(startClosePriceEntries, endClosePriceEntries, filledOrderAggregations, PositionRenewalActions);
            var totProfit = AppReporter.CalcTotalProfit(filledOrderAggregations);
            var totProfitStr = LoggingUtil.UsdToString(totProfit);
            var minFreeStr = LoggingUtil.UsdToString(MinFreeUsdBalance);

            LoggingUtil.LogTo(LogType.SimulationResults, $"Position Run Summaries:\n{runSummaryTable.ToString()}");
            LoggingUtil.LogTo(LogType.SimulationResults, $"Total Profit: {totProfitStr}");
            LoggingUtil.LogTo(LogType.SimulationResults, $"Min Free USD: {minFreeStr}");
            LoggingUtil.LogTo(LogType.SimulationResults, $"Num Zero Quantity: {OrderActions.NumZeroQuantityOrders}");
        }

        AppValidator.ValidateFinalOrderActions(OrderActions);
    }
    protected override void SubscribeOptional(App app)
    {
        app.Subscribe(SubscribableTopic.AccountBalanceUpdate, this.OnAccountBalanceUpdateMock);
        app.Subscribe(SubscribableTopic.ImmediateFillBuyPlacement, this.OnImmediateFillBuyPlacement);
    }

    public static ConsoleTable GetTableOfPositionRunSummaries(
        Dictionary<string, ClosePriceEntry> startClosePriceEntries, Dictionary<string, ClosePriceEntry> endClosePriceEntries
        , FilledOrderAggregations filledOrderAggregations, PositionRenewalActions allPositionRenewalActions)
    {
        var runSummaries = new List<PositionRunSummary>();
        var positionRenewalsBySymbol = allPositionRenewalActions.GetData();
        var filledOrderData = filledOrderAggregations.GetData();
        foreach (var symbol in filledOrderAggregations.GetSymbols())
        {
            // populate filled order metrics
            var aggsForSymbol = filledOrderData[symbol];
            var filledBuyData = aggsForSymbol[OrderDirection.Buy];
            var filledSellData = aggsForSymbol[OrderDirection.Sell];

            var (buyQty, numBuysFilled, buyAmt) = (filledBuyData.FilledQty, filledBuyData.NumOrdersFilledCt, filledBuyData.FilledUsdAmt);
            var (sellQty, numSellsFilled, sellAmt) = (filledSellData.FilledQty, filledSellData.NumOrdersFilledCt, filledSellData.FilledUsdAmt);


            // populate position renewal metrics
            var positionRenewals = positionRenewalsBySymbol[symbol];
            var numPositionRefreshes = positionRenewals.Where(pa => pa.Type == PositionRenewalAction.ActionType.Refresh).Count();
            var numPositionRenewals = positionRenewals.Where(pa => pa.Type == PositionRenewalAction.ActionType.Renewal).Count();

            var profit = sellAmt - buyAmt;

            runSummaries.Add(new PositionRunSummary
            {
                Symbol = symbol,
                NumPositionRefreshs = numPositionRefreshes,
                NumPositionRenewals = numPositionRenewals,
                NumBuysFilled = numBuysFilled,
                NumSellsFilled = numSellsFilled,
                BuyQty = buyQty,
                SellQty = sellQty,
                BuyAmt = (float)buyAmt,
                SellAmt = (float)sellAmt,
                Profit = profit,
                StartPrice = startClosePriceEntries[symbol].ClosePrice,
                EndPrice = endClosePriceEntries[symbol].ClosePrice
            });
        }

        return PositionRunSummary.GetTable(PositionRunSummary.GetColHeaders(), runSummaries);
    }
}