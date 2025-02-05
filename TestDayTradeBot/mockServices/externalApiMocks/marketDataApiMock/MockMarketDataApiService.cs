
namespace TestTradeBot.mockServices.externalApiMocks.marketDataApiMock;

using DayTradeBot.contracts.externalApis.marketDataApi;
using DayTradeBot.domains.externalApis.marketDataApi;

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously.

public class MockMarketDataApiService : IMarketDataApiService
{
    public PriceUpdateSocketBase PriceUpdateSocket { get; set; }

    private IEnumerable<ClosePriceBar> ClosePriceBarDatas { get; set; }

    public MockMarketDataApiService(IEnumerable<PriceUpdateSocketData> priceUpdates, IEnumerable<ClosePriceBar> closePriceBars)
    {
        PriceUpdateSocket = new MockPriceUpdateSocket(1);
        var mockSocket = (MockPriceUpdateSocket)PriceUpdateSocket;
        mockSocket.SetDataList(priceUpdates);
        ClosePriceBarDatas = closePriceBars;
    }

    public async Task<GetClosePriceBarsResponse> GetClosePriceBarDatas(IEnumerable<string> symbols)
    {
        var closePriceResp = new GetClosePriceBarsResponse();
        closePriceResp.Datas = ClosePriceBarDatas;
        return closePriceResp;
    }
}