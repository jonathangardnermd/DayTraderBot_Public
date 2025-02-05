namespace DayTradeBot.domains.tradeEngine.core;

using DayTradeBot.contracts.externalApis.marketDataApi;
using DayTradeBot.contracts.externalApis.brokerageApi;
using DayTradeBot.common.util.loggingUtil;
using DayTradeBot.common.util.serializationUtil;
using DayTradeBot.common.util.dataStructureUtil;
using DayTradeBot.common.util.fileUtil;
using DayTradeBot.common.constants;
using DayTradeBot.common.settings;
using DayTradeBot.domains.externalApis.marketDataApi;
using DayTradeBot.domains.externalApis.brokerageApi;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously.
public abstract class TradeEngineBase
{
    public IBrokerageApiService BrokerageApi { get; set; }
    public IMarketDataApiService MarketDataApi { get; set; }
    public List<string> SymbolsToTrade { get; set; }

    public AccountData AccountData { get; set; }
    public IEnumerable<ClosePriceBar> ClosePriceBarDatas { get; set; }

    public PositionsCollection Positions { get; set; }

    public int RoundNum { get; set; }
    public Dictionary<string, bool> LastSymbolsTraded { get; set; }

    public bool HavePlacedFirstBuyOrders { get; set; }

    public DateOnly Today { get; set; }
    public Dictionary<SubscribableTopic, List<Func<object?, Task>>> Subscribers { get; set; } = new Dictionary<SubscribableTopic, List<Func<object?, Task>>>();

    public TradeEngineBase(IMarketDataApiService marketDataApi, IBrokerageApiService brokerageApi, List<string> symbolsToTrade, DateOnly today)
    {
        Today = today;
        BrokerageApi = brokerageApi;
        MarketDataApi = marketDataApi;
        SymbolsToTrade = symbolsToTrade;
        Positions = new PositionsCollection();
        LastSymbolsTraded = new Dictionary<string, bool>();
    }

