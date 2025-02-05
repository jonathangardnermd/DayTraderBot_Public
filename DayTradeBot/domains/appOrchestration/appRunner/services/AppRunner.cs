namespace DayTradeBot.domains.appOrchestration;

using DayTradeBot.contracts.externalApis.brokerageApi;
using DayTradeBot.contracts.externalApis.marketDataApi;
using DayTradeBot.common.constants;
using DayTradeBot.domains.externalApis.brokerageApi;
using DayTradeBot.domains.externalApis.marketDataApi;
using DayTradeBot.domains.tradeEngine.algo;
using DayTradeBot.domains.app;

/*
The AppRunner sits on top of the App itself, and this allows for things like multi-day simulations.

That is, since the App itself was designed to run for one trading day at a time, the AppRunner is necessary 
to handle any multi-day functionality. Importantly, the AppRunner handles persistence of the trading data 
at the end of each trading day (because persistence is only necessary for multi-day scenarios, and therefore 
the single-day App shouldn't bother itself with such things as persistence.)

This AppRunner is a PROD-specific derived class and hence only runs the App for a single day. 

The Test project has a TEST-specific derived class which actually runs multi-day simulations. 
*/
#pragma warning disable CS1998
public class AppRunner : AppRunnerBase
{
    public AppRunner() : base()
    {
    }
    public async Task RunRealDayTrader(List<string> symbolsToTrade)
    {
        var app = CreateNewApplicationService(symbolsToTrade);
        await this.Run(app);
    }

    public static App CreateNewApplicationService(List<string> symbolsToTrade)
    {
        IBrokerageApiService brokerageApi = new BrokerageApiService();
        IMarketDataApiService marketDataApi = new MarketDataApiService();
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);

        var algoParams = AlgoParamsGenerator.CreateData_Version_Bull();
        algoParams.PrintAlgorithmToLog(LogType.SimulationResults);

        var algoParamsBySymbol = symbolsToTrade.ToDictionary(symbol => symbol, symbol => algoParams);
        var engine = new TradeEngineAlgo(marketDataApi, brokerageApi, symbolsToTrade, today, algoParamsBySymbol);
        var app = new App(engine);
        return app;
    }

    protected override void SubscribeOptional(App app)
    {
    }
}