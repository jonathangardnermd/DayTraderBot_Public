namespace DayTradeBot.domains.appOrchestration;

using DayTradeBot.common.constants;
using DayTradeBot.common.util.dataStructureUtil;
using DayTradeBot.domains.tradeEngine.core;

public class OrderActions : DictOfDictsOfLists<string, OrderDirection, OrderAction>
{
    private int CurrSeqNum { get; set; } = 0;
    public int NumZeroQuantityOrders { get; set; } = 0;

    public void Add(OrderAction orderAction)
    {
        if (orderAction.Type == OrderAction.ActionType.ZeroQuantity)
        {
            NumZeroQuantityOrders++;
            return;
        }
        CurrSeqNum++;
        orderAction.SeqNum = CurrSeqNum;
        var order = orderAction.Order;
        Add(primaryKey: order.Symbol, secondaryKey: order.Direction, objToAdd: orderAction);
    }

    public Dictionary<OrderDirection, IEnumerable<OrderAction>> GetFilledOrdersByDirection()
    {
        return this.ToFilteredDictOfListsByConsolidatingPrimaryKey(orderAction => orderAction.IsFilledOrder);
    }

    public IEnumerable<OrderAction> GetOrderActionsSortedBySeqNum()
    {
        return this.Flatten()
        .OrderBy(oa => oa.SeqNum);
    }
}

public class PositionRenewalActions : DictOfLists<string, PositionRenewalAction>
{
    public int CurrSeqNum { get; set; } = 0;
    public Dictionary<Guid, bool> GuidLookupDict { get; set; } = new Dictionary<Guid, bool>();
    public void Add(PositionRenewalAction positionRenewalAction)
    {
        CurrSeqNum++;
        positionRenewalAction.SeqNum = CurrSeqNum;
        var symbol = positionRenewalAction.Position.Symbol;
        Add(symbol, positionRenewalAction);

        var guid = positionRenewalAction.Position.Uid;
        GuidLookupDict[guid] = true;
    }

    public Dictionary<Guid, bool> GetGuidLookupDict()
    {
        return GuidLookupDict;
    }
}