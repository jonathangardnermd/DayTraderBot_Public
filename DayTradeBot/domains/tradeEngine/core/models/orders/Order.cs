namespace DayTradeBot.domains.tradeEngine.core;

using System.Text.Json.Serialization;
using DayTradeBot.common.constants;
using DayTradeBot.common.util.loggingUtil;

[JsonDerivedType(typeof(Order), typeDiscriminator: "base")]
[JsonDerivedType(typeof(BuyOrder), typeDiscriminator: "buy")]
[JsonDerivedType(typeof(SellOrder), typeDiscriminator: "sell")]

#pragma warning disable CS8618 // uninitialized after constructor
public class Order : TabularEntity
{
    public Guid Uid { get; set; }
    public string? ClientOrderId { get; set; }
    public Guid? OrderId { get; set; }
    public OrderStatus Status { get; set; }

    public Position Position { get; set; }
    public OrderDirection Direction { get; set; }
    public string Symbol { get; set; }
    public int OrderQuantity { get; set; }
    public float LimitPrice { get; set; }
    public Order? ParentOrder { get; set; }

    public Order PrimaryOrder { get; set; }

    public DateTime PlacedTimeStamp { get; set; }

    public DateTime? CompletedTimeStamp { get; set; }
    public float? AvgFilledPrice { get; set; }
    public int FilledQuantity { get; set; }

    public float BasisPrice { get; set; }
    public float PctChangeFromBasisPrice { get; set; }

    public float LockedUsdAmt
    {
        get { return LimitPrice * OrderQuantity; }
    }

    public float? ParentLimitPrice
    {
        get
        {
            if (this.ParentOrder == null)
            {
                return null;
            }
            return this.ParentOrder.LimitPrice;
        }
    }

    public int NumParents { get; set; }

    public Order(Position position, OrderDirection direction, string symbol, float basisPrice, float pctChange, Order? parentOrder)
    {
        Uid = Guid.NewGuid();
        Position = position;
        ParentOrder = parentOrder;

        if (parentOrder == null)
        {
            // this IS a primary order, so this is ITS OWN primary order
            PrimaryOrder = this;
        }
        else
        {
            PrimaryOrder = parentOrder.PrimaryOrder;
        }

        NumParents = ParentOrder == null ? 0 : (ParentOrder.NumParents + 1);

        Direction = direction;
        Symbol = symbol;
        Status = OrderStatus.NotPlacedYet;

        LimitPrice = basisPrice * (1 + pctChange);
        LimitPrice = (float)Math.Round(LimitPrice, 2);
        BasisPrice = basisPrice;
        PctChangeFromBasisPrice = pctChange;
    }

    public Order()
    {
        Uid = Guid.NewGuid();
    }


    public DateOnly? GetFilledDte()
    {
        if (CompletedTimeStamp == null)
        {
            return null;
        }
        else
        {
            return DateOnly.FromDateTime(CompletedTimeStamp.GetValueOrDefault());
        }
    }

    public float? GetFilledUsdAmount()
    {
        if (AvgFilledPrice == null)
        {
            return null;
        }
        return AvgFilledPrice * FilledQuantity;
    }



    public override List<string> GetColumnValues(bool shouldTruncateGuids = false)
    {
        var clientOrderId = LoggingUtil.GetGuidString(this.ClientOrderId, shouldTruncateGuids);
        var orderId = LoggingUtil.GetGuidString(this.OrderId, shouldTruncateGuids);
        var positionUid = LoggingUtil.GetGuidString(this.Position.Uid, shouldTruncateGuids);

        string specialType = "--";
        if (this is BuyOrder)
        {
            var buy = (BuyOrder)this;
            if (buy.IsImmediateFillBuy)
            {
                specialType = "ImmedFill";
            }
        }
        else if (this is SellOrder)
        {
            var buy = (SellOrder)this;
            if (buy.IsBreakEvenSell)
            {
                specialType = "BreakEven";
            }
        }
        return new List<string>
        {
            this.Symbol,
            this.Direction.ToString(),
            this.Status.ToString(),
            this.NumParents+"",
            this.OrderQuantity+"",
            LoggingUtil.UsdToString(this.LimitPrice),
            LoggingUtil.UsdToString(this.ParentLimitPrice),
            LoggingUtil.PctToString(this.PctChangeFromBasisPrice),
            LoggingUtil.UsdToString(this.BasisPrice),
            clientOrderId,
            orderId,
            specialType,
            LoggingUtil.UsdToString(this.AvgFilledPrice),
            this.FilledQuantity+"",
            positionUid,
        };
    }
    public override List<TableColumn> GetColumnDefs()
    {
        return GetColumnDefinitons();
    }

    public static List<TableColumn> GetColumnDefinitons()
    {
        return new List<TableColumn>{
            new TableColumn{ColName = "Symbol", ColWidth = 10},
            new TableColumn{ColName = "Direction", ColWidth = 11},
            new TableColumn{ColName = "Status", ColWidth = 15},
            new TableColumn{ColName = "NumParents", ColWidth = 12},
            new TableColumn{ColName = "Qty", ColWidth = 6},
            new TableColumn{ColName = "LimitPrice", ColWidth = 11},
            new TableColumn{ColName = "ParentLimitPrice", ColWidth = 16},
            new TableColumn{ColName = "PctChangeFromBasis", ColWidth = 20},
            new TableColumn{ColName = "BasisPrice", ColWidth = 13},
            new TableColumn{ColName = "ClientOrderId", ColWidth = 45},
            new TableColumn{ColName = "OrderId", ColWidth = 45},
            new TableColumn{ColName = "SpecialType", ColWidth = 16},
            new TableColumn{ColName = "FilledPrice", ColWidth = 11},
            new TableColumn{ColName = "FilledQty", ColWidth = 11},
            new TableColumn{ColName = "PositionUid", ColWidth = 45},
        };
    }
    public static string[] GetColHeaders()
    {
        return GetColumnDefinitons().Select(col => col.ColName).ToArray();
    }
}