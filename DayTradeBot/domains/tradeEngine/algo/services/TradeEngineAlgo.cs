namespace DayTradeBot.domains.tradeEngine.algo;

using DayTradeBot.contracts.externalApis.brokerageApi;
using DayTradeBot.contracts.externalApis.marketDataApi;
using DayTradeBot.common.constants;
using DayTradeBot.common.util.loggingUtil;
using DayTradeBot.domains.tradeEngine.core;

/*
Implement the specifics of the trading algorithm you choose to use.
*/
public class TradeEngineAlgo : TradeEngineBase
{
    private Dictionary<string, AlgoParams> AlgoParamsBySymbol { get; set; }
    public TradeEngineAlgo(IMarketDataApiService marketDataApi, IBrokerageApiService brokerageApi, List<string> symbolsToTrade, DateOnly today, Dictionary<string, AlgoParams> algoParamsBySymbol)
        : base(marketDataApi, brokerageApi, symbolsToTrade, today)
    {
        AlgoParamsBySymbol = algoParamsBySymbol;
    }

    public override int GetMaxNumOpenPrimaryBuys()
    {
        return 2;
    }

    protected override async Task HandleInitialBuysForPosition(Position pos)
    {
        var buyOrders = new List<BuyOrder>();

        float basisPrice = pos.GetBasisPrice() ?? 0;

        foreach (var data in AlgoParamsBySymbol[pos.Symbol].PlannedPrimaryBuyDatas)
        {
            buyOrders.Add(new BuyOrder(pos, pos.Fi.Symbol, basisPrice, data.PctChange, data.MaxUsd));
        }

        if (buyOrders.Where(o => o.OrderQuantity == 0).Count() > 0)
        {
            var table = TabularEntity.GetTable(Order.GetColHeaders(), buyOrders);
            throw new Exception($"Cannot create initial buy with qty=0! maxUsdAmount may be less than the price of the FinancialInstrument.\n{table.ToString()}");
        }

        await AddNewOrdersToCurrentOrders(pos, buyOrders);
    }

    protected override async Task HandleFilledBuy(BuyOrder buyOrder, PriceUpdateRound priceUpdateRound)
    {
        var pos = buyOrder.Position;

        if (pos.GetCurrentPctChange() < AlgoParamsBySymbol[pos.Symbol].BreakevenSellData.PctChangeToTriggerBreakevenSell)
        {
            if (priceUpdateRound.LabelAsBreakEvenSellAtomic())
            {
                // the priceUpdateRound has not already been labeled as breakEven
                // therefore, perform the breakEven logic
                var lowestPricedBuyFilled = priceUpdateRound.FilledBuys.OrderBy(o => o.LimitPrice).First();
                await PlaceBreakEvenSell(pos, lowestPricedBuyFilled, priceUpdateRound);
                return;
            }
            else
            {
                // the breakEven logic has already been executed on this priceUpdateRound, 
                // so no need to handle any of the other buys. They've already been handled.
                return;
            }
        }

        CorrespondingOrdersPlan correspondingSellPlans;
        if (buyOrder.NumParents == 0)
        {
            if (buyOrder.PctChangeFromBasisPrice < AlgoParamsBySymbol[pos.Symbol].BigDropPrimaryCutoffPct)
            {
                // big pctChange drop, so sell after it rises a LOT
                correspondingSellPlans = AlgoParamsBySymbol[pos.Symbol].BigDropPrimaryCorrespondingSellsPlan;
            }
            else
            {
                // small pctChange drop, so sell after it rises a LITTLE
                correspondingSellPlans = AlgoParamsBySymbol[pos.Symbol].SmallDropPrimaryCorrespondingSellsPlan;
            }
        }
        else
        {
            // NOT primary buys. I.E. these are buys placed after sells were filled
            correspondingSellPlans = AlgoParamsBySymbol[pos.Symbol].NonPrimaryCorrespondingSellsPlan;
        }

        correspondingSellPlans.DistributeQuantity(buyOrder.FilledQuantity);

        var sellOrders = new List<SellOrder>();
        foreach (var plan in correspondingSellPlans.Plans)
        {
            var sellOrder = new SellOrder(pos, pos.Fi.Symbol, plan.PctChange, plan.DistributedQty, buyOrder);
            if (sellOrder.OrderQuantity > 0)
            {
                sellOrders.Add(sellOrder);
            }
            else
            {
                priceUpdateRound.NumZeroQuantityNonPrimarySells++;
                sellOrder.Status = OrderStatus.ZeroQuantity;
                await AddOrderAction(sellOrder, OrderAction.ActionType.ZeroQuantity);
            }
        }
        if (sellOrders.Sum(sell => sell.OrderQuantity) != buyOrder.FilledQuantity)
        {
            throw new Exception($"Distributed integers not adding up to {buyOrder.FilledQuantity}!");
        }

        await AddNewOrdersToCurrentOrders(pos, sellOrders);
        await PlaceOrders(sellOrders);

        priceUpdateRound.AddPlacedNonPrimarySells(sellOrders);
    }