    public async Task OnPriceUpdate(PriceUpdateSocketData priceUpdate)
    {
        // get the position corresponding to the priceUpdate
        string symbol = priceUpdate.Symbol;
        var updatedPos = Positions.GetBySymbol(symbol);

        // exit if the price isn't a change
        if (updatedPos.Fi.CurrentBidPrice == priceUpdate.CurrentBidPrice
            && updatedPos.Fi.CurrentAskPrice == priceUpdate.CurrentAskPrice)
        {
            return;
        }

        // exit if the price is anomolous (i.e. this must be bad data, and we shouldn't trade based on it)
        if (IsAnomalousPrice(updatedPos, priceUpdate))
        {
            return;
        }

        // perform basic validation check
        ValidatePriceUpdate(priceUpdate);

        // update the position
        updatedPos.Fi.UpdateCurrentPrices(priceUpdate);
        Positions.Sort();

        // set metaData
        RoundNum++;
        var priceUpdateRound = new PriceUpdateRound(this.Today, RoundNum, priceUpdate,
            prevBidPrice: updatedPos.Fi.PrevBidPrice,
            prevAskPrice: updatedPos.Fi.PrevAskPrice);

        if (!HavePlacedFirstBuyOrders)
        {
            await TryPlaceFirstBuyOrders(priceUpdateRound, updatedPos);
        }
        else
        {
            if (updatedPos.CurrentOrders.didCrossPriceOfClosestOpenBuyOrder(priceUpdate.CurrentAskPrice))
            {
                /*
                For the updatedPosition, 
                1) Fill existing buys by price cross
                2) Place and fill immediate-fill buys, if any
                3) Handle all filled buys from the previous two steps (i.e. place corresponding sells for them)
                */

                // 1) Fill existing buys by price cross
                var price = priceUpdate.CurrentAskPrice;
                var filledBuyOrders = await FillBuyOrdersByPriceCross(updatedPos, price);

                // 2) Place and fill immediate-fill buys, if any
                var immediateFillBuyOrdersPlaced = await PlaceImmediateFillBuysForPosition(updatedPos);
                var (confirmedFilledOrders, unfilledBuys) = await ImmediatelyFillBuyOrdersByAssumption(immediateFillBuyOrdersPlaced);

                // 3) Handle all filled buys from the previous two steps (i.e. place corresponding sells for them)
                var allFilledBuys = new List<BuyOrder>();
                allFilledBuys.AddRange(filledBuyOrders);
                allFilledBuys.AddRange(confirmedFilledOrders);

                priceUpdateRound.AddFilledBuys(allFilledBuys);
                priceUpdateRound.AddUnFilledImmediateFillBuys(unfilledBuys);

                var tasks = allFilledBuys.Select(buy => HandleFilledBuy(buy, priceUpdateRound));
                await Task.WhenAll(tasks);

                /*
                Next, place the next primary buys. Note, although the previous part only dealt
                with the updatedPosition, the primary buys placed could be for any position. 
                For example, the filling of buys for one position can allow us to place a new
                openPrimaryBuy for a different position without violating maxNumOpenPrimaryBuys.
                */
                var primaryBuysPlaced = await this.PlaceNextPrimaryBuys();

                // Add the modified orders to the priceUpdateRound tracking object
                priceUpdateRound.AddPrimaryPlacedBuys(primaryBuysPlaced);
            }
            else if (updatedPos.CurrentOrders.didCrossPriceOfClosestOpenSellOrder(priceUpdate.CurrentBidPrice))
            {
                var price = priceUpdate.CurrentBidPrice;
                var filledSellOrders = await FillSellOrdersByPriceCross(updatedPos, price);

                var tasks = filledSellOrders.Select(sell => HandleFilledSell(sell, priceUpdateRound));
                await Task.WhenAll(tasks);

                // Add the modified orders to the priceUpdateRound tracking object
                priceUpdateRound.AddFilledSells(filledSellOrders);
            }
        }

        /*
        Update the current orders for any positions that were modified.
        Importantly, this must be done to update ClosestBuyOrder and ClosestSellOrder 
        so that price crosses are correctly (and efficiently) identified by the code.
        */
        var positionsOfModifiedOrders = priceUpdateRound
            .GetAllModifiedOrders()
            .Select(o => o.Position)
            .Distinct();
        positionsOfModifiedOrders.PerformActionOnEach(pos => pos.CurrentOrders.Update());

        if (priceUpdateRound.DidModifyOrdersThatChangeFreeBal())
        {
            var resp = await this.BrokerageApi.GetAccountData();
            if (!resp.WasSuccessfulResp || resp.Data == null)
            {
                throw new Exception($"Account Data retrieval was unsucessful:\n{resp.ToLogRow()}");
            }
            this.AccountData = resp.Data;
        }

        // publish the EndOfPriceUpdateRound event for any subscribers
        await Publish(SubscribableTopic.EndOfPriceUpdateRound, priceUpdateRound);
    }

    public async Task PerformStartOfDayTasks()
    {
        bool isFreshStart = await LoadPersistedData();
        await Publish(SubscribableTopic.AfterStartOfDayLoad);

        if (!isFreshStart)
        {
            await EditPositionsForStartOfDay();
        }

        HavePlacedFirstBuyOrders = false;

        this.Positions.Sort();
    }

    public async Task PerformEndOfDayTasks()
    {
        await Publish(SubscribableTopic.BeforeEndOfDaySave);
        await PersistPositionsBySymbol();
    }

    public void Subscribe(SubscribableTopic topic, Func<object?, Task> handler)
    {
        if (!Subscribers.TryGetValue(topic, out var subscribers))
        {
            subscribers = new List<Func<object?, Task>>();
            Subscribers[topic] = subscribers;
        }
        subscribers.Add(handler);
    }

    public async Task FakeForceCompleteAllOpenSells()
    {
        //TODO: this method should be in the mock runner bc I won't ever do this in PROD
        foreach (var symbol in Positions.Keys())
        {
            var pos = Positions.GetBySymbol(symbol);
            var price = pos.Fi.CurrentBidPrice;

            var openSells = pos.CurrentOrders.GetSellOrders(OrderStatus.Open).ToList();
            LoggingUtil.LogTo(LogType.Main, $"Force-completing open sells! symbol={symbol}, ct={openSells.Count()}");
            foreach (var sellOrder in openSells)
            {
                var mockOrderStatusData = new OrderStatusData
                {
                    AvgFilledPrice = price,
                    FilledQuantity = sellOrder.OrderQuantity,
                    Status = OrderStatus.Filled
                };
                await FillOrder(sellOrder, mockOrderStatusData);
            }
        }
    }

