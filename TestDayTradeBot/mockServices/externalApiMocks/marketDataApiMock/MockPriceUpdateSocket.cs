namespace TestTradeBot.mockServices.externalApiMocks.marketDataApiMock;

using DayTradeBot.domains.externalApis.marketDataApi;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously.

public class MockPriceUpdateSocket : PriceUpdateSocketBase
{
    private IEnumerable<PriceUpdateSocketData>? DataList { get; set; }
    private int CurrentIdx { get; set; }
    private int DelayBetweenMessagesInMilli { get; set; }
    private CancellationTokenSource? CancellationTokenSource;

    private Action<PriceUpdateSocketData>? ReceiveFxn;
    public MockPriceUpdateSocket(int delayBetweenMessagesInMilli)
    {
        DelayBetweenMessagesInMilli = delayBetweenMessagesInMilli;
    }
    public override async Task Connect()
    {
        await Task.Delay(1);
    }

    public override async Task SubscribeForSymbols(List<string> symbols, Action<PriceUpdateSocketData> fxnToSubscribe)
    {
        // do nothing with the symbols because we actually control which symbols are sent via the SetDataList function!
        this.SetReceiveFxn(fxnToSubscribe);
        Start();
    }

    public void SetDataList(IEnumerable<PriceUpdateSocketData> dataList)
    {
        DataList = dataList;
    }



    public void SetReceiveFxn(Action<PriceUpdateSocketData> receiveFxn)
    {
        ReceiveFxn = receiveFxn;
    }

    public async Task Start()
    {
        if (ReceiveFxn == null)
        {
            throw new ArgumentNullException("ReceiveFxn cannot be null when starting a MockSocket");
        }
        if (DataList == null)
        {
            throw new ArgumentNullException("DataList cannot be null when starting a MockSocket");
        }
        await Task.Run(async () =>
        {
            CurrentIdx = 0;
            CancellationTokenSource = new CancellationTokenSource();
            while (CurrentIdx < DataList.Count())
            {
                if (CancellationTokenSource.Token.IsCancellationRequested)
                    break;
                PriceUpdateSocketData update = DataList.ElementAt(CurrentIdx);
                ReceiveFxn(update);
                CurrentIdx++;
                // await Task.Delay(DelayBetweenMessagesInMilli);
            }
        });
        Dispose();
    }

    protected override async Task DisposeAsync()
    {
        if (CancellationTokenSource == null)
        {
            throw new ArgumentNullException("A mock socket must be started before stopping it");
        }
        CancellationTokenSource.Cancel();
    }
}