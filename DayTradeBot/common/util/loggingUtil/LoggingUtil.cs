namespace DayTradeBot.common.util.loggingUtil;

using System.Globalization;
using Serilog;
using Serilog.Core;
using DayTradeBot.common.settings;
using DayTradeBot.common.util.fileUtil;
using DayTradeBot.common.constants;

public static class LoggingUtil
{
    public static Dictionary<LogType, Logger> Loggers = new Dictionary<LogType, Logger>();

    public static void InitLoggers()
    {
        var consoleAndFileLogTypes = new[]
        {
            LogType.Debug,
            LogType.Main,
            LogType.Error,
            LogType.ManagedResources,
            LogType.SimulationResults,
            LogType.DailySummary,
            LogType.OrderChanges
        };

        var allLogTypes = Enum.GetValues(typeof(LogType)).Cast<LogType>();

        foreach (LogType logType in allLogTypes)
        {
            var fileName = $"{logType.ToString()}.log";
            if (consoleAndFileLogTypes.Contains(logType))
            {
                Loggers[logType] = GetConsoleAndFileLogger(fileName);
            }
            else
            {
                Loggers[logType] = GetFileOnlyLogger(fileName);
            }
        }
    }

    private static Logger GetLogger(string fileName, bool writeToConsole, bool shouldPrependConsoleStrings = true)
    {
        string folderPath = AppSettings.GetLogsFolderPath();
        FileUtil.EnsureDirectoryExists(folderPath);

        string filePath = Path.Combine(folderPath, fileName);

        var loggerConfiguration = new LoggerConfiguration()
            .WriteTo.File(filePath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7);

        if (writeToConsole)
        {
            string outputTemplate = "{Message}{NewLine}";
            if (shouldPrependConsoleStrings)
            {
                outputTemplate = "[{Timestamp:HH:mm:ss}] {Message}{NewLine}";
            }
            loggerConfiguration = loggerConfiguration.WriteTo.Console(outputTemplate: outputTemplate);
        }

        var log = loggerConfiguration.CreateLogger();
        return log;
    }

    private static Logger GetConsoleAndFileLogger(string fileName)
    {
        return GetLogger(fileName, writeToConsole: true);
    }

    private static Logger GetFileOnlyLogger(string fileName)
    {
        return GetLogger(fileName, writeToConsole: false);
    }


    public static void LogTo(LogType logType, string strToLog)
    {
        if (!AppSettings.IsLogTypeOn(logType))
        {
            throw new Exception($"Cannot log to a log that is not turned on. Logtype={logType}");
        }
        Loggers[logType].Information(strToLog);
    }

    public static string PctToString(float? pct)
    {
        if (pct == null)
        {
            return "--";
        }
        var nonNullPct = (float)pct;
        return (nonNullPct * 100).ToString("0.00") + "%";
    }

    public static string UsdToString(float? usdAmount)
    {
        if (usdAmount == null)
        {
            return "--";
        }
        var nonNullAmt = (float)usdAmount;
        return nonNullAmt.ToString("C", CultureInfo.GetCultureInfo("en-US"));
    }

    public static string DteToString(DateOnly? dte)
    {
        if (dte == null)
        {
            return "--";
        }
        return $"{dte:yyyy-MM-dd}";
    }

    public static string TimeSpanToString(TimeSpan timeSpan)
    {
        return timeSpan.ToString(@"dd\.hh\:mm\:ss");
    }
    public static string TimeToString(TimeOnly timeOnly)
    {
        return timeOnly.ToString(@"hh\:mm\:ss");
    }

    public static string DateTimeToString(DateTime? dt)
    {
        if (dt == null)
        {
            return "--";
        }
        return $"{dt:yyyy-MM-dd hh:mm:ss}";
    }

    public static string GetGuidString(string? guid, bool shouldTruncate)
    {

        guid = guid ?? "--";
        if (shouldTruncate)
        {
            string[] sections = guid.Split('-');
            guid = sections[^1];
        }
        return guid;
    }

    public static string GetGuidString(Guid? guid, bool shouldTruncate)
    {
        return GetGuidString(guid?.ToString(), shouldTruncate);
    }
}