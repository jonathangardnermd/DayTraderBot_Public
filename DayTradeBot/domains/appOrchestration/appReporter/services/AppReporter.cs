namespace DayTradeBot.domains.appOrchestration;

using ConsoleTables;
using DayTradeBot.common.constants;
using DayTradeBot.common.util.loggingUtil;
using DayTradeBot.domains.tradeEngine.core;

public class AppReporter
{
    public static void LogTableOfPriceUpdateRound(PriceUpdateRound priceUpdateRound, LogType logType)
    {
        var table = TabularEntity.GetTable(PriceUpdateRound.GetColHeaders(), new[] { priceUpdateRound });
        var tableStr = table.ToString();
        LoggingUtil.LogTo(logType, $"Price Update Round:\n{tableStr}");
    }

    public static void LogTableOfPositions(IEnumerable<Position> positions, LogType logType)
    {
        var table = TabularEntity.GetTable(Position.GetColHeaders(), positions);
        var tableStr = table.ToString();
        LoggingUtil.LogTo(logType, $"Current Positions\n{tableStr}");
    }

    public static void LogTableOfModifiedOrdersForPriceRound(PriceUpdateRound priceUpdateRound, LogType logType)
    {
        var modifiedOrders = priceUpdateRound.GetAllModifiedOrders();
        var table = TabularEntity.GetTable(Order.GetColHeaders(), modifiedOrders.OrderByDescending(o => o.Symbol).ThenBy(o => o.Status).ThenBy(o => o.LimitPrice));
        var tableStr = table.ToString();
        LoggingUtil.LogTo(logType, $"Modified Orders:\n{tableStr}");
    }

    public static void LogTableOfAllPrimaryOrders(Position pos, LogType logType)
    {
        var primaryOrders = pos.Orders.GetAllPrimaryOrders();
        var table = TabularEntity.GetTable(Order.GetColHeaders(), primaryOrders.OrderByDescending(o => o.LimitPrice));
        var tableStr = table.ToString();
        LoggingUtil.LogTo(logType, $"All primary orders for {pos.Symbol}:\n{tableStr}");
    }

    public static void LogTableOfAllFilledOrders(Position pos, LogType logType)
    {
        var filledOrders = pos.Orders.FilledOrders.GetAllOrders();
        var table = TabularEntity.GetTable(Order.GetColHeaders(), filledOrders.OrderByDescending(o => o.Direction).ThenByDescending(o => o.LimitPrice));
        var tableStr = table.ToString();
        LoggingUtil.LogTo(logType, $"All filled orders for {pos.Symbol}:\n{tableStr}");
    }

    public static float CalcTotalProfit(FilledOrderAggregations filledOrderAggregations)
    {
        float totProfit = 0.0f;
        var data = filledOrderAggregations.GetData();
        foreach (var symbol in filledOrderAggregations.GetSymbols())
        {
            var aggsForSymbol = data[symbol];
            var buyAmt = aggsForSymbol[OrderDirection.Buy].FilledUsdAmt;
            var sellAmt = aggsForSymbol[OrderDirection.Sell].FilledUsdAmt;
            totProfit += sellAmt - buyAmt;
        }
        return totProfit;
    }

    public static FilledOrderAggregations CalcFilledOrderData(OrderActions orderActions)
    {
        var datas = new FilledOrderAggregations();
        var orderActionsBySymbol = orderActions.GetData();

        foreach (var symbol in orderActionsBySymbol.Keys)
        {
            var orderActionsForSymbol = orderActionsBySymbol[symbol];

            int buyQty = 0, sellQty = 0, numBuysFilled = 0, numSellsFilled = 0;
            float buyAmt = 0, sellAmt = 0;

            if (orderActionsForSymbol.TryGetValue(OrderDirection.Buy, out var buyActions))
            {
                var filledBuyActions = buyActions.Where(oa => oa.IsFilledOrder);

                (buyQty, buyAmt, numBuysFilled) = filledBuyActions.Aggregate(
                    (qty: 0, amt: 0f, count: 0),
                    (acc, oa) => (
                        qty: acc.qty + oa.Order.FilledQuantity,
                        amt: acc.amt + (oa.Order.GetFilledUsdAmount() ?? 0),
                        count: acc.count + 1
                    )
                );

            }

            if (orderActionsForSymbol.TryGetValue(OrderDirection.Sell, out var sellActions))
            {
                var filledSellActions = sellActions.Where(oa => oa.IsFilledOrder);

                (sellQty, sellAmt, numSellsFilled) = filledSellActions.Aggregate(
                    (qty: 0, amt: 0f, count: 0),
                    (acc, oa) => (
                        qty: acc.qty + oa.Order.FilledQuantity,
                        amt: acc.amt + (oa.Order.GetFilledUsdAmount() ?? 0),
                        count: acc.count + 1
                    )
                );


            }

            // set the buy metrics
            var data = new FilledOrderAggregation
            {
                FilledUsdAmt = buyAmt,
                FilledQty = buyQty,
                NumOrdersFilledCt = numBuysFilled
            };
            datas.SetFilledOrderData(symbol, OrderDirection.Buy, data);

            // set the sell metrics
            data = new FilledOrderAggregation
            {
                FilledUsdAmt = sellAmt,
                FilledQty = sellQty,
                NumOrdersFilledCt = numSellsFilled
            };
            datas.SetFilledOrderData(symbol, OrderDirection.Sell, data);
        }

        return datas;
    }

    public static ConsoleTable GetTableOfPrimaryOrderGroups(Position pos)
    {
        var primaryGrping = PrimaryOrderGrouping.GetPrimaryOrderGroupFromOrders(pos.Orders.GetAllOrders());

        var rows = new List<PrimaryOrderGroupRow>();
        foreach (var primaryGrp in primaryGrping.OrdersByPrimaryGuid.Values)
        {
            var primaryOrder = primaryGrp.PrimaryOrder;
            rows.Add(new PrimaryOrderGroupRow
            {
                Symbol = pos.Symbol,
                PrimaryPctChange = primaryOrder.PctChangeFromBasisPrice,
                PrimaryStatus = primaryOrder.Status,
                PrimaryQty = primaryOrder.OrderQuantity,
                PrimaryPrice = primaryOrder.LimitPrice,
                PrimaryFillPrice = primaryOrder.AvgFilledPrice,
                PrimaryFilledQty = primaryOrder.FilledQuantity,
                FreeUsdPrimary = -1 * primaryOrder.GetFilledUsdAmount(),
                FreeUsdChildren = primaryGrp.GetNetFreeUsdAmt(),
                NumChildBuys = primaryGrp.Buys.Count,
                NumChildSells = primaryGrp.Sells.Count,
                ChildBoughtQty = primaryGrp.GetBuyOrders(OrderStatus.Filled).Sum(o => o.FilledQuantity),
                ChildSoldQty = primaryGrp.GetSellOrders(OrderStatus.Filled).Sum(o => o.FilledQuantity)
            });
        }

        var primaryOrders = pos.Orders.GetAllPrimaryOrders();
        var table = TabularEntity.GetTable(PrimaryOrderGroupRow.GetColHeaders(), rows.OrderByDescending(r => r.PrimaryPrice));
        return table;
    }

}

