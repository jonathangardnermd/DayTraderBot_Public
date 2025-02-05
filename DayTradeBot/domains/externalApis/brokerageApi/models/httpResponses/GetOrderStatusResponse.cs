namespace DayTradeBot.domains.externalApis.brokerageApi;

using DayTradeBot.common.constants;
using DayTradeBot.common.util.loggingUtil;
using DayTradeBot.domains.tradeEngine.core;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.

public class GetOrderStatusResponse : TabularEntity
{
    public Order LimitOrder { get; set; }
    public OrderStatusData Data { get; set; }

    public string? RawStatusFromResponse { get; set; } = "";
    public bool WasSuccessfulResp { get; set; }

    public override List<string> GetColumnValues(bool shouldTruncateGuids = false)
    {
        var vals = new List<string>();
        vals.AddRange(LimitOrder.GetColumnValues(shouldTruncateGuids));
        vals.AddRange(new List<string>{
            RawStatusFromResponse ?? "",
            LoggingUtil.UsdToString(Data.AvgFilledPrice),
            Data.FilledQuantity+"",
            LoggingUtil.DateTimeToString(Data.CreatedAtUtc),
            LoggingUtil.DateTimeToString(Data.FilledAtUtc),
            LoggingUtil.DateTimeToString(Data.CancelledAtUtc),
            WasSuccessfulResp.ToString()
        });
        return vals;
    }

    public override List<TableColumn> GetColumnDefs()
    {
        return GetColumnDefinitons();
    }

    public static List<TableColumn> GetColumnDefinitons()
    {
        var vals = new List<TableColumn>();
        vals.AddRange(Order.GetColumnDefinitons());
        vals.AddRange(new List<TableColumn> {
            new TableColumn { ColName = "RespStatus", ColWidth = 15 },
            new TableColumn { ColName = "RespFillPrice", ColWidth = 15 },
            new TableColumn { ColName = "RespFillQuantity", ColWidth = 20 },
            new TableColumn { ColName = "CreatedAtUtc", ColWidth = 25 },
            new TableColumn { ColName = "FilledAtUtc", ColWidth = 25 },
            new TableColumn { ColName = "CancelledAtUtc", ColWidth = 25 },
            new TableColumn { ColName = "Success", ColWidth = 8 }
        });
        return vals;
    }

    public static string[] GetColHeaders()
    {
        return GetColumnDefinitons().Select(col => col.ColName).ToArray();
    }
}

public class OrderStatusData
{
    public OrderStatus Status { get; set; }
    public float? AvgFilledPrice { get; set; }
    public int FilledQuantity { get; set; }

    public DateTime? CreatedAtUtc { get; set; }
    public DateTime? FilledAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }

}