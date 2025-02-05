
namespace DayTradeBot.domains.externalApis.marketDataApi;

using Alpaca.Markets;
using DayTradeBot.contracts.externalApis.marketDataApi;
using DayTradeBot.common.util.alpacaUtil;

public class MarketDataApiService : IMarketDataApiService
{
    public PriceUpdateSocketBase PriceUpdateSocket { get; set; }
    private IAlpacaDataClient DataClient { get; set; }

    public MarketDataApiService()
    {
        var apiKeys = AlpacaUtil.GetAlpacaApiKeys();
        var priceUpdateSocket = new PriceUpdateSocket(apiKeys.ApiKey, apiKeys.ApiSecret);
        PriceUpdateSocket = priceUpdateSocket;

        DataClient = Environments.Live.GetAlpacaDataClient(new SecretKey(apiKeys.ApiKey, apiKeys.ApiSecret));
    }

    public async Task<GetClosePriceBarsResponse> GetClosePriceBarDatas(IEnumerable<string> symbols)
    {
        DateTime start = DateTime.Today.AddDays(-7);
        DateTime end = DateTime.Today.AddDays(-1);
        var timeframe = BarTimeFrame.Day;

        var closePriceDatas = new List<ClosePriceBar>();
        foreach (var symbol in symbols)
        {
            var bars = await DataClient.ListHistoricalBarsAsync(
                new HistoricalBarsRequest(symbol, start, end, timeframe));

            var bar = bars.Items[bars.Items.Count - 1];

            closePriceDatas.Add(
                new ClosePriceBar
                {
                    Close = (float)bar.Close,
                    Symbol = bar.Symbol,
                    Dte = DateOnly.FromDateTime(bar.TimeUtc)
                }
            );
        }

        return new GetClosePriceBarsResponse
        {
            Datas = closePriceDatas
        };
    }
}