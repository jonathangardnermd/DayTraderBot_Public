using DayTradeBot.common.constants;
using DayTradeBot.domains.appOrchestration;
using DayTradeBot.common.util.managedResourceUtil;
using DayTradeBot.common.util.exceptionUtil;

try
{
    // var cmdLineArgsOverride = new string[] { "--D" };
    // args = cmdLineArgsOverride;
    AppInitializer.Init(
        cmdLineArgs: args,
        runtimeEnv: Env.Prod,
        brokerageApiEnv: BrokerageApiEnv.PlayMoney
    );

    var symbolsToTrade = new List<string>{
        "SPY", "SPDN"
    };
    var appRunner = new AppRunner();
    await appRunner.RunRealDayTrader(symbolsToTrade);
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





