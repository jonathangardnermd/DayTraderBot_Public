namespace DayTradeBot.domains.tradeEngine.core;

using DayTradeBot.common.util.loggingUtil;
using DayTradeBot.common.constants;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
public class OrderAction : TabularEntity
{
    public enum ActionType
    {
        FilledOrder,
        FilledOrderAtCurrentPrice,
        PlacedOrder,
        CanceledOrder,
        Created,
        ZeroQuantity
    };

    public int SeqNum { get; set; }
    public DateOnly Dte { get; set; }
    public int RoundNum { get; set; }
    public ActionType Type { get; set; }
    public Order Order { get; set; } //CANNOT use the orders themselves, so should def make the Order object private. Just want to easily pick what data we want to use from the Order object.

    public bool IsFilledOrder
    {
        get
        {
            return Type == ActionType.FilledOrder || Type == ActionType.FilledOrderAtCurrentPrice;
        }
    }

    public static OrderStatus ActionTypeToOrderStatus(ActionType actionType)
    {
        if (actionType == ActionType.PlacedOrder)
        {
            return OrderStatus.Open;
        }
        if (actionType == ActionType.CanceledOrder)
        {
            return OrderStatus.Cancelled;
        }
        if (actionType == ActionType.Created)
        {
            return OrderStatus.NotPlacedYet;
        }
        if (actionType == ActionType.FilledOrderAtCurrentPrice || actionType == ActionType.FilledOrder)
        {
            return OrderStatus.Filled;
        }
        if (actionType == ActionType.ZeroQuantity)
        {
            return OrderStatus.ZeroQuantity;
        }
        throw new Exception($"unrecognized actionType={actionType}");
    }

    public override List<string> GetColumnValues(bool shouldTruncateGuids = false)
    {
        return new List<string>
        {
            this.Dte.ToString("yyyy-MM-dd"),
            this.RoundNum+"",
            this.Order.Symbol+"",
            this.Order.Direction.ToString(),
            this.Type.ToString(),
            this.Order.Status.ToString(),
            LoggingUtil.UsdToString(this.Order.LimitPrice),
            this.Order.OrderQuantity+"",
            LoggingUtil.UsdToString(this.Order.AvgFilledPrice),
            this.Order.FilledQuantity+"",
            LoggingUtil.UsdToString(this.Order.ParentOrder?.LimitPrice),
        };
    }

    public override List<TableColumn> GetColumnDefs()
    {
        return GetColumnDefinitons();
    }

    public static List<TableColumn> GetColumnDefinitons()
    {
        return new List<TableColumn>{
            new TableColumn{ColName = "Dte", ColWidth = 12},
            new TableColumn{ColName = "RoundNum", ColWidth = 9},
            new TableColumn{ColName = "Symbol", ColWidth = 12},
            new TableColumn{ColName = "Direction", ColWidth = 11},
            new TableColumn{ColName = "Action", ColWidth = 27},
            new TableColumn{ColName = "Status", ColWidth = 16},
            new TableColumn{ColName = "LimitPrice", ColWidth = 12},
            new TableColumn{ColName = "Qty", ColWidth = 6},
            new TableColumn{ColName = "FilledPrice", ColWidth = 12},
            new TableColumn{ColName = "FilledQty", ColWidth = 10},
            new TableColumn{ColName = "ParentLimitPrice", ColWidth = 20}
        };
    }

    public static IEnumerable<OrderAction> GetOrderFulfillments(List<OrderAction> allOrderActions)
    {
        var orderFulfillmentActions = allOrderActions.Where(orderAction =>
            orderAction.Type == OrderAction.ActionType.FilledOrder
            || orderAction.Type == OrderAction.ActionType.FilledOrderAtCurrentPrice
        );
        return orderFulfillmentActions;
    }
    public static string[] GetColHeaders()
    {
        return GetColumnDefinitons().Select(col => col.ColName).ToArray();
    }
}