    protected async Task<OrderAction> AddOrderAction(Order order, OrderAction.ActionType actionType)
    {
        var orderAction = new OrderAction
        {
            Dte = Today,
            RoundNum = RoundNum,
            Type = actionType,
            Order = order
        };
        await Publish(SubscribableTopic.OrderAction, orderAction);
        return orderAction;
    }

    protected async Task<List<SellOrder>> CancelAllOpenSells(Position pos)
    {
        var sellOrders = pos.CurrentOrders.GetSellOrders(OrderStatus.Open).ToList();
        var tasks1 = sellOrders.Select(o => CancelOrder(o));
        await Task.WhenAll(tasks1);

        var tasks2 = sellOrders.Select(o => this.AddOrderAction(o, OrderAction.ActionType.CanceledOrder));
        await Task.WhenAll(tasks2);
        return sellOrders;
    }

    protected async Task PlaceOrders(IEnumerable<Order> orders)
    {
        var tasks = orders.Select(PlaceOrder);
        await Task.WhenAll(tasks);
    }
    public abstract int GetMaxNumOpenPrimaryBuys();
    protected abstract Task HandleFilledBuy(BuyOrder buyOrder, PriceUpdateRound priceUpdateRound);
    protected abstract Task HandleFilledSell(SellOrder sellOrder, PriceUpdateRound priceUpdateRound);
    protected abstract Task HandleInitialBuysForPosition(Position position);
    private async Task Publish(SubscribableTopic topicName, object? obj = null)
    {
        if (!Subscribers.ContainsKey(topicName))
        {
            return;
        }
        List<Func<object?, Task>> subscribers = Subscribers[topicName];
        var tasks = subscribers.Select(handler => handler(obj));
        await Task.WhenAll(tasks);
    }

    private async Task<bool> LoadPersistedData()
    {
        bool isFreshStart = true;
        await PullInitialData();

        DateOnly lastTradingDte = ClosePriceBarDatas.Max(data => data.Dte);
        var positionsBySymbol = await LoadPersistedPositionsBySymbol(lastTradingDte);

        if (positionsBySymbol == null)
        {
            // no "previous" day to carry over, so start fresh
            foreach (string symbol in SymbolsToTrade)
            {
                await RefreshPosition(symbol, isFirstRefresh: true);
            }
            isFreshStart = true;
        }
        else
        {
            // we have persisted the positions from the previous day, so conditionally load the persisted data
            foreach (string symbol in positionsBySymbol.Keys)
            {
                var pos = positionsBySymbol[symbol];
                LoadPosition(pos);
            }
            isFreshStart = false;
        }
        return isFreshStart;
    }

    private async Task CancelAllOpenOrders(Position pos)
    {
        var openOrders = new List<Order>();
        openOrders.AddRange(pos.CurrentOrders.GetBuyOrders(OrderStatus.Open));
        openOrders.AddRange(pos.CurrentOrders.GetSellOrders(OrderStatus.Open));

        var tasks1 = openOrders.Select(o => CancelOrder(o));
        await Task.WhenAll(tasks1);

        var tasks2 = openOrders.Select(o => this.AddOrderAction(o, OrderAction.ActionType.CanceledOrder));
        await Task.WhenAll(tasks2);
    }

    private async Task<Position> RefreshPosition(string symbol, bool isFirstRefresh = false, PositionRenewalAction.ActionType actionType = PositionRenewalAction.ActionType.Refresh)
    {
        Position oldPos = isFirstRefresh ?
            new Position(new FinancialInstrument(symbol)) : Positions.GetBySymbol(symbol);
        await PublishPositionRenewalAction(oldPos, actionType);

        var pos = CreateBlankPosition(symbol);
        LoadPosition(pos);
        return pos;
    }

