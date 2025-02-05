namespace DayTradeBot.domains.tradeEngine.algo;

using DayTradeBot.common.constants;
using DayTradeBot.common.util.mathUtil;
using DayTradeBot.common.util.loggingUtil;
using DayTradeBot.domains.tradeEngine.core;

# pragma warning disable CS8618 // Non-nullable field is uninitialized.

public class AlgoParamsGenerator
{
    public static AlgoParams CreateData_Version_Big_Bear()
    {
        var qtyPcts = new float[] { 1f };
        var SHIFT = -.5f;
        return new AlgoParams
        {
            PlannedPrimaryBuyDatas = new List<PrimaryBuyOrderData>
            {
                new PrimaryBuyOrderData(0.0f  + SHIFT, 4000 * 1.2f),
                new PrimaryBuyOrderData(-.01f  + SHIFT, 8000 * 1.2f),
                new PrimaryBuyOrderData(-.02f  + SHIFT, 16000 * 1.2f),
                new PrimaryBuyOrderData(-.03f  + SHIFT, 32000 * 1.2f),
            },
            SmallDropPrimaryCorrespondingSellsPlan =
                CorrespondingOrdersPlan.Create(qtyPcts, new float[] { .0025f }),

            BigDropPrimaryCorrespondingSellsPlan =
                CorrespondingOrdersPlan.Create(qtyPcts, new float[] { .005f }),

            CorrespondingBuysPlan =
                CorrespondingOrdersPlan.Create(qtyPcts, new float[] { -.015f }),

            NonPrimaryCorrespondingSellsPlan =
                CorrespondingOrdersPlan.Create(qtyPcts, new float[] { .0025f }),

            BreakevenSellData = new BreakevenSellData
            {
                PctChangeToTriggerBreakevenSell = .1f, // for the "big bear" params, place the break-even cutoff such that even the FIRST buy triggers break-even logic (ie its ALWAYS in break-even mode). 
                MinPctChangeForSellPrice = .0025f
            },
            BigDropPrimaryCutoffPct = -.02f
        };
    }
    public static AlgoParams CreateData_Version_Bear()
    {
        var qtyPcts = new float[] { 1f };
        var SHIFT = -.02f;
        return new AlgoParams
        {
            PlannedPrimaryBuyDatas = new List<PrimaryBuyOrderData>
            {
                new PrimaryBuyOrderData(0.0f + SHIFT, 4000 * 1.2f),
                new PrimaryBuyOrderData(-.025f + SHIFT, 8000 * 1.2f),
                new PrimaryBuyOrderData(-.05f + SHIFT, 16000 * 1.2f),
                new PrimaryBuyOrderData(-.075f + SHIFT, 32000 * 1.2f),
            },
            SmallDropPrimaryCorrespondingSellsPlan =
                CorrespondingOrdersPlan.Create(qtyPcts, new float[] { .01f }),

            BigDropPrimaryCorrespondingSellsPlan =
                CorrespondingOrdersPlan.Create(qtyPcts, new float[] { .015f }),

            CorrespondingBuysPlan =
                CorrespondingOrdersPlan.Create(qtyPcts, new float[] { -.05f }),

            NonPrimaryCorrespondingSellsPlan =
                CorrespondingOrdersPlan.Create(qtyPcts, new float[] { .005f }),

            BreakevenSellData = new BreakevenSellData
            {
                PctChangeToTriggerBreakevenSell = -.065f,
                MinPctChangeForSellPrice = .01f
            },
            BigDropPrimaryCutoffPct = -.04f
        };
    }

    public static AlgoParams CreateData_Version_Bull()
    {
        // float BREAKEVEN_POINT = -.075f;
        var qtyPcts = new float[] { 1f };
        float INITIAL_BUY = 10000f;
        return new AlgoParams
        {
            PlannedPrimaryBuyDatas = new List<PrimaryBuyOrderData> {
                new PrimaryBuyOrderData(0.01f, INITIAL_BUY * 1.2f),
                new PrimaryBuyOrderData(-.005f, 15000 * 1.2f),
                new PrimaryBuyOrderData(-.0075f, 25000 * 1.2f),
                new PrimaryBuyOrderData(-.01f, 35000 * 1.2f),
            },
            SmallDropPrimaryCorrespondingSellsPlan =
                CorrespondingOrdersPlan.Create(qtyPcts, new float[] { .005f }),

            BigDropPrimaryCorrespondingSellsPlan =
                CorrespondingOrdersPlan.Create(qtyPcts, new float[] { .01f }),

            CorrespondingBuysPlan =
                CorrespondingOrdersPlan.Create(qtyPcts, new float[] { -.005f }),

            NonPrimaryCorrespondingSellsPlan =
                CorrespondingOrdersPlan.Create(qtyPcts, new float[] { .005f }),

            BreakevenSellData = new BreakevenSellData
            {
                PctChangeToTriggerBreakevenSell = -.055f,
                MinPctChangeForSellPrice = .005f
            },
            BigDropPrimaryCutoffPct = -.04f
        };
    }

