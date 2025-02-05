

namespace DayTradeBot.domains.tradeEngine.core;

using DayTradeBot.common.constants;

public class SellOrder : Order
{
    public bool IsBreakEvenSell { get; set; } = false;

    public SellOrder(Position position, string symbol, float pctChange, int quantity, BuyOrder parentOrder)
        : base(position, OrderDirection.Sell, symbol, parentOrder.LimitPrice, pctChange, parentOrder)
    {
        OrderQuantity = quantity;
    }

    public SellOrder(Position position, string symbol, int quantity, float limitPrice, BuyOrder parentOrder)
    : base(position, OrderDirection.Sell, symbol, limitPrice, pctChange: 0f, parentOrder)
    {
        OrderQuantity = quantity;
        IsBreakEvenSell = true;
    }

    public SellOrder() : base()
    {

    }
    public string ToFormattedString()
    {
        return $"{Direction.ToString()} Order for '{Symbol}': LimitPrice={LimitPrice}, Quantity={OrderQuantity}, PctChangeFromBasisPrice={PctChangeFromBasisPrice}, Status={Status}";
    }
}