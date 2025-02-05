namespace DayTradeBot.domains.externalApis.marketDataApi;

public class PriceUpdateSocketData
{
    public string Symbol { get; set; }
    public float CurrentBidPrice { get; set; }
    public float CurrentAskPrice { get; set; }
    public TimeSpan TimeOfDay { get; set; }

    public PriceUpdateSocketData(string symbol, float currentBidPrice, float currentAskPrice, TimeSpan timeOfDay)
    {
        Symbol = symbol;
        CurrentBidPrice = currentBidPrice;
        CurrentAskPrice = currentAskPrice;
        TimeOfDay = timeOfDay;
    }

    public override string ToString()
    {
        return $"[{Symbol}] - CurrentBidPrice: {CurrentBidPrice}, CurrentAskPrice: {CurrentAskPrice}, Timestamp={TimeOfDay:hh\\:mm\\:ss}";
    }
}