    private async Task EditPositionsForStartOfDay()
    {
        foreach (var symbol in Positions.Keys())
        {
            var pos = Positions.GetBySymbol(symbol);
            var filledBuys = pos.FilledOrders.Buys;

            if (filledBuys.Count() == 0)
            {
                // position has no filled buys. Refresh it!
                await CancelAllOpenOrders(pos);
                await RefreshPosition(symbol);
            }
            else
            {
                var openSells = pos.CurrentOrders.GetSellOrders(OrderStatus.Open);
                if (openSells.Count() == 0)
                {
                    // if a position has filled buys and no corresponding open sells, renew it
                    await CancelAllOpenOrders(pos);
                    await RefreshPosition(symbol, actionType: PositionRenewalAction.ActionType.Renewal);
                }
            }
        }
    }

    private void ValidatePriceUpdate(PriceUpdateSocketData priceUpdate)
    {
        if (priceUpdate.CurrentBidPrice == 0 || priceUpdate.CurrentAskPrice == 0)
        {
            throw new ArgumentException("price update to $0.00 is not allowed");
        }
    }

    private int GetTotNumOpenPrimaryBuys()
    {
        int totNumOpenPrimaryBuys = 0;
        foreach (var symbol in Positions.Keys())
        {
            var pos = Positions.GetBySymbol(symbol);
            totNumOpenPrimaryBuys += pos.Orders.CurrentOrders.GetNumOpenPrimaryBuys();
        }
        return totNumOpenPrimaryBuys;
    }

    private List<BuyOrder> GetAllOpenPrimaryBuys()
    {
        var openPrimaryBuys = new List<BuyOrder>();
        foreach (var symbol in Positions.Keys())
        {
            var pos = Positions.GetBySymbol(symbol);
            openPrimaryBuys.AddRange(pos.Orders.CurrentOrders.GetAllOpenPrimaryBuys());
        }
        return openPrimaryBuys;
    }

    private async Task PullInitialData()
    {
        var accountDataTask = BrokerageApi.GetAccountData();
        var closeDatasTask = MarketDataApi.GetClosePriceBarDatas(SymbolsToTrade);

        await Task.WhenAll(accountDataTask, closeDatasTask);

        var accountDataResponse = await accountDataTask;
        if (accountDataResponse.Data == null)
        {
            throw new InvalidOperationException("Account Data is empty");
        }
        AccountData = accountDataResponse.Data;

        var closeDatasResponse = await closeDatasTask;
        if (closeDatasResponse.Datas == null)
        {
            throw new InvalidOperationException("Close Data Prices is empty");
        }
        ClosePriceBarDatas = closeDatasResponse.Datas;
    }

    private static string GetPersistFilePath(DateOnly today)
    {
        string folderPath = AppSettings.GetPersistFolderPath();
        string filePath = Path.Combine(folderPath, $"positionsBySymbol_{today:yyyy-MM-dd}.json");
        return filePath;
    }

    private async Task<Dictionary<string, Position>?> LoadPersistedPositionsBySymbol(DateOnly lastTradingDte)
    {
        string filePath = GetPersistFilePath(lastTradingDte);
        if (!File.Exists(filePath))
        {
            LoggingUtil.LogTo(LogType.Main, $"No persisted data found on {this.Today:yyyy-MM-dd}");
            return null;
        }
        var deserialized = SerializationUtil.DeserializePositionsBySymbol(filePath);
        LoggingUtil.LogTo(LogType.Main, $"Successfully deserialized persisted data on {this.Today:yyyy-MM-dd}");
        return deserialized;
    }

    private async Task PersistPositionsBySymbol()
    {
        FileUtil.EnsureDirectoryExists(AppSettings.GetPersistFolderPath());
        string filePath = GetPersistFilePath(this.Today);
        SerializationUtil.SerializePositionsBySymbol(this.Positions.ToDictionary(), filePath);
        LoggingUtil.LogTo(LogType.Main, $"Successfully serialized persisted data on {this.Today:yyyy-MM-dd}");
    }

    private Position CreateBlankPosition(string symbol)
    {
        var fi = new FinancialInstrument(symbol);
        var pos = new Position(fi);
        return pos;
    }

    private void LoadPosition(Position pos)
    {
        var symbol = pos.Fi.Symbol;
        Positions.SetPosition(pos);

        var closePriceBar = ClosePriceBarDatas.First(closeBar => closeBar.Symbol == symbol);
        pos.Fi.PrevClosePrice = closePriceBar.Close;

        if (pos.OrigClosePriceBar == null)
        {
            pos.OrigClosePriceBar = closePriceBar;
        }
    }

