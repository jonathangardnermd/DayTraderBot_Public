namespace TestTradeBot.mockServices.applicationMock;

using DayTradeBot.contracts.externalApis.brokerageApi;
using DayTradeBot.contracts.externalApis.marketDataApi;
using DayTradeBot.common.constants;
using DayTradeBot.common.settings;
using DayTradeBot.common.util.dataStructureUtil;
using DayTradeBot.common.util.loggingUtil;
using DayTradeBot.domains.app;
using DayTradeBot.domains.tradeEngine.algo;
using DayTradeBot.domains.externalApis.marketDataApi;
using DayTradeBot.domains.externalApis.brokerageApi;

using TestTradeBot.mockServices.mockDataServices.dataLoaders;
using TestTradeBot.mockServices.mockDataServices.common;
using TestTradeBot.mockServices.externalApiMocks.brokerageApiMock;
using TestTradeBot.mockServices.externalApiMocks.marketDataApiMock;

public class MockAppFactory
{
    private Dictionary<DateOnly, Dictionary<string, ClosePriceEntry>> ClosePricesByDteAndSymbol { get; set; }
    private Dictionary<DateOnly, List<PriceUpdateEntry>> PriceUpdatesByDte { get; set; }

    private List<DateOnly> ReverseSortedTradingDtes { get; set; }

    private List<string> SymbolsToTrade { get; set; }

    public MockAppFactory(string dataset, List<string> symbolsToTrade)
    {
        SymbolsToTrade = symbolsToTrade;

        var closePriceLoader = new MockClosePriceBarLoader(dataset);
        ClosePricesByDteAndSymbol = closePriceLoader.LoadAllClosePricesFromFiles();

        var priceUpdateLoader = new MockPriceUpdateLoader(dataset);
        PriceUpdatesByDte = priceUpdateLoader.LoadAllPriceUpdatesFromFiles();

        /*
        The priceUpdates sometimes contain updates on days when the market isn't open. 
        Therefore, the close prices are a better indicator of which dates are "trading dates", 
        meaning days when the market is open.
        */
        ReverseSortedTradingDtes = ClosePricesByDteAndSymbol.Keys.OrderDescending().ToList();
    }

    public DateOnly GetLastTradingDteBeforeOrEqualTo(DateOnly lastAllowedDte)
    {
        int idx = ReverseSortedTradingDtes.IndexOf(dte => dte <= lastAllowedDte);
        if (idx < 0)
        {
            throw new Exception($"No trading dte before or equal to {lastAllowedDte:yyyy-MM-dd}");
        }
        return ReverseSortedTradingDtes.ElementAt(idx);
    }

    private DateOnly GetLastTradingDteBefore(DateOnly refDte)
    {
        int idx = ReverseSortedTradingDtes.IndexOf(dte => dte < refDte);
        if (idx < 0)
        {
            throw new Exception($"No trading dte before {refDte:yyyy-MM-dd}");
        }
        return ReverseSortedTradingDtes.ElementAt(idx);
    }

    private DateOnly GetFirstTradingDteOnOrAfter(DateOnly refDte)
    {
        var sortedTradingDtes = ClosePricesByDteAndSymbol.Keys.Order().ToList();
        int idx = sortedTradingDtes.IndexOf(dte => dte >= refDte);
        if (idx < 0)
        {
            throw new Exception($"No trading dte on or after {refDte:yyyy-MM-dd}");
        }
        return sortedTradingDtes.ElementAt(idx);
    }

    private bool IsTradingDte(DateOnly dte)
    {
        /*
        The priceUpdates sometimes contain updates on days when the market isn't open. 
        Therefore, the close prices are a better indicator of which dates are "trading dates", 
        meaning days when the market is open.
        */
        return ClosePricesByDteAndSymbol.ContainsKey(dte);
    }

    public (Dictionary<string, ClosePriceEntry>, Dictionary<string, ClosePriceEntry>) GetStartAndEndClosePriceEntries(DateOnly startDte, DateOnly endDte)
    {
        var lastTradingDte = GetLastTradingDteBeforeOrEqualTo(endDte);
        var firstTradingDte = GetFirstTradingDteOnOrAfter(startDte);

        return (ClosePricesByDteAndSymbol[firstTradingDte], ClosePricesByDteAndSymbol[lastTradingDte]);
    }

    public App? CreateNewApplicationService(DateOnly today, float freeUsdBal, Dictionary<string, AlgoParams> algoParamsBySymbol)
    {
        if (!IsTradingDte(today))
        {
            return null;
        }

        var prevTradingDte = GetLastTradingDteBefore(today);
        var freeUsdBalStr = LoggingUtil.UsdToString(freeUsdBal);

        if (AppSettings.IsLogTypeOn(LogType.Debug))
        {
            LoggingUtil.LogTo(LogType.Debug, $"Creating AppService for Today={today:yyyy-MM-dd}, LastTradingDte={prevTradingDte}, FreeUsdBalance={freeUsdBalStr}");
        }
        if (!ClosePricesByDteAndSymbol.ContainsKey(prevTradingDte))
        {
            return null;
        }

        var closePricesBySymbol = ClosePricesByDteAndSymbol[prevTradingDte].Select(kvp =>
            new ClosePriceBar
            {
                Symbol = kvp.Value.Symbol,
                Dte = kvp.Value.Date,
                Close = kvp.Value.ClosePrice
            });

        var priceUpdates = PriceUpdatesByDte[today].Select(entry =>
            new PriceUpdateSocketData(entry.Symbol, entry.Price, entry.Price, entry.TimeOfDay));

        // initialize APIs: BrokerageApi and MarketDataApi
        IBrokerageApiService brokerageApi = new MockBrokerageApiService();
        var mockBrokerageApi = (MockBrokerageApiService)brokerageApi;
        mockBrokerageApi.SetAccountData(new AccountData
        {
            FreeUsdBalance = freeUsdBal
        });
        IMarketDataApiService marketDataApi = new MockMarketDataApiService(priceUpdates, closePricesBySymbol);

        var engine = new TradeEngineAlgo(marketDataApi, brokerageApi, SymbolsToTrade, today, algoParamsBySymbol);
        var app = new App(engine);
        return app;
    }
}