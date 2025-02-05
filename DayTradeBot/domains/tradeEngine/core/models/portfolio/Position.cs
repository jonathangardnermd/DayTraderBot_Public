namespace DayTradeBot.domains.tradeEngine.core;


using DayTradeBot.common.util.loggingUtil;
using DayTradeBot.domains.externalApis.marketDataApi;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
[Serializable]
public class Position : TabularEntity
{
    public Guid Uid { get; set; }
    public FinancialInstrument Fi { get; set; }

    public string Symbol { get; set; }
    public Orders Orders { get; set; }

    public ClosePriceBar OrigClosePriceBar
    { get; set; }

    public CurrentOrders CurrentOrders
    {
        get { return Orders.CurrentOrders; }
    }

    public FilledOrders FilledOrders
    {
        get { return Orders.FilledOrders; }
    }


    public CancelledOrders CancelledOrders
    {
        get { return Orders.CancelledOrders; }
    }

    public Position(FinancialInstrument financialInstrument)
    {
        Uid = Guid.NewGuid();
        Fi = financialInstrument;
        Symbol = financialInstrument.Symbol;
        Orders = new Orders();
    }

    public Position()
    {
        Uid = Guid.NewGuid();
    }

    public float? GetCurrentPctChange()
    {
        var basisPrice = GetBasisPrice();
        return (Fi.CurrentBidPrice - basisPrice) / basisPrice;
    }

    public float GetPctChangeFromCurrPrice(PriceUpdateSocketData priceUpdate)
    {
        return (priceUpdate.CurrentBidPrice - Fi.CurrentBidPrice) / Fi.CurrentBidPrice;
    }

    public float? GetBasisPrice()
    {
        return OrigClosePriceBar.Close;
    }

    public string ToFormattedString()
    {
        string pctChangeStr = LoggingUtil.PctToString(GetCurrentPctChange());

        string currPriceStr = LoggingUtil.UsdToString(this.Fi.GetCurrentPrice());
        currPriceStr = this.Fi.HasPrice() ? $"${currPriceStr}" : "empty";

        string origClosePriceStr = LoggingUtil.PctToString(this.OrigClosePriceBar.Close);

        return $"Symbol={this.Fi.Symbol}, "
            + $"CurrPrice=${currPriceStr}, "
            + $"PctChange={pctChangeStr}, "
            + $"OrigClosePrice=${origClosePriceStr}, "
            + $"OrigClosePriceDte={this.OrigClosePriceBar.Dte:yyyy-MM-dd}, "
            ;
    }

    public override List<string> GetColumnValues(bool shouldTruncateGuids = false)
    {
        return new List<string>
        {
            this.Symbol,
            LoggingUtil.UsdToString(this.Fi.PrevBidPrice),
            LoggingUtil.UsdToString(this.Fi.CurrentBidPrice),
            LoggingUtil.PctToString(this.GetCurrentPctChange()),
            LoggingUtil.UsdToString(this.GetBasisPrice()),
            LoggingUtil.DteToString(this.OrigClosePriceBar.Dte),
            this.Fi.NumPriceUpdatesToday+"",
            LoggingUtil.UsdToString(this.CurrentOrders.ClosestOpenBuyOrder?.LimitPrice),
            LoggingUtil.UsdToString(this.CurrentOrders.ClosestOpenSellOrder?.LimitPrice),
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
            new TableColumn{ColName = "PrevPrice", ColWidth = 11},
            new TableColumn{ColName = "CurrPrice", ColWidth = 10},
            new TableColumn{ColName = "CurrPctFromBasis", ColWidth = 10},
            new TableColumn{ColName = "BasisPrice", ColWidth = 6},
            new TableColumn{ColName = "PosBasisDte", ColWidth = 11},
            new TableColumn{ColName = "NumUpdatesSoFar", ColWidth = 16},
            new TableColumn{ColName = "ClosestBuyPrice", ColWidth = 20},
            new TableColumn{ColName = "ClosestSellPrice", ColWidth = 13},
        };
    }

    public static string[] GetColHeaders()
    {
        return GetColumnDefinitons().Select(col => col.ColName).ToArray();
    }
}