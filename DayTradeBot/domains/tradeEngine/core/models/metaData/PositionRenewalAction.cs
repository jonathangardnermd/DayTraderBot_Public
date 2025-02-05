namespace DayTradeBot.domains.tradeEngine.core;

using DayTradeBot.common.util.loggingUtil;

# pragma warning disable CS8618 // Non-nullable field is uninitialized.
public class PositionRenewalAction : TabularEntity
{
    public int SeqNum { get; set; }
    public DateOnly Dte { get; set; }
    public Position Position { get; set; } //CANNOT use the positions themselves, so should def make the Order object private. Just want to easily pick what data we want to use from the Position object.

    public ActionType Type { get; set; }
    public enum ActionType
    {
        Renewal,
        Refresh,
    };

    public override List<string> GetColumnValues(bool shouldTruncateGuids = false)
    {
        var filledBuyOrders = this.Position.FilledOrders.Buys;
        var filledSellOrders = this.Position.FilledOrders.Sells;
        return new List<string>
        {
            this.Dte.ToString("yyyy-MM-dd"),
            this.Position.Symbol,
            this.Type.ToString(),
            this.Position.FilledOrders.Buys.Count+"",
            this.Position.FilledOrders.Sells.Count+"",
            LoggingUtil.UsdToString(this.Position.FilledOrders.Buys.Sum(o => o.GetFilledUsdAmount())),
            LoggingUtil.UsdToString(this.Position.FilledOrders.Sells.Sum(o => o.GetFilledUsdAmount()))
        };
    }

    public override List<TableColumn> GetColumnDefs()
    {
        return GetColumnDefinitons();
    }

    public static List<TableColumn> GetColumnDefinitons()
    {
        return new List<TableColumn>{
            new TableColumn{ColName = "RenewalDte", ColWidth = 12},
            new TableColumn{ColName = "Symbol", ColWidth = 9},
            new TableColumn{ColName = "ActionType", ColWidth = 12},
            new TableColumn{ColName = "NumFilledBuys", ColWidth = 15},
            new TableColumn{ColName = "NumFilledSells", ColWidth = 16},
            new TableColumn{ColName = "TotFilledBuysAmt", ColWidth = 18},
            new TableColumn{ColName = "TotFilledSellsAmt", ColWidth = 18},
        };
    }
    public static string[] GetColHeaders()
    {
        return GetColumnDefinitons().Select(col => col.ColName).ToArray();
    }
}