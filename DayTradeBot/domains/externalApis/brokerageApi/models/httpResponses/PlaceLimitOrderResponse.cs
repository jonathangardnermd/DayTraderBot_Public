namespace DayTradeBot.domains.externalApis.brokerageApi;

using DayTradeBot.common.util.loggingUtil;
using DayTradeBot.domains.tradeEngine.core;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
public class PlaceLimitOrderResponse : TabularEntity
{
    public Order LimitOrder { get; set; }
    public string? RawStatusFromResponse { get; set; } = "";

    public bool WasSuccessfulResp { get; set; }
    public override List<string> GetColumnValues(bool shouldTruncateGuids = false)
    {
        var vals = new List<string>();
        vals.AddRange(LimitOrder.GetColumnValues(shouldTruncateGuids));

        vals.AddRange(new List<string>{
            RawStatusFromResponse ?? "",
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
        vals.AddRange(new List<TableColumn>
        {
            new TableColumn { ColName = "RespStatus", ColWidth = 15 },
            new TableColumn{ColName = "Success",ColWidth = 8}
        });
        return vals;
    }

    public static string[] GetColHeaders()
    {
        return GetColumnDefinitons().Select(col => col.ColName).ToArray();
    }
}