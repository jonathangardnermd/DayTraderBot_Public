namespace DayTradeBot.domains.externalApis.marketDataApi;

using DayTradeBot.common.util.managedResourceUtil;

public abstract class PriceUpdateSocketBase : Disposable
{
    public abstract Task Connect();
    public abstract Task SubscribeForSymbols(List<string> symbols, Action<PriceUpdateSocketData> fxnToSubscribe);
}