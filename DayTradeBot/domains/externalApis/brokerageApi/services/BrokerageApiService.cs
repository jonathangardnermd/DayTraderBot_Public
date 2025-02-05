
namespace DayTradeBot.domains.externalApis.brokerageApi;

using Alpaca.Markets;
using DayTradeBot.contracts.externalApis.brokerageApi;
using DayTradeBot.common.util.alpacaUtil;
using DayTradeBot.common.util.loggingUtil;
using DayTradeBot.common.util.exceptionUtil;
using DayTradeBot.common.settings;
using DayTradeBot.common.constants;
using DayTradeBot.domains.tradeEngine.core;

public class BrokerageApiService : IBrokerageApiService
{
    public IAlpacaTradingClient TradingClient { get; set; }
    public BrokerageApiService()
    {
        var apiKeys = AlpacaUtil.GetAlpacaApiKeys();
        TradingClient = AlpacaUtil.GetTradingClient();
    }

    public async Task<PlaceLimitOrderResponse> PlaceLimitBuyOrder(BuyOrder order)
    {
        var resp = new PlaceLimitOrderResponse
        {
            LimitOrder = order
        };

        try
        {
            if (order.OrderQuantity <= 0)
            {
                throw new Exception("Order quantity must be greater than zero!");
            }
            IOrder? alpacaOrder = null;

            alpacaOrder = await TradingClient.PostOrderAsync(
                    LimitOrder.Buy(order.Symbol, order.OrderQuantity, (decimal)order.LimitPrice)
                    .WithDuration(TimeInForce.Gtc));

            if (alpacaOrder == null)
            {
                throw new Exception($"Null response from alpaca when placing sell order: {order.ToLogRow()}");
            }

            order.ClientOrderId = alpacaOrder?.ClientOrderId;
            order.OrderId = alpacaOrder?.OrderId;
            resp.RawStatusFromResponse = alpacaOrder?.OrderStatus.ToString();

            var convertedStatus = ConvertAlpacaOrderStatus(alpacaOrder?.OrderStatus);
            resp.WasSuccessfulResp = convertedStatus == DayTradeBot.common.constants.OrderStatus.Open;
        }
        catch (Exception ex)
        {
            resp.WasSuccessfulResp = false;
            ExceptionUtil.AddException(ex);
        }
        finally
        {
            if (AppSettings.IsLogTypeOn(LogType.ApiPlaceOrder))
            {
                LoggingUtil.LogTo(LogType.ApiPlaceOrder, resp.ToLogRow());
            }
        }

        return resp;
    }

    public async Task<PlaceLimitOrderResponse> PlaceLimitSellOrder(SellOrder order)
    {
        var resp = new PlaceLimitOrderResponse
        {
            LimitOrder = order
        };

        try
        {
            if (order.OrderQuantity <= 0)
            {
                throw new Exception("Order quantity must be greater than zero!");
            }
            IOrder? alpacaOrder = null;

            alpacaOrder = await TradingClient.PostOrderAsync(
                    LimitOrder.Sell(order.Symbol, order.OrderQuantity, (decimal)order.LimitPrice)
                    .WithDuration(TimeInForce.Gtc));

            if (alpacaOrder == null)
            {
                throw new Exception($"Null response from alpaca when placing sell order: {order.ToLogRow()}");
            }
            order.ClientOrderId = alpacaOrder?.ClientOrderId;
            order.OrderId = alpacaOrder?.OrderId;
            resp.RawStatusFromResponse = alpacaOrder?.OrderStatus.ToString();

            var convertedStatus = ConvertAlpacaOrderStatus(alpacaOrder?.OrderStatus);
            resp.WasSuccessfulResp = convertedStatus == DayTradeBot.common.constants.OrderStatus.Open;
        }
        catch (Exception ex)
        {
            resp.WasSuccessfulResp = false;
            ExceptionUtil.AddException(ex);
        }
        finally
        {
            if (AppSettings.IsLogTypeOn(LogType.ApiPlaceOrder))
            {
                LoggingUtil.LogTo(LogType.ApiPlaceOrder, resp.ToLogRow());
            }
        }

        return resp;
    }

