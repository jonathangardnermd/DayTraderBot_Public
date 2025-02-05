namespace DayTradeBot.domains.appOrchestration;

using DayTradeBot.common.constants;
using DayTradeBot.common.settings;
using DayTradeBot.common.util.loggingUtil;
using DayTradeBot.domains.tradeEngine.core;

#pragma warning disable CS1998
public class AppValidator
{
    public static void ValidateFinalOrderActions(OrderActions orderActions)
    {
        var filledOrderActions = orderActions.Flatten().Where(oa => oa.IsFilledOrder);
        var filledBuys = filledOrderActions.Where(oa => oa.Order.Direction == OrderDirection.Buy);
        var filledSells = filledOrderActions.Where(oa => oa.Order.Direction == OrderDirection.Sell);

        var filledBuyQty = filledBuys.Sum(oa => oa.Order.FilledQuantity);
        var filledSellQty = filledSells.Sum(oa => oa.Order.FilledQuantity);

        if (filledBuyQty != filledSellQty)
        {
            LoggingUtil.LogTo(LogType.Error, $"filledBuyQty={filledBuyQty}, filledSellQty={filledSellQty}");
            throw new Exception("Filled buy qty is not equal to filled sell qty!");
        }

        // LoggingUtil.LogToDebug($"NumFilledBuys={filledBuys.Count()}, NumFilledSells={filledSells.Count()}, NumZeroQuantityOrders={orderActions.NumZeroQuantityOrders}");
        // if (filledBuys.Count() * 2 != (filledSells.Count() + orderActions.NumZeroQuantityOrders))
        // {
        //     throw new Exception("Number of filledBuys is inconsistent with number of filled sells and numZeroQty orders");
        // }
    }

    public static void ValidatePriceUpdateRound(TradeEngineBase engine, PriceUpdateRound priceUpdateRound)
    {
        if (AppSettings.RuntimeEnv != Env.Prod)
        {
            var numOpenOrdersValidation = ValidateNumOpenPrimaryOrders(engine);
            if (!numOpenOrdersValidation.IsValid)
            {
                string errors = string.Join("\n", numOpenOrdersValidation.Errors);
                throw new Exception($"PriceUpdateRound errors:\n{errors}");
            }
        }

        // if (!(priceUpdateRound.DidPlaceBreakEvenSell) &&
        //     priceUpdateRound.FilledBuys.Count * 2 != (priceUpdateRound.NonPrimaryPlacedSells.Count + priceUpdateRound.NumZeroQuantityNonPrimarySells))
        // {
        //     throw new Exception($"FilledBuys not placing corresponding sells!");
        // }
        if (priceUpdateRound.FilledSells.Count != priceUpdateRound.NonPrimaryPlacedBuys.Count)
        {
            throw new Exception($"FilledSells not placing corresponding buys!");
        }
    }

