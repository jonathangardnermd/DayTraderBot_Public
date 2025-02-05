
namespace DayTradeBot.contracts.externalApis.brokerageApi;

using DayTradeBot.domains.tradeEngine.core;
using DayTradeBot.domains.externalApis.brokerageApi;

public interface IBrokerageApiService
{
    Task<PlaceLimitOrderResponse> PlaceLimitBuyOrder(BuyOrder order);

    Task<PlaceLimitOrderResponse> PlaceLimitSellOrder(SellOrder order);

    Task<CancelOrderResponse> CancelOrder(Order order);

    Task<GetAccountDataResponse> GetAccountData();

    Task<GetOrderStatusResponse> GetOrderStatus(Order order);
}