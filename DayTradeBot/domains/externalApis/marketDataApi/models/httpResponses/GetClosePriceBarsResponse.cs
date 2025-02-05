namespace DayTradeBot.domains.externalApis.marketDataApi;

public class GetClosePriceBarsResponse
{
    public IEnumerable<ClosePriceBar>? Datas { get; set; }
}

public class ClosePriceBar
{
    public string? Symbol { get; set; }
    public DateOnly Dte { get; set; }
    public float Open { get; set; }
    public float High { get; set; }
    public float Low { get; set; }
    public float Close { get; set; }
    public long Volume { get; set; }
}