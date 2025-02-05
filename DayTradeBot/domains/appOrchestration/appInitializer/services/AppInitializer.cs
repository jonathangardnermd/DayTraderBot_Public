namespace DayTradeBot.domains.appOrchestration;

using DayTradeBot.common.settings;
using DayTradeBot.common.constants;
using DayTradeBot.common.util.fileUtil;
using DayTradeBot.common.util.loggingUtil;
using DayTradeBot.common.util.managedResourceUtil;
using DayTradeBot.domains.tradeEngine.core;
using DayTradeBot.domains.externalApis.brokerageApi;

/*
The AppInitializer turns logs on/off, controls which brokerageApi is used (i.e. real or play money), etc.

AppInitializer.Init() should be called as the first step at the root of any script. See Program.cs for an example.
*/
public class AppInitializer
{
    public static AppRunMode? RunMode { get; private set; }

    public static void Init(string[] cmdLineArgs, Env runtimeEnv, BrokerageApiEnv brokerageApiEnv)
    {
        Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine("\n\n");
            ManagedResourceUtil.DisposeAll();
        };
        AppSettings.SetEnvironment(runtimeEnv);
        AppSettings.SetBrokerageEnv(brokerageApiEnv);
        HandleCmdLineArgs(cmdLineArgs);
        InitLoggers();

