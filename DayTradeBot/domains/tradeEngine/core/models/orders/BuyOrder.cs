namespace DayTradeBot.domains.tradeEngine.core;

using DayTradeBot.common.constants;

public class BuyOrder : Order
{
    public float MaxUsdAmount { get; set; }

    public bool IsImmediateFillBuy { get; set; } = false;

    public BuyOrder(Position position, string symbol, float basisPrice, float pctChange, float maxUsdAmount, SellOrder? parentOrder = null)
        : base(position, OrderDirection.Buy, symbol, basisPrice, pctChange, parentOrder)
    {
        OrderQuantity = (int)Math.Floor(maxUsdAmount / LimitPrice);
        MaxUsdAmount = maxUsdAmount;
    }

    public BuyOrder(Position position, string symbol, float pctChange, int quantity, SellOrder parentOrder)
        : base(position, OrderDirection.Buy, symbol, parentOrder.LimitPrice, pctChange, parentOrder)
    {
        OrderQuantity = quantity;
    }

    public BuyOrder() : base()
    {

    }

    public bool IsOpenPrimaryBuy()
    {
        if (Status != OrderStatus.Open)
        {
            return false;
        }
        if (NumParents != 0)
        {
            // not a primary order
            return false;
        }
        return true;
    }

    public bool IsPrimary()
    {
        if (NumParents != 0)
        {
            // not a primary order
            return false;
        }
        return true;
    }
}