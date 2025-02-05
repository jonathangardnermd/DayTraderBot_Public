namespace DayTradeBot.domains.externalApis.marketDataApi;

using Alpaca.Markets;
using DayTradeBot.common.settings;
using DayTradeBot.common.constants;
using DayTradeBot.common.util.loggingUtil;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.

public class PriceUpdateSocket : PriceUpdateSocketBase
{
    private IAlpacaDataStreamingClient AlpacaDataStreamingClient { get; set; }
    private Action<PriceUpdateSocketData> SubscribedFxn { get; set; }

    public PriceUpdateSocket(string API_KEY, string API_SECRET)
    {
        AlpacaDataStreamingClient = Environments.Live.GetAlpacaDataStreamingClient(new SecretKey(API_KEY, API_SECRET));
    }

    public override async Task Connect()
    {
        var authStatus = await AlpacaDataStreamingClient.ConnectAndAuthenticateAsync();

        if (AppSettings.IsLogTypeOn(LogType.Debug))
        {
            LoggingUtil.LogTo(LogType.Debug, $"PriceUpdateSocket Connection AuthStatus={authStatus.ToString()}");
        }
    }

    public override async Task SubscribeForSymbols(List<string> symbols, Action<PriceUpdateSocketData> fxnToSubscribe)
    {
        SubscribedFxn = fxnToSubscribe;
        foreach (var symbol in symbols)
        {
            var sub = AlpacaDataStreamingClient.GetQuoteSubscription(symbol);
            sub.Received += OnReceived;

            /*
            This next line creates a new worker thread in which the subscribed function will receive the updates from the socket.
            That means this function will return fairly quickly and allow the main thread to continue processing as 
            updates are received in the separate worker thread.

            Note also that the OnReceived function we subscribe is NOT asynchronous. We want to process price updates 
            one at a time, but they may come in simulataneously here, and they won't wait for the processing of the 
            previous price update to be completed before OnReceived is called again. 

            Therefore, we need to "throttle" the price updates somehow, and that is what the "SequentialSocketListener" 
            is for. 
            */
            await AlpacaDataStreamingClient.SubscribeAsync(sub);
        }
    }

    private void OnReceived(IQuote quote)
    {
        if (SubscribedFxn == null)
        {
            throw new Exception("SubscribedFxn should not be null when receiving data.");
        }
        SubscribedFxn(new PriceUpdateSocketData(
            symbol: quote.Symbol,
            currentAskPrice: (float)quote.AskPrice,
            currentBidPrice: (float)quote.BidPrice,
            timeOfDay: quote.TimestampUtc.TimeOfDay
        ));
    }

    protected override async Task DisposeAsync()
    {
        await AlpacaDataStreamingClient.DisconnectAsync();
        AlpacaDataStreamingClient.Dispose();
    }
}