    protected override async Task HandleFilledSell(SellOrder sellOrder, PriceUpdateRound priceUpdateRound)
    {
        var pos = sellOrder.Position;

        CorrespondingOrdersPlan correspondingBuyPlans = AlgoParamsBySymbol[pos.Symbol].CorrespondingBuysPlan;
        correspondingBuyPlans.DistributeQuantity(sellOrder.FilledQuantity);
        var buyOrders = new List<BuyOrder>();
        foreach (var plan in correspondingBuyPlans.Plans)
        {
            var buyOrder = new BuyOrder(pos, pos.Fi.Symbol, plan.PctChange, plan.DistributedQty, sellOrder);
            if (buyOrder.OrderQuantity > 0)
            {
                buyOrders.Add(buyOrder);
            }
            else
            {
                throw new Exception("Buy order with quantity=0 is not currently allowed");
            }
        }
        if (buyOrders.Sum(order => order.OrderQuantity) != sellOrder.FilledQuantity)
        {
            throw new Exception($"Distributed integers not adding up to {sellOrder.FilledQuantity}!");
        }

        await AddNewOrdersToCurrentOrders(pos, buyOrders);
        await PlaceOrders(buyOrders);

        priceUpdateRound.AddPlacedNonPrimaryBuys(buyOrders);
    }

    private async Task AddNewOrdersToCurrentOrders(Position pos, IEnumerable<Order> ordersToAdd)
    {
        var tasks = ordersToAdd.Select(o => this.AddOrderAction(o, OrderAction.ActionType.Created));
        await Task.WhenAll(tasks);
        pos.CurrentOrders.AddOrders(ordersToAdd);
    }

    private async Task PlaceBreakEvenSell(Position pos, BuyOrder parentBuyOrder, PriceUpdateRound priceUpdateRound)
    {
        var filledBuys = pos.FilledOrders.Buys;
        var filledSells = pos.FilledOrders.Sells;

        var buyQty = filledBuys.Sum(o => o.FilledQuantity);
        var buyAmt = filledBuys.Sum(o => o.GetFilledUsdAmount()) ?? 0;
        var sellQty = filledSells.Sum(o => o.FilledQuantity);
        var sellAmt = filledSells.Sum(o => o.GetFilledUsdAmount()) ?? 0;

        var netQty = buyQty - sellQty;
        var netAmt = buyAmt - sellAmt;

        float breakEvenPrice = (float)netAmt / netQty;

        var minAllowableBreakevenSellPrice = pos.Fi.CurrentBidPrice * (1 + AlgoParamsBySymbol[pos.Symbol].BreakevenSellData.MinPctChangeForSellPrice);
        breakEvenPrice = Math.Max(breakEvenPrice, minAllowableBreakevenSellPrice);

        if (breakEvenPrice < parentBuyOrder.AvgFilledPrice)
        {
            priceUpdateRound.DebugFlag = true;
        }

        var cancelledSells = await this.CancelAllOpenSells(pos);
        priceUpdateRound.AddCancelledSells(cancelledSells);

        var sellOrders = new List<SellOrder>{
            new SellOrder(pos, pos.Fi.Symbol, netQty, breakEvenPrice, parentBuyOrder)
        };

        await AddNewOrdersToCurrentOrders(pos, sellOrders);
        await PlaceOrders(sellOrders);
        priceUpdateRound.AddPlacedNonPrimarySells(sellOrders);
    }
}