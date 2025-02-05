
namespace DayTradeBot.common.util.managedResourceUtil;

using DayTradeBot.common.util.exceptionUtil;
using DayTradeBot.common.util.loggingUtil;
using System.Collections.Concurrent;
using DayTradeBot.common.constants;

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously.

public class ManagedResourceUtil
{
    private static bool disposing = false;
    private static ConcurrentDictionary<Guid, (Threadable, Task)> threadables = new ConcurrentDictionary<Guid, (Threadable, Task)>();
    private static ConcurrentDictionary<Guid, Disposable> disposables = new ConcurrentDictionary<Guid, Disposable>();

    public static void AddRunningThreadable(Threadable threadable, Task task)
    {
        if (disposing)
        {
            // TODO: maybe handle this differently
            return;
        }
        threadables.TryAdd(threadable.Uid, (threadable, task));

        if (threadables.Count > 100)
        {
            TrimDisposedThreadables();
        }
    }

    public static void TrimDisposedThreadables()
    {
        (Threadable, Task) val;
        foreach (var item in threadables.Values)
        {
            var (currThreadable, currTask) = item;
            if (currThreadable.IsDisposed())
            {
                threadables.TryRemove(currThreadable.Uid, out val);
            }
        }
    }

    public static void AddDisposable(Disposable disposable)
    {
        disposables.TryAdd(disposable.Uid, disposable);
    }

    public static void DisposeAll()
    {
        if (disposing == true)
        {
            return;
        }
        disposing = true;
        DisposeAllThreadables();
        DisposeAllDisposables();

        disposables = new ConcurrentDictionary<Guid, Disposable>();
        threadables = new ConcurrentDictionary<Guid, (Threadable, Task)>();

        disposing = false;
    }
    private static void DisposeAllDisposables()
    {
        foreach (var disposable in disposables.Values)
        {
            disposable.Dispose();
        }
        LoggingUtil.LogTo(LogType.ManagedResources, $"Disposed of {disposables.Count()} disposables.");
    }
    private static void DisposeAllThreadables()
    {
        Dictionary<Guid, int> numIncompleteTasksByThreadableUid = new Dictionary<Guid, int>();
        foreach (var item in threadables.Values)
        {
            var (threadable, task) = item;
            if (!task.IsCompleted)
            {
                var uid = threadable.Uid;
                if (!numIncompleteTasksByThreadableUid.ContainsKey(uid))
                {
                    numIncompleteTasksByThreadableUid[uid] = 1;
                }
                else
                {
                    numIncompleteTasksByThreadableUid[uid]++;
                    throw new Exception("More than one running task for a single threadable!");
                }
            }
            threadable.Dispose();
        }
        LoggingUtil.LogTo(LogType.ManagedResources, $"Disposed of {threadables.Count()} threadables.");
    }
}

public abstract class Threadable : Disposable
{
    protected CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
    private int isRunning = 0;

    protected abstract Task Main();

    public async Task RunIfNotRunning()
    {
        if (Interlocked.Exchange(ref isRunning, 1) == 0)
        {
            try
            {
                CancellationTokenSource = new CancellationTokenSource();
                Task task = Task.Run(Main);
                ManagedResourceUtil.AddRunningThreadable(this, task);
                await task;
            }
            catch (Exception e)
            {
                ExceptionUtil.AddException(e);
            }
            finally
            {
                Interlocked.Exchange(ref isRunning, 0);
                Dispose();
            }
        }
    }

    protected override async Task DisposeAsync()
    {
        if (IsRunning())
        {
            CancellationTokenSource.Cancel();
        }
    }

    private bool IsRunning()
    {
        return isRunning == 1;
    }
}

public abstract class Disposable : IDisposable
{
    public Guid Uid { get; private set; }
    private int isDisposed = 0;

    public Disposable()
    {
        Uid = Guid.NewGuid();
    }
    protected abstract Task DisposeAsync();

    public bool IsDisposed()
    {
        return isDisposed == 1;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref isDisposed, 1) == 0)
        {
            DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}