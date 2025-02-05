namespace TestTradeBot.mockServices.externalApiMocks.brokerageApiMock;

using DayTradeBot.contracts.externalApis.brokerageApi;
using DayTradeBot.common.constants;
using DayTradeBot.common.settings;
using DayTradeBot.common.util.loggingUtil;
using DayTradeBot.domains.externalApis.brokerageApi;
using DayTradeBot.domains.tradeEngine.core;

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously.

public class MockBrokerageApiService : IBrokerageApiService
{
    private AccountData? AccountData { get; set; }
    private Dictionary<Guid, float> FilledPriceByOrderGuid { get; set; } = new Dictionary<Guid, float>();

    public MockBrokerageApiService()
    {
    }

    public async Task<PlaceLimitOrderResponse> PlaceLimitBuyOrder(BuyOrder order)
    {
        var resp = new PlaceLimitOrderResponse();
        resp.WasSuccessfulResp = true;
        resp.LimitOrder = order;
        resp.RawStatusFromResponse = "New";

        if (AppSettings.IsLogTypeOn(LogType.ApiPlaceOrder))
        {
            LoggingUtil.LogTo(LogType.ApiPlaceOrder, resp.ToLogRow());
        }
        return resp;
    }

    public async Task<PlaceLimitOrderResponse> PlaceLimitSellOrder(SellOrder order)
    {
        var resp = new PlaceLimitOrderResponse();
        resp.WasSuccessfulResp = true;
        resp.LimitOrder = order;
        resp.RawStatusFromResponse = "New";

        if (AppSettings.IsLogTypeOn(LogType.ApiPlaceOrder))
        {
            LoggingUtil.LogTo(LogType.ApiPlaceOrder, resp.ToLogRow());
        }
        return resp;
    }

    public async Task<CancelOrderResponse> CancelOrder(Order order)
    {
        var resp = new CancelOrderResponse();
        resp.WasSuccessfulResp = true;
        resp.Order = order;

        if (AppSettings.IsLogTypeOn(LogType.ApiCancelOrder))
        {
            LoggingUtil.LogTo(LogType.ApiCancelOrder, resp.ToLogRow());
        }
        return resp;
    }

    public async Task<GetAccountDataResponse> GetAccountData()
    {
        var resp = new GetAccountDataResponse();
        resp.Data = AccountData;
        resp.WasSuccessfulResp = true;

        if (AppSettings.IsLogTypeOn(LogType.ApiGetAccount))
        {
            LoggingUtil.LogTo(LogType.ApiGetAccount, resp.ToLogRow());
        }
        return resp;
    }

    public async Task<GetOrderStatusResponse> GetOrderStatus(Order order)
    {
        var resp = new GetOrderStatusResponse();

        var avgFilledPrice = order.LimitPrice;
        if (FilledPriceByOrderGuid.ContainsKey(order.Uid))
        {
            avgFilledPrice = FilledPriceByOrderGuid[order.Uid];
        }
        resp.Data = new OrderStatusData
        {
            Status = OrderStatus.Filled,
            FilledQuantity = order.OrderQuantity,
            AvgFilledPrice = avgFilledPrice
        };
        resp.RawStatusFromResponse = OrderStatus.Filled.ToString();
        resp.LimitOrder = order;
        resp.WasSuccessfulResp = true;

        if (AppSettings.IsLogTypeOn(LogType.ApiGetOrderStatus))
        {
            LoggingUtil.LogTo(LogType.ApiGetOrderStatus, resp.ToLogRow());
        }
        return resp;
    }

    public void SetAccountData(AccountData accountData)
    {
        AccountData = accountData;
    }
    public void SetFilledPrice(Order order, float filledPrice)
    {
        FilledPriceByOrderGuid[order.Uid] = filledPrice;
    }
}