    public async Task<CancelOrderResponse> CancelOrder(Order order)
    {
        var resp = new CancelOrderResponse
        {
            Order = order
        };

        try
        {
            if (order.OrderId == null)
            {
                throw new Exception($"Cannot cancel order with null orderId: {order.ToLogRow()}");
            }
            resp.WasSuccessfulResp = await TradingClient.CancelOrderAsync((Guid)order.OrderId);
        }
        catch (Exception ex)
        {
            resp.WasSuccessfulResp = false;
            ExceptionUtil.AddException(ex);
        }
        finally
        {
            if (AppSettings.IsLogTypeOn(LogType.ApiCancelOrder))
            {
                LoggingUtil.LogTo(LogType.ApiCancelOrder, resp.ToLogRow());
            }
        }

        return resp;
    }

    public async Task<GetAccountDataResponse> GetAccountData()
    {
        var resp = new GetAccountDataResponse();
        try
        {
            IAccount? account = null;

            account = await TradingClient.GetAccountAsync();

            if (account == null)
            {
                throw new Exception("Account is null");
            }
            var buyingPower = account.BuyingPower ?? 0;
            resp.Data = new AccountData
            {
                FreeUsdBalance = (float)buyingPower
            };
            resp.WasSuccessfulResp = true;
        }
        catch (Exception ex)
        {
            resp.WasSuccessfulResp = false;
            ExceptionUtil.AddException(ex);
        }
        finally
        {
            if (AppSettings.IsLogTypeOn(LogType.ApiGetAccount))
            {
                LoggingUtil.LogTo(LogType.ApiGetAccount, resp.ToLogRow());
            }
        }

        return resp;
    }

    public async Task<GetOrderStatusResponse> GetOrderStatus(Order order)
    {
        var resp = new GetOrderStatusResponse
        {
            LimitOrder = order,
            Data = new OrderStatusData()
        };

        try
        {
            if (order.ClientOrderId == null)
            {
                throw new Exception("Order id cannot be null when getting order status?");
            }
            IOrder? alpacaOrder = null;

            alpacaOrder = await TradingClient.GetOrderAsync(order.ClientOrderId);

            if (alpacaOrder == null)
            {
                throw new Exception($"Alpaca response is null when getting the order status: {order.ToLogRow()}");
            }
            resp.RawStatusFromResponse = alpacaOrder.OrderStatus.ToString();
            resp.Data.Status = ConvertAlpacaOrderStatus(alpacaOrder.OrderStatus);
            resp.Data.AvgFilledPrice = (float?)alpacaOrder.AverageFillPrice;
            resp.Data.FilledQuantity = (int)alpacaOrder.FilledQuantity;

            resp.Data.CreatedAtUtc = alpacaOrder.CreatedAtUtc;
            resp.Data.CancelledAtUtc = alpacaOrder.CancelledAtUtc;
            resp.Data.FilledAtUtc = alpacaOrder.FilledAtUtc;
            resp.WasSuccessfulResp = true;
        }
        catch (Exception ex)
        {
            resp.WasSuccessfulResp = false;
            ExceptionUtil.AddException(ex);
        }
        finally
        {
            if (AppSettings.IsLogTypeOn(LogType.ApiGetOrderStatus))
            {
                LoggingUtil.LogTo(LogType.ApiGetOrderStatus, resp.ToLogRow());
            }
        }

        return resp;
    }

    private static DayTradeBot.common.constants.OrderStatus ConvertAlpacaOrderStatus(Alpaca.Markets.OrderStatus? status)
    {
        if (status == Alpaca.Markets.OrderStatus.Filled)
        {
            return DayTradeBot.common.constants.OrderStatus.Filled;
        }
        if (status == Alpaca.Markets.OrderStatus.Canceled || status == Alpaca.Markets.OrderStatus.PendingCancel)
        {
            return DayTradeBot.common.constants.OrderStatus.Cancelled;
        }
        if (status == Alpaca.Markets.OrderStatus.Accepted || status == Alpaca.Markets.OrderStatus.New || status == Alpaca.Markets.OrderStatus.PartiallyFilled || status == Alpaca.Markets.OrderStatus.PendingNew)
        {
            return DayTradeBot.common.constants.OrderStatus.Open;
        }
        throw new Exception($"Unrecognized Alpaca OrderStatus={status.ToString()}");
    }
}