    private async Task<List<BuyOrder>> FillBuyOrdersByPriceCross(Position pos, float price)
    {
        var buyOrdersCrossed = pos.CurrentOrders.GetOpenBuyOrdersAbovePrice(price).ToList();
        var orderStatusResps = await RetrieveOrderStatuses(buyOrdersCrossed);

        var ordersFilled = new List<BuyOrder>();
        for (int i = 0; i < orderStatusResps.Count(); i++)
        {
            var orderStatusResp = orderStatusResps[i];
            var orderStatusData = orderStatusResp.Data;
            var buyOrder = buyOrdersCrossed[i];

            if (orderStatusData.Status != OrderStatus.Filled)
            {
                // LoggingUtil.LogTo(LogType.Error, $"Buy order not filled according to the brokerageApi:\n{buyOrder.ToLogRow()}\norderStatusData={orderStatusData.Status}");
                continue;
            }

            await FillOrder(buyOrder, orderStatusData);
            ordersFilled.Add(buyOrder);
        }
        return ordersFilled;
    }

    private async Task<List<SellOrder>> FillSellOrdersByPriceCross(Position pos, float price)
    {
        var sellOrdersCrossed = pos.CurrentOrders.GetOpenSellOrdersBelowPrice(price).ToList();
        var orderStatusResps = await RetrieveOrderStatuses(sellOrdersCrossed);

        var ordersFilled = new List<SellOrder>();
        for (int i = 0; i < orderStatusResps.Count(); i++)
        {
            var orderStatusResp = orderStatusResps[i];
            var orderStatusData = orderStatusResp.Data;
            var sellOrder = sellOrdersCrossed[i];

            if (orderStatusData.Status != OrderStatus.Filled)
            {
                LoggingUtil.LogTo(LogType.Error, $"Sell order not filled according to the brokerageApi: {sellOrder.ToLogRow()}");
                continue;
            }

            await FillOrder(sellOrder, orderStatusData);
            ordersFilled.Add(sellOrder);
        }
        return ordersFilled;
    }

    private async Task<List<BuyOrder>> PlaceImmediateFillBuysForPosition(Position pos)
    {
        var currPrice = pos.Fi.CurrentAskPrice;
        var buyOrdersToPlace = pos.CurrentOrders.GetImmediateFillBuys(currPrice).ToList();

        var tasks = buyOrdersToPlace.Select(PlaceOrder);
        await Task.WhenAll(tasks);

        foreach (var buy in buyOrdersToPlace)
        {
            if (buy.Status == OrderStatus.Open)
            {
                buy.IsImmediateFillBuy = true;
                await Publish(SubscribableTopic.ImmediateFillBuyPlacement, buy);
            }
        }
        return buyOrdersToPlace;
    }

    private async Task<List<BuyOrder>> PlaceNextPrimaryBuys()
    {
        int numOpenPrimaryBuys = this.GetTotNumOpenPrimaryBuys();
        int maxNumOpenPrimaryBuys = this.GetMaxNumOpenPrimaryBuys();

        var allBuyOrdersPlaced = new List<BuyOrder>();
        IEnumerable<Position> positionsSortedByPctDrop = this.Positions.GetEnumerable();
        int idx = 0;
        while (numOpenPrimaryBuys < maxNumOpenPrimaryBuys && idx < positionsSortedByPctDrop.Count())
        {
            var pos = positionsSortedByPctDrop.ElementAt(idx);

            var placedBuyOrder = await PlaceNextPrimaryBuyForPosition(pos);
            if (placedBuyOrder != null)
            {
                numOpenPrimaryBuys++; // because only one of the possibly multiple orders placed is assumed to be truly "open"; if others were placed, we assume they will immediately be filled bc they are above the current price
                allBuyOrdersPlaced.Add(placedBuyOrder);
            }
            idx++;
        }

        return allBuyOrdersPlaced;
    }

