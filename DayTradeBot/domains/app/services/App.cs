namespace DayTradeBot.domains.app;

using System.Diagnostics;
using System.Collections.Concurrent;

using DayTradeBot.common.constants;
using DayTradeBot.common.settings;
using DayTradeBot.common.util.managedResourceUtil;
using DayTradeBot.common.util.loggingUtil;
using DayTradeBot.common.util.exceptionUtil;
using DayTradeBot.domains.externalApis.marketDataApi;
using DayTradeBot.domains.tradeEngine.core;

public class App
{
    public TradeEngineBase Engine { get; set; }

    public DateOnly Today { get; set; }

    private ConcurrentQueue<Exception> exceptionQueue = new ConcurrentQueue<Exception>();
    public App(TradeEngineBase engine)
    {
        Engine = engine;
    }

    public async Task RunAsync()
    {
        // time the application for logging purposes
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        if (AppSettings.IsLogTypeOn(LogType.Debug))
        {
            LoggingUtil.LogTo(LogType.Debug, "\nRunning the application.\n");
        }

        // create disposable objects and inform ManagedResourceUtil of them to ensure proper disposal
        var socketListener = new SequentialSocketListener<PriceUpdateSocketData>();
        var priceUpdateSocket = Engine.MarketDataApi.PriceUpdateSocket;
        ManagedResourceUtil.AddDisposable(socketListener);
        ManagedResourceUtil.AddDisposable(priceUpdateSocket);
        try
        {
            // perform start-of-day tasks like loading previously-persisted data
            await Engine.PerformStartOfDayTasks();

            // subscribe the OnPriceUpdate function of the TradeEngine to the squentialSocketListener for one-at-a-time price updates
            socketListener.Subscribe(Engine.OnPriceUpdate);

            // connect and subscribe for price updates from the MarketDataApi's PriceUpdateSocket
            await priceUpdateSocket.Connect();
            await priceUpdateSocket.SubscribeForSymbols(Engine.SymbolsToTrade, socketListener.EnqueueUpdate);

            /*
            Check if it's time to stop for the day, either because 
            1) the socket is done sending updates (e.g. when using mockData)
            OR
            2) the markets have closed and it's time to shutdown until tomorrow
            */
            bool morePriceUpdatesToProcess = true;
            bool shouldStopAppDueToTheTime = (AppSettings.StopAppAfterUtcTime != null) && (DateTime.UtcNow.TimeOfDay > AppSettings.StopAppAfterUtcTime);
            while (morePriceUpdatesToProcess && !shouldStopAppDueToTheTime)
            {
                if (ExceptionUtil.HasThrownExceptions())
                {
                    if (AppSettings.IsLogTypeOn(LogType.Debug))
                    {
                        LoggingUtil.LogTo(LogType.Debug, $"Stopping execution due to errors in the exceptionQueue...");
                    }
                    return;
                }
                await Task.Delay(1000);

                bool socketIsNotDone = !priceUpdateSocket.IsDisposed();
                bool listenerIsNotDone = !socketListener.IsEmptyQueue();
                morePriceUpdatesToProcess = socketIsNotDone || listenerIsNotDone;
            }
            stopwatch.Stop();
        }
        catch (Exception e)
        {
            ExceptionUtil.AddException(e);
        }

        LoggingUtil.LogTo(LogType.Main, $"Done running the application after {stopwatch.Elapsed.TotalSeconds} seconds.");
    }

    public async Task PerformEndOfDayTasks()
    {
        await Engine.PerformEndOfDayTasks();
    }

    public void Subscribe(SubscribableTopic topic, Func<object?, Task> handler)
    {
        this.Engine.Subscribe(topic, handler);
    }

}

