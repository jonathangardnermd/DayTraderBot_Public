namespace DayTradeBot.domains.tradeEngine.core;

using DayTradeBot.common.constants;
using DayTradeBot.domains.externalApis.brokerageApi;

# pragma warning disable CS8618 // Non-nullable field is uninitialized.
[Serializable]
public class Orders
{
    public CurrentOrders CurrentOrders { get; set; } = new CurrentOrders();
    public FilledOrders FilledOrders { get; set; } = new FilledOrders();
    public CancelledOrders CancelledOrders { get; set; } = new CancelledOrders();

    public Orders()
    {
    }

    public IEnumerable<Order> GetAllOrders()
    {
        return Enumerable.Concat<Order>(
            CurrentOrders.Buys,
            CurrentOrders.Sells
        )
        .Concat(FilledOrders.Buys)
        .Concat(FilledOrders.Sells)
        .Concat(CancelledOrders.Buys)
        .Concat(CancelledOrders.Sells);
    }

    public IEnumerable<BuyOrder> GetAllPrimaryOrders()
    {
        return this.GetAllOrders()
            .OfType<BuyOrder>()
            .Where(o => o.IsPrimary());
    }

    public void MarkAsPlaced(Order order)
    {
        if (order.Status != OrderStatus.NotPlacedYet)
        {
            throw new Exception($"Only 'NotPlacedYet' orders can be placed! {order.ToLogRow()}");
        }
        order.Status = OrderStatus.Open;
        order.PlacedTimeStamp = DateTime.Now;
    }

    public void MarkAsCancelled(Order order)
    {
        if (order.Status != OrderStatus.Open)
        {
            throw new Exception("Only 'Open' orders can be Cancelled!");
        }
        order.Status = OrderStatus.Cancelled;
        order.CompletedTimeStamp = DateTime.Now;

        // update the collections
        this.CurrentOrders.RemoveOrder(order);
        this.CancelledOrders.AddOrder(order);
    }

    public void MarkAsFilled(Order order, OrderStatusData orderStatusData)
    {
        if (order.Status != OrderStatus.Open)
        {
            throw new Exception("Only 'Open' orders can be filled!");
        }
        order.Status = OrderStatus.Filled;
        order.FilledQuantity = orderStatusData.FilledQuantity;
        order.AvgFilledPrice = orderStatusData.AvgFilledPrice;
        order.CompletedTimeStamp = DateTime.Now;

        // update the collections
        this.CurrentOrders.RemoveOrder(order);
        this.FilledOrders.AddOrder(order);
    }
}

[Serializable]
public class CurrentOrders : OrderList
{
    public BuyOrder? ClosestOpenBuyOrder { get; set; }
    public SellOrder? ClosestOpenSellOrder { get; set; }

    public CurrentOrders()
    {
    }

    public void Update()
    {
        UpdateBuys();
        UpdateSells();
    }

    public void UpdateBuys()
    {
        Buys = Buys.OrderByDescending(order => order.LimitPrice).ToList();
        UpdateClosestOpenBuyOrder();
    }

    public void UpdateSells()
    {
        Sells = Sells.OrderBy(order => order.LimitPrice).ToList();
        UpdateClosestOpenSellOrder();
    }

    private void UpdateClosestOpenBuyOrder()
    {
        ClosestOpenBuyOrder = (BuyOrder?)Buys.FirstOrDefault(
            order => order.Status == OrderStatus.Open
                && order.Direction == OrderDirection.Buy);
    }

    private void UpdateClosestOpenSellOrder()
    {
        ClosestOpenSellOrder = (SellOrder?)Sells.FirstOrDefault(
            order => order.Status == OrderStatus.Open
                && order.Direction == OrderDirection.Sell);
    }

    public bool didCrossPriceOfClosestOpenBuyOrder(float price)
    {
        if (ClosestOpenBuyOrder == null)
        {
            return false;
        }
        return ClosestOpenBuyOrder.LimitPrice > price;
    }

    public bool didCrossPriceOfClosestOpenSellOrder(float price)
    {
        if (ClosestOpenSellOrder == null)
        {
            return false;
        }
        return ClosestOpenSellOrder.LimitPrice < price;
    }

    public IEnumerable<BuyOrder> GetAllOpenPrimaryBuys()
    {
        return this.GetBuyOrders()
            .Where(o => o.IsOpenPrimaryBuy());
    }

    public IEnumerable<BuyOrder> GetAllPrimaryOrders()
    {
        return this.GetBuyOrders()
            .Where(o => o.IsPrimary());
    }

    public int GetNumOpenPrimaryBuys()
    {
        return GetAllOpenPrimaryBuys().Count();
    }

    public IEnumerable<SellOrder> GetOpenSellOrdersBelowPrice(float price)
    {
        var openSells = GetSellOrders(OrderStatus.Open);
        var sellOrdersAbovePrice = openSells
            .Where(order => order.LimitPrice < price);
        return sellOrdersAbovePrice;
    }

    public IEnumerable<BuyOrder> GetOpenBuyOrdersAbovePrice(float price)
    {
        var openBuys = GetBuyOrders(OrderStatus.Open);
        var buyOrdersAbovePrice = openBuys
            .Where(order => order.LimitPrice > price);
        return buyOrdersAbovePrice;
    }