    private async Task TryPlaceFirstBuyOrders(PriceUpdateRound priceUpdateRound, Position updatedPos)
    {
        bool allHaveCurrentBidPrice = Positions.GetEnumerable().All(position => position.Fi.CurrentBidPrice != 0);

        if (!allHaveCurrentBidPrice)
        {
            return;
        }

        if (AppSettings.IsLogTypeOn(LogType.Debug))
        {
            LoggingUtil.LogTo(LogType.Debug, "All positions have a price update for today");
        }
        foreach (var pos in Positions.GetEnumerable())
        {
            if (pos.CurrentOrders.Buys.Count() == 0 && pos.CurrentOrders.Sells.Count() == 0)
            {
                // only create buys on positions that haven't had any created already (e.g. persisted positions that we've been playing for days)
                await this.HandleInitialBuysForPosition(pos);
            }
        }

        /*
        For all positions, place and fill immediate-fill buys. 
        WARNING: This disregards the GetMaxNumOpenPrimaryBuys constraint.
        1) place and try-to-fill immediate-fill buys with limitPrice > currentPrice
        2) save the filled buys for debug / validation purposes
        3) handle the fill buys (e.g. place corresponding sells for them)
        */
        foreach (var pos in Positions.GetEnumerable())
        {
            // 1) place and try-to-fill immediate-fill buys with limitPrice > currentPrice
            var immediateFillBuys = await PlaceImmediateFillBuysForPosition(pos);
            var (confirmedFilledBuys, unfilledBuys) = await ImmediatelyFillBuyOrdersByAssumption(immediateFillBuys);

            // 2) Add the modified orders to the priceUpdateRound tracking object
            priceUpdateRound.AddFilledBuys(confirmedFilledBuys);
            priceUpdateRound.AddUnFilledImmediateFillBuys(unfilledBuys);

            // 3) handle the fill buys (e.g. place corresponding sells for them)
            var tasks = confirmedFilledBuys.Select(immediateFillBuy => HandleFilledBuy(immediateFillBuy, priceUpdateRound));
            await Task.WhenAll(tasks);
        }

        // Place the next primary buys.
        var primaryBuysPlaced = await PlaceNextPrimaryBuys();

        // Add the modified orders to the priceUpdateRound tracking object
        priceUpdateRound.AddPrimaryPlacedBuys(primaryBuysPlaced);

        HavePlacedFirstBuyOrders = true;
    }

    private async Task<GetOrderStatusResponse[]> RetrieveOrderStatuses(IEnumerable<Order> orders)
    {
        var orderStatusTasks = orders.Select(this.BrokerageApi.GetOrderStatus);
        var orderStatusResponses = await Task.WhenAll(orderStatusTasks);

        foreach (var resp in orderStatusResponses)
        {
            if (!resp.WasSuccessfulResp)
            {
                throw new Exception($"Retrieval of order status was unsucessful:\n{resp.ToLogRow()}");
            }
        }
        return orderStatusResponses;
    }

    private async Task<(List<BuyOrder>, List<BuyOrder>)> ImmediatelyFillBuyOrdersByAssumption(List<BuyOrder> buyOrders)
    {
        var unfilledOrders = new List<BuyOrder>();
        var filledOrders = new List<BuyOrder>();
        var orderStatusResps = await RetrieveOrderStatuses(buyOrders);
        for (int i = 0; i < orderStatusResps.Count(); i++)
        {
            var orderStatusResp = orderStatusResps[i];
            var orderStatusData = orderStatusResp.Data;

            var buyOrder = buyOrders[i];

            if (buyOrder.LimitPrice <= buyOrder.Position.Fi.CurrentAskPrice)
            {
                unfilledOrders.Add(buyOrder);
                throw new Exception($"Immediate-fill order does not have a limit price above the currPrice.\n{buyOrder.ToLogRow()}");
            }
            else if (orderStatusData.Status != OrderStatus.Filled)
            {
                unfilledOrders.Add(buyOrder);
                LoggingUtil.LogTo(LogType.Error, $"An immediate-fill order has NOT been filled according to the brokerageApi: {buyOrder.LimitPrice}");
            }
            else if (orderStatusData.FilledQuantity != buyOrder.OrderQuantity)
            {
                unfilledOrders.Add(buyOrder);
                throw new Exception($"Immediate-fill order marked as filled but FilledQuantity inconsistent.\n{orderStatusResp.ToLogRow()}");
            }
            else
            {
                await FillOrder(buyOrder, orderStatusData);
                filledOrders.Add(buyOrder);
            }
        }

        return (filledOrders, unfilledOrders);
    }

