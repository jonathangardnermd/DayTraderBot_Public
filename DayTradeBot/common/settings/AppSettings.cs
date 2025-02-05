namespace DayTradeBot.common.settings;

using DayTradeBot.common.constants;

public class AppSettings
{
    public static TimeSpan? StopAppAfterUtcTime { get; set; } = new TimeSpan(20, 0, 15);
    public static Dictionary<LogType, bool> LogToggles { get; private set; } = new Dictionary<LogType, bool>();
    public static Env? RuntimeEnv { get; private set; }
    public static LogMode? LogMode { get; private set; }



    public static BrokerageApiEnv BrokerageEnv { get; private set; }
    public static string GetRootDirectory()
    {
        string? rootDir = Environment.GetEnvironmentVariable("DAY_TRADER_BOT_ROOT_DIR");
        if (!string.IsNullOrEmpty(rootDir))
        {
            return rootDir;
        }
        return "/Users/jonathangardner/Projects/DayTradeBot";
    }

    public static void TurnOffStopTime()
    {
        StopAppAfterUtcTime = null;
    }
    public static void TurnLogTypeOn(LogType logType)
    {
        LogToggles[logType] = true;
    }

    public static void TurnLogTypeOff(LogType logType)
    {
        LogToggles[logType] = false;
    }

    public static bool IsLogTypeOn(LogType logType)
    {
        if (!LogToggles.ContainsKey(logType))
        {
            // default is for logs to be on
            return true;
        }
        return LogToggles[logType];
    }

    public static void SetEnvironment(Env env)
    {
        RuntimeEnv = env;
    }

    public static void SetLogMode(LogMode logMode)
    {
        LogMode = logMode;
    }


    public static void SetBrokerageEnv(BrokerageApiEnv brokerageEnv)
    {
        BrokerageEnv = brokerageEnv;
    }

    public static string GetPersistFolderPath()
    {
        if (AppSettings.RuntimeEnv == Env.Prod)
        {
            return Path.Combine(GetRootDirectory(), "_output", "prod", "persistedData");
        }
        return Path.Combine(GetRootDirectory(), "_output", "test", "persistedData");
    }

    public static string GetLogsFolderPath()
    {
        if (AppSettings.RuntimeEnv == Env.Prod)
        {
            return Path.Combine(GetRootDirectory(), "_output", "prod", "logs");
        }
        return Path.Combine(GetRootDirectory(), "_output", "test", "logs");
    }
}