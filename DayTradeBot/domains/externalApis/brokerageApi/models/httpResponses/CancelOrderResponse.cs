namespace DayTradeBot.domains.externalApis.brokerageApi;

using DayTradeBot.common.util.loggingUtil;
using DayTradeBot.domains.tradeEngine.core;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
public class CancelOrderResponse : TabularEntity
{
    public Order Order { get; set; }

    public bool WasSuccessfulResp { get; set; }
    public override List<string> GetColumnValues(bool shouldTruncateGuids = false)
    {
        var vals = new List<string>();
        vals.AddRange(Order.GetColumnValues(shouldTruncateGuids));
        vals.Add(WasSuccessfulResp.ToString());
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
        vals.Add(
            new TableColumn
            {
                ColName = "Success",
                ColWidth = 8
            }
        );
        return vals;
    }

    public static string[] GetColHeaders()
    {
        return GetColumnDefinitons().Select(col => col.ColName).ToArray();
    }
}