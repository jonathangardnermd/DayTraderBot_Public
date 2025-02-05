using DayTradeBot.domains.appOrchestration;
using DayTradeBot.domains.tradeEngine.algo;
using DayTradeBot.common.settings;
using DayTradeBot.common.constants;
using DayTradeBot.common.util.managedResourceUtil;
using DayTradeBot.common.util.exceptionUtil;

using TestTradeBot.mockServices.applicationMock;
using TestTradeBot.mockServices.mockDataServices.dataPullers;

try
{
    // var cmdLineArgsOverride = new string[] { "--sim", "--verbose" };
    // args = cmdLineArgsOverride;
    AppInitializer.Init(
        cmdLineArgs: args,
        runtimeEnv: Env.Test,
        brokerageApiEnv: BrokerageApiEnv.PlayMoney
    );
    AppSettings.TurnOffStopTime(); // allow tests/simulations to run as long as needed
    var dataset = "dataset_2018";
    DateOnly startDte = new DateOnly(2019, 1, 1);
    DateOnly endDte = new DateOnly(2020, 1, 1);
    var symbolsToTrade = new List<string>{
        "SPY", "SH"
    };

    if (AppInitializer.RunMode == AppRunMode.RunSingleTestSimulation)
    {
        MockAppRunner appRunner = new MockAppRunner();

        var shAlgoParams = AlgoParamsGenerator.CreateData_Version_Big_Bear();
        shAlgoParams.PrintAlgorithmToLog(LogType.SimulationResults, "big_bear");

        var spyAlgoParams = AlgoParamsGenerator.CreateData_Version_Big_Bull();
        spyAlgoParams.PrintAlgorithmToLog(LogType.SimulationResults, "big_bull");

        var algoParamsBySymbol = new Dictionary<string, AlgoParams>
        {
            { "SH", shAlgoParams },
            { "SPY", spyAlgoParams },
        };
        await appRunner.RunMockMultiDaySimulation(symbolsToTrade, dataset, startDte, endDte, algoParamsBySymbol);
    }
    else if (AppInitializer.RunMode == AppRunMode.PullData)
    {
        // PULL CLOSE PRICES
        var closePriceStartDte = startDte.AddDays(-7);
        var closePriceEndDte = endDte.AddDays(-1);

        var closePricePuller = new AlpacaMockClosePriceBarPuller();
        await closePricePuller.Run(dataset, symbolsToTrade, closePriceStartDte, closePriceEndDte);

        // PULL PRICE UPDATES
        var dte = startDte;
        var priceUpdatePuller = new AlpacaMockPriceUpdatePuller();
        while (dte <= endDte)
        {
            await priceUpdatePuller.Run(dataset, symbolsToTrade, dte, dte);
            dte = dte.AddDays(1);
        }
    }
}
catch (Exception e)
{
    ExceptionUtil.AddException(e);
    throw;
}
finally
{
    ManagedResourceUtil.DisposeAll();
}