    public static AlgoParams CreateData_Version_Big_Bull()
    {
        // float BREAKEVEN_POINT = -.075f;
        var qtyPcts = new float[] { 1f };
        float INITIAL_BUY = 40000f;
        return new AlgoParams
        {
            PlannedPrimaryBuyDatas = new List<PrimaryBuyOrderData> {
                new PrimaryBuyOrderData(0.01f, INITIAL_BUY * 1.2f),
                new PrimaryBuyOrderData(-.0025f, 15000 * 1.2f),
                new PrimaryBuyOrderData(-.005f, 25000 * 1.2f),
                new PrimaryBuyOrderData(-.0075f, 35000 * 1.2f),
            },
            SmallDropPrimaryCorrespondingSellsPlan =
                CorrespondingOrdersPlan.Create(qtyPcts, new float[] { .01f }),

            BigDropPrimaryCorrespondingSellsPlan =
                CorrespondingOrdersPlan.Create(qtyPcts, new float[] { .01f }),

            CorrespondingBuysPlan =
                CorrespondingOrdersPlan.Create(qtyPcts, new float[] { -.005f }),

            NonPrimaryCorrespondingSellsPlan =
                CorrespondingOrdersPlan.Create(qtyPcts, new float[] { .01f }),

            BreakevenSellData = new BreakevenSellData
            {
                PctChangeToTriggerBreakevenSell = -.055f,
                MinPctChangeForSellPrice = .005f
            },
            BigDropPrimaryCutoffPct = -.04f
        };
    }
}

public class AlgoParams
{
    public List<PrimaryBuyOrderData> PlannedPrimaryBuyDatas { get; set; }
    public CorrespondingOrdersPlan NonPrimaryCorrespondingSellsPlan { get; set; }
    public CorrespondingOrdersPlan BigDropPrimaryCorrespondingSellsPlan { get; set; }
    public CorrespondingOrdersPlan SmallDropPrimaryCorrespondingSellsPlan { get; set; }
    public CorrespondingOrdersPlan CorrespondingBuysPlan { get; set; }
    public BreakevenSellData BreakevenSellData { get; set; }

    public float BigDropPrimaryCutoffPct { get; set; }

    public void PrintAlgorithmToLog(LogType logType, string versionStr = "")
    {
        LoggingUtil.LogTo(logType, "+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
        LoggingUtil.LogTo(logType, $"Trade Algorithm Print-out ({versionStr})");
        LoggingUtil.LogTo(logType, "+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");

        var table = TabularEntity.GetTable(PrimaryBuyOrderData.GetColHeaders(), PlannedPrimaryBuyDatas);
        LoggingUtil.LogTo(logType, $"\n{versionStr}\nPrimary buys will be placed in the following array:\n{table.ToString()}");

        table = TabularEntity.GetTable(CorrespondingOrderPlan.GetColHeaders(), SmallDropPrimaryCorrespondingSellsPlan.Plans);
        LoggingUtil.LogTo(logType, $"\n{versionStr}\nFor 'small drop' primary buys, the corresponding sells will be placed as follows:\n{table.ToString()}");

        table = TabularEntity.GetTable(CorrespondingOrderPlan.GetColHeaders(), BigDropPrimaryCorrespondingSellsPlan.Plans);
        LoggingUtil.LogTo(logType, $"\n{versionStr}\nFor 'big drop' primary buys, the corresponding sells will be placed as follows:\n{table.ToString()}");

        table = TabularEntity.GetTable(CorrespondingOrderPlan.GetColHeaders(), CorrespondingBuysPlan.Plans);
        LoggingUtil.LogTo(logType, $"\n{versionStr}\nFor sells, the corresponding buys will be placed as follows:\n{table.ToString()}");

        table = TabularEntity.GetTable(CorrespondingOrderPlan.GetColHeaders(), NonPrimaryCorrespondingSellsPlan.Plans);
        LoggingUtil.LogTo(logType, $"\n{versionStr}\nFor non-primary buys (placed due to a filled sell), the corresponding sells will be as follows:\n{table.ToString()}");

        table = TabularEntity.GetTable(BreakevenSellData.GetColHeaders(), new List<BreakevenSellData> { BreakevenSellData });
        LoggingUtil.LogTo(logType, $"\n{versionStr}\nWhen the price of an asset drops too much, 'Break-even' sells will be placed as follows:\n{table.ToString()}");
    }
}

public class BreakevenSellData : TabularEntity
{
    public float PctChangeToTriggerBreakevenSell { get; set; }
    public float MinPctChangeForSellPrice { get; set; }