        if (RunMode == null)
        {
            RunMode = AppRunMode.RunSingleTestSimulation;
            LoggingUtil.LogTo(LogType.Main, $"No option passed to determine RunMode. Defaulted to {RunMode.ToString()}");
        }
    }

    /*
    Any options passed via the command line are parsed and handled here. 

    Using these options you can 
    1) Delete all logs, or all persistedData, or both for whichever RuntimeEnv you're in (Prod or Test)
    2) Control the logging level (e.g. verbose or quiet, etc)
    3) Depending on your runtime environment, you may also control the "RunMode", which serves the same purpose as an interactive menu that offers multiple options of different scripts to run.
    */
    private static void HandleCmdLineArgs(string[] cmdLineArgs)
    {
        AppSettings.SetLogMode(LogMode.Default);

        bool shouldDeleteLogs = false;
        bool shouldDeletePersistedData = false;
        foreach (string arg in cmdLineArgs)
        {
            if (arg == "--quiet")
            {
                AppSettings.SetLogMode(LogMode.Quiet);
            }
            else if (arg == "--verbose")
            {
                AppSettings.SetLogMode(LogMode.Verbose);
            }
            else if (arg == "--talkative")
            {
                AppSettings.SetLogMode(LogMode.Talkative);
            }
            else if (arg == "--D")
            {
                shouldDeleteLogs = true;
                shouldDeletePersistedData = true;
            }
            else if (arg == "--dl")
            {
                shouldDeleteLogs = true;
            }
            else if (arg == "--dp")
            {
                shouldDeletePersistedData = true;
            }
            else if (AppSettings.RuntimeEnv == Env.Test && arg == "--sims")
            {
                RunMode = AppRunMode.RunMultipleTestSimulations;
            }
            else if (AppSettings.RuntimeEnv == Env.Test && arg == "--sim")
            {
                RunMode = AppRunMode.RunSingleTestSimulation;
            }
            else if (AppSettings.RuntimeEnv == Env.Test && arg == "--test")
            {
                RunMode = AppRunMode.RunTests;
            }
            else if (AppSettings.RuntimeEnv == Env.Test && arg == "--pulldata")
            {
                RunMode = AppRunMode.PullData;
            }
        }

        if (AppSettings.RuntimeEnv == Env.Prod)
        {
            // only one runMode in prod for now. If that changes, this can be made cmdLineArg-dependent and moved up into the foreach
            RunMode = AppRunMode.RunProd;
        }

        if (shouldDeleteLogs || AppSettings.RuntimeEnv == Env.Test)
        {
            DeleteAllLogsForCurrentEnvironment();
        }
        if (shouldDeletePersistedData || AppSettings.RuntimeEnv == Env.Test)
        {
            DeleteAllPersistedDataForCurrentEnvironment();
        }
    }

    /*
    Initialize the loggers and turn them on/off depending on the LogMode. 

    For tabular logs, add a header row to the log file. Note, log files are not deleted, so this will potentially add a header row to a log file that already has data in it.
    */
    private static void InitLoggers()
    {
        LoggingUtil.InitLoggers();
        LoggingUtil.LogTo(LogType.Main, $"LogMode is {AppSettings.LogMode.ToString()}.");

        // since logs are on by default, turn off logs according to the logMode
        if (AppSettings.LogMode == LogMode.Default)
        {
            AppSettings.TurnLogTypeOff(LogType.Debug);
            AppSettings.TurnLogTypeOff(LogType.PriceUpdateRound);
            AppSettings.TurnLogTypeOff(LogType.DailySummary);
        }
        else if (AppSettings.LogMode == LogMode.Quiet)
        {
            AppSettings.TurnLogTypeOff(LogType.Debug);
            AppSettings.TurnLogTypeOff(LogType.PriceUpdateRound);
            AppSettings.TurnLogTypeOff(LogType.ApiPlaceOrder);
            AppSettings.TurnLogTypeOff(LogType.ApiGetOrderStatus);
            AppSettings.TurnLogTypeOff(LogType.ApiGetAccount);
            AppSettings.TurnLogTypeOff(LogType.ApiCancelOrder);
            AppSettings.TurnLogTypeOff(LogType.OrderAction);
            AppSettings.TurnLogTypeOff(LogType.PositionRenewalAction);
            AppSettings.TurnLogTypeOff(LogType.OrderChanges);
            AppSettings.TurnLogTypeOff(LogType.DailySummary);
        }
        else if (AppSettings.LogMode == LogMode.Talkative)
        {
            AppSettings.TurnLogTypeOff(LogType.Debug);
            AppSettings.TurnLogTypeOff(LogType.PriceUpdateRound);
            AppSettings.TurnLogTypeOff(LogType.ApiPlaceOrder);
            AppSettings.TurnLogTypeOff(LogType.ApiGetOrderStatus);
            AppSettings.TurnLogTypeOff(LogType.ApiGetAccount);
            AppSettings.TurnLogTypeOff(LogType.ApiCancelOrder);
            AppSettings.TurnLogTypeOff(LogType.OrderAction);
            AppSettings.TurnLogTypeOff(LogType.PositionRenewalAction);
        }

        // add header rows to the tabular log files
        if (AppSettings.IsLogTypeOn(LogType.PriceUpdateRound))
        {
            string headerStr = GetLogHeaderRow(PriceUpdateRound.GetColumnDefinitons());
            LoggingUtil.LogTo(LogType.PriceUpdateRound, headerStr);
        }

        if (AppSettings.IsLogTypeOn(LogType.OrderAction))
        {
            string headerStr = GetLogHeaderRow(OrderAction.GetColumnDefinitons());
            LoggingUtil.LogTo(LogType.OrderAction, headerStr);
        }

        if (AppSettings.IsLogTypeOn(LogType.PositionRenewalAction))
        {
            string headerStr = GetLogHeaderRow(PositionRenewalAction.GetColumnDefinitons());
            LoggingUtil.LogTo(LogType.PositionRenewalAction, headerStr);
        }

        if (AppSettings.IsLogTypeOn(LogType.ApiPlaceOrder))
        {
            string headerStr = GetLogHeaderRow(PlaceLimitOrderResponse.GetColumnDefinitons());
            LoggingUtil.LogTo(LogType.ApiPlaceOrder, headerStr);
        }

        if (AppSettings.IsLogTypeOn(LogType.ApiGetOrderStatus))
        {
            string headerStr = GetLogHeaderRow(GetOrderStatusResponse.GetColumnDefinitons());
            LoggingUtil.LogTo(LogType.ApiGetOrderStatus, headerStr);
        }

        if (AppSettings.IsLogTypeOn(LogType.ApiGetAccount))
        {
            string headerStr = GetLogHeaderRow(GetAccountDataResponse.GetColumnDefinitons());
            LoggingUtil.LogTo(LogType.ApiGetAccount, headerStr);
        }

        if (AppSettings.IsLogTypeOn(LogType.ApiCancelOrder))
        {
            string headerStr = GetLogHeaderRow(CancelOrderResponse.GetColumnDefinitons());
            LoggingUtil.LogTo(LogType.ApiCancelOrder, headerStr);
        }
    }

    private static void DeleteAllLogsForCurrentEnvironment()
    {
        FileUtil.DeleteFilesInFolder(AppSettings.GetLogsFolderPath());
    }

    private static void DeleteAllPersistedDataForCurrentEnvironment()
    {
        FileUtil.DeleteFilesInFolder(AppSettings.GetPersistFolderPath());
    }

    private static string GetLogHeaderRow(List<TableColumn> colDefs)
    {
        string logRowStr = "";
        foreach (var col in colDefs)
        {
            logRowStr += col.ColName.PadRight(col.ColWidth);
        }
        return logRowStr;
    }
}