    public BuyOrder? GetNextPrimaryBuyToPlace()
    {
        if (GetNumOpenPrimaryBuys() > 0)
        {
            // since we already have an open buy order, no need to place a new one
            return null;
        }
        return GetBuyOrders(OrderStatus.NotPlacedYet).FirstOrDefault();
    }

    public IEnumerable<BuyOrder> GetImmediateFillBuys(float price)
    {
        if (price == 0)
        {
            throw new ArgumentException("cannot pass price=$0.00 to this function");
        }
        return GetBuyOrders(OrderStatus.NotPlacedYet).Where(order => order.LimitPrice > price);
    }
}

[Serializable]
public class FilledOrders : OrderList
{
    public FilledOrders()
    {
    }
}


[Serializable]
public class CancelledOrders : OrderList
{
    public CancelledOrders()
    {
    }
}



public class OrderList
{
    public List<BuyOrder> Buys { get; set; } = new List<BuyOrder>();
    public List<SellOrder> Sells { get; set; } = new List<SellOrder>();

    public OrderList()
    {
    }

    public int GetHoldingQty()
    {
        int qty = 0;
        qty -= Sells.Where(o => o.Status == OrderStatus.Filled).Sum(o => o.FilledQuantity);
        qty += Buys.Where(o => o.Status == OrderStatus.Filled).Sum(o => o.FilledQuantity);
        return qty;
    }

    public float GetNetFreeUsdAmt()
    {
        float amt = 0;
        amt += Sells.Where(o => o.Status == OrderStatus.Filled).Sum(o => o.GetFilledUsdAmount()) ?? 0;
        amt -= Buys.Where(o => o.Status == OrderStatus.Filled).Sum(o => o.GetFilledUsdAmount()) ?? 0;
        return amt;
    }
    public List<Order> GetAllOrders()
    {
        var lst = new List<Order>();
        lst.AddRange(Buys);
        lst.AddRange(Sells);
        return lst;
    }
    public void AddBuyOrder(BuyOrder order)
    {
        Buys.Add(order);
    }

    public void AddSellOrder(SellOrder order)
    {
        Sells.Add(order);
    }

    public void AddOrder(Order order)
    {
        if (order.Direction == OrderDirection.Buy)
        {
            AddBuyOrder((BuyOrder)order);
        }
        else if (order.Direction == OrderDirection.Sell)
        {
            AddSellOrder((SellOrder)order);
        }
        else
        {
            throw new ArgumentException($"Unrecognized order direction: {order.Direction}");
        }
    }

    public void AddOrders(IEnumerable<Order> ordersToAdd)
    {
        foreach (var order in ordersToAdd)
        {
            AddOrder(order);
        }
    }

    public void RemoveBuyOrder(BuyOrder buyOrder)
    {
        var buys = (List<BuyOrder>)Buys;
        bool wasFound = buys.Remove(buyOrder);
        if (!wasFound)
        {
            throw new ArgumentException("Order was not found in the order list, so it was not removed!");
        }
    }

    public void RemoveSellOrder(SellOrder sellOrder)
    {
        var sells = (List<SellOrder>)Sells;
        bool wasFound = sells.Remove(sellOrder);
        if (!wasFound)
        {
            throw new ArgumentException("Order was not found in the order list, so it was not removed!");
        }
    }

    public void RemoveOrder(Order order)
    {
        if (order.Direction == OrderDirection.Buy)
        {
            RemoveBuyOrder((BuyOrder)order);
        }
        else if (order.Direction == OrderDirection.Sell)
        {
            RemoveSellOrder((SellOrder)order);
        }
        else
        {
            throw new ArgumentException($"Unrecognized order direction: {order.Direction}");
        }
    }

    public IEnumerable<BuyOrder> GetBuyOrders(OrderStatus? status = null)
    {
        if (status == null)
        {
            return this.Buys;
        }
        return this.Buys.Where(order => order.Status == status);
    }

    public IEnumerable<SellOrder> GetSellOrders(OrderStatus? status = null)
    {
        if (status == null)
        {
            return this.Sells;
        }
        return this.Sells.Where(order => order.Status == status);
    }
}

public class PrimaryOrderList : OrderList
{
    public BuyOrder PrimaryOrder { get; set; }
}

public class PrimaryOrderGrouping
{
    public Dictionary<Guid, PrimaryOrderList> OrdersByPrimaryGuid { get; set; } = new Dictionary<Guid, PrimaryOrderList>();

    public static PrimaryOrderGrouping GetPrimaryOrderGroupFromOrders(IEnumerable<Order> allOrders)
    {
        var grping = new PrimaryOrderGrouping();

        foreach (var order in allOrders)
        {
            var primaryOrder = order.PrimaryOrder;
            var primaryUid = primaryOrder.Uid;
            if (!grping.OrdersByPrimaryGuid.TryGetValue(primaryUid, out PrimaryOrderList? primaryOrderList))
            {
                primaryOrderList = new PrimaryOrderList { PrimaryOrder = (BuyOrder)primaryOrder };
                grping.OrdersByPrimaryGuid[primaryUid] = primaryOrderList;
            }
            if (order.ParentOrder != null)
            {
                // not a primary order
                primaryOrderList.AddOrder(order);
            }
        }

        return grping;
    }
}