    public static ValidationResults ValidateNumOpenPrimaryOrders(TradeEngineBase Engine)
    {
        /*
        Ensure numOpenPrimaryOrders is <= max
        Ensure no symbol has more than one openPrimaryOrder
        */
        var positions = Engine.Positions.GetEnumerable().ToList();
        var openPrimaryBuysBySymbol = new Dictionary<string, List<BuyOrder>>();
        var errors = new List<string>();
        var openPrimaryBuys = new List<BuyOrder>();
        foreach (var position in positions)
        {
            var openPrimaryBuysForPosition = position.Orders.CurrentOrders.GetAllOpenPrimaryBuys().ToList();
            openPrimaryBuysBySymbol[position.Symbol] = openPrimaryBuysForPosition;

            if (openPrimaryBuysForPosition.Count > 1)
            {
                errors.Add($"Symbol has more than one openPrimary: {position.Symbol}");
            }
            openPrimaryBuys.AddRange(openPrimaryBuysForPosition);
        }

        int maxNumOpenPrimaryBuys = Engine.GetMaxNumOpenPrimaryBuys();
        if (Engine.HavePlacedFirstBuyOrders && openPrimaryBuys.Count < maxNumOpenPrimaryBuys)
        {
            // perform an extra check to ensure there is a valid reason why openPrimaryBuys.Count < maxNumOpenPrimaryBuys
            foreach (var position in positions)
            {
                var openPrimaryBuysForPosition = openPrimaryBuysBySymbol[position.Symbol];
                if (openPrimaryBuysForPosition.Count == 0)
                {
                    // this position does not currently have an open primary buy
                    var hasUnplacedPrimaryBuyOrder = position.Orders.CurrentOrders.Buys.Any(o => o.Status == OrderStatus.NotPlacedYet && o.NumParents == 0);
                    if (hasUnplacedPrimaryBuyOrder)
                    {
                        // this position does not currently have an open primary buy, but it has unplaced primary buys that should have been placed
                        errors.Add($"Less than {maxNumOpenPrimaryBuys} total openPrimary buys");
                        break;
                    }
                }
            }
        }

        return new ValidationResults
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
    public static bool IsSelfConsistentData(IEnumerable<OrderAction> orderActions, Dictionary<string, Position> positionsBySymbol, Dictionary<Guid, bool> oldPositionGuidMap)
    {
        // are the positions simply the cumulative effect of the individual orderActions? (i.e. right number of orders, correct limitPrices, statuses, etc?)
        var reconstructedOrders = new Dictionary<Guid, Order>();
        foreach (var orderAction in orderActions)
        {
            var posGuid = orderAction.Order.Position.Uid;
            if (oldPositionGuidMap.ContainsKey(posGuid))
            {
                // this is no longer a "current" position, and the corresponding orders will not appear in the Positions returned from the engine
                // therefore, we do not want to check to ensure that these orders are in the orders returned by the engine (since they should not be).
                continue;
            }
            var order = orderAction.Order;
            var uid = orderAction.Order.Uid;

            Order? reconstructedOrder = null;
            if (!reconstructedOrders.ContainsKey(uid))
            {
                reconstructedOrder = new Order();
                reconstructedOrders[uid] = reconstructedOrder;
                reconstructedOrder.LimitPrice = order.LimitPrice;
                reconstructedOrder.OrderQuantity = order.OrderQuantity;
                reconstructedOrder.Direction = order.Direction;
                reconstructedOrder.Symbol = order.Symbol;
                reconstructedOrder.Position = order.Position;
            }
            else
            {
                reconstructedOrder = reconstructedOrders[uid];

                if (reconstructedOrder.LimitPrice != order.LimitPrice)
                {
                    return false;
                }
                if (reconstructedOrder.OrderQuantity != order.OrderQuantity)
                {
                    return false;
                }
                if (reconstructedOrder.Direction != order.Direction)
                {
                    return false;
                }
                if (reconstructedOrder.Symbol != order.Symbol)
                {
                    return false;
                }
            }

            var orderStatus = OrderAction.ActionTypeToOrderStatus(orderAction.Type);
            reconstructedOrder.Status = orderStatus;
        }

        // now loop through orders in the position to ensure that they are consistent with the reconstructed orders

        var ordersFromPositions = new Dictionary<Guid, Order>();
        foreach (var symbol in positionsBySymbol.Keys)
        {
            var pos = positionsBySymbol[symbol];

            var allOrdersInPosition = pos.Orders.GetAllOrders();

            foreach (var order in allOrdersInPosition)
            {
                ordersFromPositions[order.Uid] = order;
            }
        }

        if (ordersFromPositions.Keys.Count != reconstructedOrders.Keys.Count)
        {
            return false;
        }

        foreach (var uid in ordersFromPositions.Keys)
        {
            if (!reconstructedOrders.ContainsKey(uid))
            {
                return false;
            }
            var reconstructedOrder = reconstructedOrders[uid];
            var orderFromPos = ordersFromPositions[uid];

            if (reconstructedOrder.LimitPrice != orderFromPos.LimitPrice)
            {
                return false;
            }
            if (reconstructedOrder.OrderQuantity != orderFromPos.OrderQuantity)
            {
                return false;
            }
            if (reconstructedOrder.Direction != orderFromPos.Direction)
            {
                return false;
            }
            if (reconstructedOrder.Symbol != orderFromPos.Symbol)
            {
                return false;
            }
            if (reconstructedOrder.Status != orderFromPos.Status)
            {
                return false;
            }
        }

        // if (AppSettings.IsLogTypeOn(LogType.Debug))
        // {
        //     var ordersFromPositionsTable = TabularEntity.GetTable(Order.GetColHeaders(), ordersFromPositions.Select(kvp => kvp.Value));
        //     var reconstructedOrdersTable = TabularEntity.GetTable(Order.GetColHeaders(), reconstructedOrders.Select(kvp => kvp.Value));

        //     LoggingUtil.LogTo(LogType.Debug, $"\nOrders From Position:\n{ordersFromPositionsTable}");
        //     LoggingUtil.LogTo(LogType.Debug, $"\nReconstructed Orders:\n{reconstructedOrdersTable}");
        // }

        return true;
    }

    private static bool AreEqualPositions(Position persistedPos, Position loadedPos)
    {
        if (persistedPos.CurrentOrders.Buys.Count() != loadedPos.CurrentOrders.Buys.Count())
        {
            return false;
        }
        if (persistedPos.CurrentOrders.Sells.Count() != loadedPos.CurrentOrders.Sells.Count())
        {
            return false;
        }
        if (persistedPos.FilledOrders.Buys.Count() != loadedPos.FilledOrders.Buys.Count())
        {
            return false;
        }
        if (persistedPos.FilledOrders.Sells.Count() != loadedPos.FilledOrders.Sells.Count())
        {
            return false;
        }
        return true;
    }

    private static bool HasUnplacedPrimaryBuyOrders(TradeEngineBase Engine)
    {
        foreach (var pos in Engine.Positions.GetEnumerable())
        {
            if (pos.Orders.CurrentOrders.GetNumOpenPrimaryBuys() > 0)
            {
                continue;
            }

            var hasUnplacedPrimaryBuyOrder = pos.Orders.CurrentOrders.Buys.Any(o => o.Status == OrderStatus.NotPlacedYet && o.NumParents == 0);
            if (hasUnplacedPrimaryBuyOrder)
            {
                return true;
            }
        }
        return false;
    }
}