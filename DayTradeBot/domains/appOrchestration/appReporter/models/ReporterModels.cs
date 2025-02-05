
namespace DayTradeBot.domains.appOrchestration;

using DayTradeBot.common.constants;
using DayTradeBot.common.util.dataStructureUtil;
using DayTradeBot.common.util.loggingUtil;

public class FilledOrderAggregations
{
    public Dictionary<string, Dictionary<OrderDirection, FilledOrderAggregation>> FilledOrderDataBySymbolAndDirection { get; set; } = new Dictionary<string, Dictionary<OrderDirection, FilledOrderAggregation>>();
    public Dictionary<string, Dictionary<OrderDirection, FilledOrderAggregation>> GetData()
    {
        return FilledOrderDataBySymbolAndDirection;
    }
    public void SetFilledOrderData(string symbol, OrderDirection direction, FilledOrderAggregation orderData)
    {
        DictOfDictsOfSingleValues<string, OrderDirection, FilledOrderAggregation>.SetInDictOfDictsOfSingleValues(FilledOrderDataBySymbolAndDirection, symbol, direction, orderData);
    }

    public FilledOrderAggregation GetFilledOrderAggregation(string symbol, OrderDirection direction)
    {
        return FilledOrderDataBySymbolAndDirection[symbol][direction];
    }

    public IEnumerable<string> GetSymbols()
    {
        return FilledOrderDataBySymbolAndDirection.Keys;
    }
}

public class FilledOrderAggregation
{
    public float FilledUsdAmt { get; set; }
    public int FilledQty { get; set; }
    public int NumOrdersFilledCt { get; set; }
}

public class PositionRunSummary : TabularEntity
{
    public string Symbol { get; set; } = "";
    public int NumPositionRefreshs { get; set; }
    public int NumPositionRenewals { get; set; }
    public int NumBuysFilled { get; set; }
    public int NumSellsFilled { get; set; }
    public int BuyQty { get; set; }
    public int SellQty { get; set; }

    public float BuyAmt { get; set; }
    public float SellAmt { get; set; }
    public float Profit { get; set; }

    public float StartPrice { get; set; }
    public float EndPrice { get; set; }

    public float PctChangeInPrice { get { return (EndPrice - StartPrice) / StartPrice; } }

    public override List<string> GetColumnValues(bool shouldTruncateGuids = false)
    {
        return new List<string>
        {
            this.Symbol,
            this.NumPositionRefreshs+"",
            this.NumPositionRenewals+"",
            this.NumBuysFilled+"",
            this.NumSellsFilled+"",
            this.BuyQty+"",
            this.SellQty+"",
            LoggingUtil.UsdToString(this.BuyAmt),
            LoggingUtil.UsdToString(this.SellAmt),
            LoggingUtil.UsdToString(this.Profit),
            LoggingUtil.UsdToString(this.StartPrice),
            LoggingUtil.UsdToString(this.EndPrice),
            LoggingUtil.PctToString(this.PctChangeInPrice),
        };
    }

    public override List<TableColumn> GetColumnDefs()
    {
        return GetColumnDefinitons();
    }

    public static List<TableColumn> GetColumnDefinitons()
    {
        return new List<TableColumn>{
            new TableColumn{ColName = "Symbol", ColWidth = 8},
            new TableColumn{ColName = "NumPositionRefreshs", ColWidth = 20},
            new TableColumn{ColName = "NumPositionRenewals", ColWidth = 20},
            new TableColumn{ColName = "NumBuysFilled", ColWidth = 15},
            new TableColumn{ColName = "NumSellsFilled", ColWidth = 15},
            new TableColumn{ColName = "BuyQty", ColWidth = 10},
            new TableColumn{ColName = "SellQty", ColWidth = 10},
            new TableColumn{ColName = "BuyAmt", ColWidth = 10},
            new TableColumn{ColName = "SellAmt", ColWidth = 10},
            new TableColumn{ColName = "Profit", ColWidth = 10},
            new TableColumn{ColName = "StartPrice", ColWidth = 14},
            new TableColumn{ColName = "EndPrice", ColWidth = 14},
            new TableColumn{ColName = "PctChangeInPrice", ColWidth = 18},
        };
    }
    public static string[] GetColHeaders()
    {
        return GetColumnDefinitons().Select(col => col.ColName).ToArray();
    }
}

public class PrimaryOrderGroupRow : TabularEntity
{
    public string Symbol { get; set; } = "";
    public float PrimaryPctChange { get; set; }
    public OrderStatus PrimaryStatus { get; set; }
    public float PrimaryPrice { get; set; }

    public int PrimaryQty { get; set; }

    public float? PrimaryFillPrice { get; set; }
    public int PrimaryFilledQty { get; set; }
    public float? FreeUsdPrimary { get; set; }
    public float? FreeUsdChildren { get; set; }
    public int NumChildBuys { get; set; }
    public int NumChildSells { get; set; }

    public int ChildSoldQty { get; set; }
    public int ChildBoughtQty { get; set; }

    public override List<string> GetColumnValues(bool shouldTruncateGuids = false)
    {
        return new List<string>
        {
            this.Symbol,
            LoggingUtil.PctToString(this.PrimaryPctChange),
            this.PrimaryStatus.ToString(),
            LoggingUtil.UsdToString(this.PrimaryPrice),
            this.PrimaryQty+"",
            LoggingUtil.UsdToString(this.PrimaryFillPrice),
            LoggingUtil.UsdToString(this.FreeUsdPrimary),
            LoggingUtil.UsdToString(this.FreeUsdChildren),
            LoggingUtil.UsdToString(FreeUsdChildren + FreeUsdPrimary),
            this.NumChildBuys + "",
            this.NumChildSells + "",
            "+"+this.PrimaryFilledQty + "",
            "-"+this.ChildSoldQty + "",
            "+"+this.ChildBoughtQty + "",
            (PrimaryFilledQty-ChildSoldQty+ChildBoughtQty) + "",
        };
    }

    public override List<TableColumn> GetColumnDefs()
    {
        return GetColumnDefinitons();
    }

    public static List<TableColumn> GetColumnDefinitons()
    {
        return new List<TableColumn>{
            new TableColumn{ColName = "Symbol", ColWidth = 8},
            new TableColumn{ColName = "PrimaryPctChange", ColWidth = 20},
            new TableColumn{ColName = "PrimaryStatus", ColWidth = 20},
            new TableColumn{ColName = "PrimaryPrice", ColWidth = 15},
            new TableColumn{ColName = "PrimaryQty", ColWidth = 15},
            new TableColumn{ColName = "PrimaryFillPrice", ColWidth = 10},
            new TableColumn{ColName = "FreeUsdPrimary", ColWidth = 10},
            new TableColumn{ColName = "FreeUsdChildren", ColWidth = 10},
            new TableColumn{ColName = "NetFreeUsd", ColWidth = 10},
            new TableColumn{ColName = "NumChildBuys", ColWidth = 10},
            new TableColumn{ColName = "NumChildSells", ColWidth = 10},
            new TableColumn{ColName = "PrimaryFilledQty", ColWidth = 10},
            new TableColumn{ColName = "ChildSoldQty", ColWidth = 10},
            new TableColumn{ColName = "ChildBoughtQty", ColWidth = 10},
            new TableColumn{ColName = "HoldingQty", ColWidth = 10},
        };
    }
    public static string[] GetColHeaders()
    {
        return GetColumnDefinitons().Select(col => col.ColName).ToArray();
    }
}