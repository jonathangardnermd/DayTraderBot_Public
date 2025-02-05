namespace DayTradeBot.common.util.exceptionUtil;

using System.Collections.Concurrent;
using DayTradeBot.common.util.loggingUtil;
using DayTradeBot.common.constants;

public static class ExceptionUtil
{
    private static ConcurrentQueue<Exception> ExceptionQueue = new ConcurrentQueue<Exception>();

    public static void ClearExceptions()
    {
        ExceptionQueue = new ConcurrentQueue<Exception>();
    }

    public static void AddException(Exception ex)
    {
        ExceptionQueue.Enqueue(ex);
        LoggingUtil.LogTo(LogType.Error, ex.ToString());
    }

    public static bool HasThrownExceptions()
    {
        return ExceptionQueue.Count > 0;
    }

    public static IEnumerable<Exception> GetExceptions()
    {
        return ExceptionQueue.ToList();
    }

    public static void LogExceptions()
    {
        foreach (var exception in ExceptionQueue)
        {
            LoggingUtil.LogTo(LogType.Error, exception.ToString() + "\n");
        }
    }
}