    private async Task<BuyOrder?> PlaceNextPrimaryBuyForPosition(Position pos)
    {
        var buyOrderToPlace = pos.CurrentOrders.GetNextPrimaryBuyToPlace();

        if (buyOrderToPlace != null)
        {
            await this.PlaceOrder(buyOrderToPlace);
        }
        return buyOrderToPlace;
    }

    private async Task PlaceOrder(Order order)
    {
        var orders = order.Position.Orders;
        PlaceLimitOrderResponse resp;
        if (order.Direction == OrderDirection.Buy)
        {
            var buyOrder = (BuyOrder)order;
            resp = await BrokerageApi.PlaceLimitBuyOrder(buyOrder);
        }
        else
        {
            var sellOrder = (SellOrder)order;
            resp = await BrokerageApi.PlaceLimitSellOrder(sellOrder);
        }

        if (!resp.WasSuccessfulResp)
        {
            LoggingUtil.LogTo(LogType.Error, $"Order was not placed:\n{order.ToLogRow()}");
            return;
        }

        orders.MarkAsPlaced(order);
        await AddOrderAction(order, OrderAction.ActionType.PlacedOrder);
    }

    private async Task FillOrder(Order order, OrderStatusData orderStatusData)
    {
        var pos = order.Position;

        pos.Orders.MarkAsFilled(order, orderStatusData);
        await AddOrderAction(order, OrderAction.ActionType.FilledOrder);

        float? filledUsdAmount = order.GetFilledUsdAmount();
        if (filledUsdAmount == null)
        {
            throw new Exception("Filled USD amount should not be null");
        }
        int amtDirection = order.Direction == OrderDirection.Buy ? -1 : 1;

        var accountBalanceUpdate = new AccountBalanceUpdate
        {
            FreeUsdBalance = this.AccountData.FreeUsdBalance + (amtDirection * (float)filledUsdAmount)
        };
        await Publish(SubscribableTopic.AccountBalanceUpdate, accountBalanceUpdate);

        var bal = LoggingUtil.UsdToString(this.AccountData.FreeUsdBalance);

        if (AppSettings.IsLogTypeOn(LogType.Debug))
        {
            LoggingUtil.LogTo(LogType.Debug, $"Account balance = {bal}");
        }
    }

    private async Task<bool> CancelOrder(Order order)
    {
        CancelOrderResponse resp;
        resp = await BrokerageApi.CancelOrder(order);
        if (!resp.WasSuccessfulResp)
        {
            throw new Exception($"Order failed to cancel!\n{order.ToLogRow()}");
        }
        order.Position.Orders.MarkAsCancelled(order);
        return resp.WasSuccessfulResp;
    }

    private async Task PublishPositionRenewalAction(Position position, PositionRenewalAction.ActionType type)
    {
        var positionRenewalAction = new PositionRenewalAction
        {
            Dte = Today,
            Position = position,
            Type = type
        };
        await Publish(SubscribableTopic.PositionRenewalAction, positionRenewalAction);
    }

    private bool IsAnomalousPrice(Position updatedPos, PriceUpdateSocketData priceUpdate)
    {
        var magnitudeOfPriceChange = Math.Abs(updatedPos.GetPctChangeFromCurrPrice(priceUpdate));
        if (magnitudeOfPriceChange > .02 && updatedPos.Fi.NumPriceUpdatesToday > 0)
        {
            LoggingUtil.LogTo(LogType.Error, $@"
                Large magnitude of price change!
                    Symbol={updatedPos.Symbol}, 
                    Dte={LoggingUtil.DteToString(this.Today)}, 
                    Time={LoggingUtil.TimeSpanToString(priceUpdate.TimeOfDay)}
                    PrevBidPrice:{updatedPos.Fi.PrevBidPrice}
                    CurrentBidPrice:{updatedPos.Fi.CurrentBidPrice}
                    NewPrice:{priceUpdate.CurrentBidPrice}
            ");
            return true;
        }
        return false;
    }
}