    public override List<string> GetColumnValues(bool shouldTruncateGuids = false)
    {
        return new List<string>{
            LoggingUtil.PctToString(PctChangeToTriggerBreakevenSell),
            LoggingUtil.PctToString(MinPctChangeForSellPrice),
        };
    }

    public override List<TableColumn> GetColumnDefs()
    {
        return GetColumnDefinitons();
    }

    public static List<TableColumn> GetColumnDefinitons()
    {
        var vals = new List<TableColumn> {
            new TableColumn { ColName = "PctChangeToTriggerBreakevenSell", ColWidth = 15 },
            new TableColumn { ColName = "MinPctChangeForSellPrice", ColWidth = 15 },
        };
        return vals;
    }

    public static string[] GetColHeaders()
    {
        return GetColumnDefinitons().Select(col => col.ColName).ToArray();
    }
}

public class PrimaryBuyOrderData : TabularEntity
{
    public float PctChange { get; set; }
    public float MaxUsd { get; set; }

    public static Func<Position, PrimaryBuyOrderData>? GetPrimaryBuyOrderData { get; set; }
    public PrimaryBuyOrderData(float pctChange, float maxUsd)
    {
        PctChange = pctChange;
        MaxUsd = maxUsd;
    }

    public override List<string> GetColumnValues(bool shouldTruncateGuids = false)
    {
        return new List<string>{
            LoggingUtil.PctToString(PctChange),
            LoggingUtil.UsdToString(MaxUsd),
        };
    }

    public override List<TableColumn> GetColumnDefs()
    {
        return GetColumnDefinitons();
    }

    public static List<TableColumn> GetColumnDefinitons()
    {
        var vals = new List<TableColumn> {
            new TableColumn { ColName = "PctChange", ColWidth = 15 },
            new TableColumn { ColName = "MaxUsd", ColWidth = 15 },
        };
        return vals;
    }

    public static string[] GetColHeaders()
    {
        return GetColumnDefinitons().Select(col => col.ColName).ToArray();
    }
}

public class CorrespondingOrderPlan : TabularEntity
{
    public float QtyPct { get; set; }
    public float PctChange { get; set; }

    public int DistributedQty { get; set; }

    public override List<string> GetColumnValues(bool shouldTruncateGuids = false)
    {
        return new List<string>{
            QtyPct+"",
            LoggingUtil.PctToString(PctChange),
        };
    }

    public override List<TableColumn> GetColumnDefs()
    {
        return GetColumnDefinitons();
    }

    public static List<TableColumn> GetColumnDefinitons()
    {
        var vals = new List<TableColumn> {
            new TableColumn { ColName = "QtyPct", ColWidth = 15 },
            new TableColumn { ColName = "PctChange", ColWidth = 15 },
        };
        return vals;
    }

    public static string[] GetColHeaders()
    {
        return GetColumnDefinitons().Select(col => col.ColName).ToArray();
    }
}

public class CorrespondingOrdersPlan
{
    public List<CorrespondingOrderPlan> Plans { get; set; }


    public static CorrespondingOrdersPlan Create(float[] qtyPcts, float[] pctChanges)
    {
        var plans = qtyPcts.Zip(pctChanges, (qtyPct, pctChange) => new CorrespondingOrderPlan
        {
            QtyPct = qtyPct,
            PctChange = pctChange
        }).ToList();
        return new CorrespondingOrdersPlan
        {
            Plans = plans
        };
    }

    public float[] GetQtyPcts()
    {
        return Plans.Select(p => p.QtyPct).ToArray();
    }

    public void DistributeQuantity(int qtyToDistribute)
    {
        int[] distributedQtys = MathUtil.DistributeIntegers(GetQtyPcts(), qtyToDistribute);

        for (int i = 0; i < Plans.Count; i++)
        {
            var orderPlan = Plans[i];
            orderPlan.DistributedQty = distributedQtys[i];
        }
    }
}