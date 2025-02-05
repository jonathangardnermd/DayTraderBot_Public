
namespace DayTradeBot.contracts.externalApis.marketDataApi;

using DayTradeBot.domains.externalApis.marketDataApi;

public interface IMarketDataApiService
{
    PriceUpdateSocketBase PriceUpdateSocket { get; set; }

    Task<GetClosePriceBarsResponse> GetClosePriceBarDatas(IEnumerable<string> symbols);
}