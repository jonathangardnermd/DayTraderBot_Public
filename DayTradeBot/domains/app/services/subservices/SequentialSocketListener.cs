namespace DayTradeBot.domains.app;

using System.Collections.Concurrent;
using DayTradeBot.common.util.exceptionUtil;
using DayTradeBot.common.util.managedResourceUtil;

/*
The EnqueueUpdate function of this class is subscribed to receive price updates from the websocket.
We want the priceUpdates to be processing one at a time, waiting for the processing of the previous 
price update to be completed before beginning the next. The socket itself won't do that for us, so 
this class was created to "throttle" the price updates as they come in.

Note, since this class inherits from Threadable, it will never have more than one thread running at a time.
*/
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
public class SequentialSocketListener<T> : Threadable
{
    private ConcurrentQueue<T> updateQueue = new ConcurrentQueue<T>();
    private Func<T, Task>? SubscribingFunction;

    public bool IsEmptyQueue()
    {
        return updateQueue.Count == 0;
    }

    public void EnqueueUpdate(T update)
    {
        /*
        Price updates (or any type of update since generics are being used) are added to the 
        thread-safe ConcurrentQueue in the order in which they are received.
        */
        if (ExceptionUtil.HasThrownExceptions())
        {
            return;
        }

        updateQueue.Enqueue(update);

        /*
        RunIfNotRunning() ultimately calls Main() via the Threadable parent class. The detour through the threadable 
        parent class is to delegate the burden of ensuring one and only one thread is created to Threadable. 
        */
        this.RunIfNotRunning();
    }

    protected override async Task Main()
    {
        /*
        This function processes all the updates in the queue until they have all been processed.
        At that point - when all updates have been processed and the queue is empty - the Threadable 
        parent class - which invokes this function - will properly handle the threading logic 
        (e.g. set the state of the threadable to isRunning=false and dispose of the thread). 
        */
        if (SubscribingFunction == null)
        {
            throw new Exception("Must subscribe a function to the socket listener before processing updates.");
        }

        // allow some priceUpdates to get queued so we aren't constantly creating and disposing threads
        await Task.Delay(100);

        // process all priceUpdates in the queue
        while (updateQueue.TryDequeue(out var priceUpdate))
        {
            if (CancellationTokenSource.Token.IsCancellationRequested)
                break;
            await SubscribingFunction(priceUpdate);
        }
    }

    public void Subscribe(Func<T, Task> fxnToSubscribe)
    {
        SubscribingFunction = fxnToSubscribe;
    }
}