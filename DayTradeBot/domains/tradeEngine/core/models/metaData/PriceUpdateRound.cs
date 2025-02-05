namespace DayTradeBot.domains.tradeEngine.core;

using DayTradeBot.common.util.loggingUtil;
using DayTradeBot.domains.externalApis.marketDataApi;

public class PriceUpdateRound : TabularEntity
{
    public bool DebugFlag { get; set; } = false;

    private int didPlaceBreakEvenSell = 0;
    public bool DidPlaceBreakEvenSell
    {
        get { return didPlaceBreakEvenSell == 1; }
        set { didPlaceBreakEvenSell = value ? 1 : 0; }
    }

    public DateOnly Today { get; set; }
    public int RoundNum { get; set; }
    public PriceUpdateSocketData PriceUpdate { get; set; }

    public float PrevBidPrice { get; set; }
    public float PrevAskPrice { get; set; }

    public List<BuyOrder> PrimaryPlacedBuys { get; set; } = new List<BuyOrder>();
    public List<BuyOrder> FilledBuys { get; set; } = new List<BuyOrder>();
    public List<SellOrder> FilledSells { get; set; } = new List<SellOrder>();
    public List<BuyOrder> NonPrimaryPlacedBuys { get; set; } = new List<BuyOrder>();
    public List<SellOrder> NonPrimaryPlacedSells { get; set; } = new List<SellOrder>();
    public List<SellOrder> CancelledSells { get; set; } = new List<SellOrder>();
    public List<BuyOrder> CancelledBuys { get; set; } = new List<BuyOrder>();

    public int NumZeroQuantityNonPrimarySells { get; set; } = 0;
    public string UpdatedSymbol
    {
        get
        {
            return PriceUpdate.Symbol;
        }
    }

    public PriceUpdateRound(DateOnly today, int roundNum, PriceUpdateSocketData priceUpdate, float prevBidPrice, float prevAskPrice)
    {
        Today = today;
        RoundNum = roundNum;
        PriceUpdate = priceUpdate;
        PrevBidPrice = prevBidPrice;
        PrevAskPrice = prevAskPrice;
    }

    public bool LabelAsBreakEvenSellAtomic()
    {
        // returns true if this priceUpdateRound has NOT been labeled as breakEven already. Otherwise returns false.
        // either way, didPlaceBreakEvenSell is updated to 1
        return Interlocked.Exchange(ref didPlaceBreakEvenSell, 1) == 0;
    }

    public void AddUnFilledImmediateFillBuys(List<BuyOrder> unfilledBuys)
    {
        PrimaryPlacedBuys.AddRange(unfilledBuys);
    }

    public void AddFilledBuys(List<BuyOrder> filledBuys)
    {
        FilledBuys.AddRange(filledBuys);
    }
    public void AddFilledSells(List<SellOrder> filledSells)
    {
        FilledSells.AddRange(filledSells);
    }
    public void AddPlacedNonPrimaryBuys(List<BuyOrder> nonPrimaryPlacedBuys)
    {
        NonPrimaryPlacedBuys.AddRange(nonPrimaryPlacedBuys);
    }
    public void AddPlacedNonPrimarySells(List<SellOrder> nonPrimaryPlacedSells)
    {
        NonPrimaryPlacedSells.AddRange(nonPrimaryPlacedSells);
    }

    public void AddPrimaryPlacedBuys(List<BuyOrder> primaryPlacedBuys)
    {
        PrimaryPlacedBuys.AddRange(primaryPlacedBuys);
    }

    public void AddCancelledSells(List<SellOrder> cancelledSells)
    {
        CancelledSells.AddRange(cancelledSells);
    }

    public void AddCancelledBuys(List<BuyOrder> cancelledBuys)
    {
        CancelledBuys.AddRange(cancelledBuys);
    }

    public IEnumerable<Order> GetAllModifiedOrders()
    {
        return (IEnumerable<Order>)FilledSells
            .Concat((IEnumerable<Order>)FilledBuys)
            .Concat((IEnumerable<Order>)NonPrimaryPlacedBuys)
            .Concat((IEnumerable<Order>)NonPrimaryPlacedSells)
            .Concat((IEnumerable<Order>)PrimaryPlacedBuys)
            .Concat((IEnumerable<Order>)CancelledSells)
            .Concat((IEnumerable<Order>)CancelledBuys)
            ;
    }

    public bool DidModifyOrdersThatChangeFreeBal()
    {
        if (FilledSells.Count > 0
            || PrimaryPlacedBuys.Count > 0
            || NonPrimaryPlacedBuys.Count > 0
            || CancelledBuys.Count > 0)
        {
            return true;
        }
        return false;
    }
    public override List<string> GetColumnValues(bool shouldTruncateGuids = false)
    {
        return new List<string>
        {
            this.Today.ToString("yyyy-MM-dd"),
            this.PriceUpdate.TimeOfDay.ToString("hh\\:mm\\:ss"),
            this.RoundNum+"",
            this.UpdatedSymbol,
            LoggingUtil.UsdToString(this.PrevBidPrice),
            LoggingUtil.UsdToString(PriceUpdate.CurrentBidPrice),
            LoggingUtil.UsdToString(this.PrevAskPrice),
            LoggingUtil.UsdToString(PriceUpdate.CurrentAskPrice),
        };
    }

    public override List<TableColumn> GetColumnDefs()
    {
        return GetColumnDefinitons();
    }

    public static List<TableColumn> GetColumnDefinitons()
    {
        return new List<TableColumn>{
            new TableColumn{ColName = "Today", ColWidth = 12},
            new TableColumn{ColName = "Time", ColWidth = 12},
            new TableColumn{ColName = "RoundNum", ColWidth = 10},
            new TableColumn{ColName = "SymbolUpdated", ColWidth = 15},
            new TableColumn{ColName = "BidPriceBefore", ColWidth = 12},
            new TableColumn{ColName = "BidPriceAfter", ColWidth = 12},
            new TableColumn{ColName = "AskPriceBefore", ColWidth = 12},
            new TableColumn{ColName = "AskPriceAfter", ColWidth = 12},
        };
    }

    public static string[] GetColHeaders()
    {
        return GetColumnDefinitons().Select(col => col.ColName).ToArray();
    }
}