namespace TestTradeBot.mockServices.mockDataServices.common;

public class ClosePriceEntry
{
    public DateOnly Date { get; set; }
    public float ClosePrice { get; set; }

    public string Symbol { get; set; } = "";

    public string ToFormattedString()
    {
        return $"Symbol={Symbol}, Date={Date:yyyy-MM-dd}, ClosePrice={ClosePrice}";
    }
}

public class PriceUpdateEntry
{
    public DateOnly Date { get; set; }
    public TimeSpan TimeOfDay { get; set; }
    public float Price { get; set; }
    public string Symbol { get; set; } = "";
    public string ToFormattedString()
    {
        var timeStr = TimeOfDay.ToString(@"hh\:mm\:ss");
        return $"Symbol={Symbol}, {Date:yyyyMMdd}, TimeOfDay={timeStr}, ClosePrice={Price}";
    }
}
