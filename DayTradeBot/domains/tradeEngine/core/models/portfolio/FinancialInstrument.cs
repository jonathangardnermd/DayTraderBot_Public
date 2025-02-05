
namespace DayTradeBot.domains.tradeEngine.core;

using System.Text.Json.Serialization;
using DayTradeBot.domains.externalApis.marketDataApi;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
[Serializable]
public class FinancialInstrument
{
    public string Symbol { get; set; }

    [JsonIgnore]
    public float CurrentBidPrice { get; set; }

    [JsonIgnore]
    public float CurrentAskPrice { get; set; }

    [JsonIgnore]
    public float PrevBidPrice { get; set; }

    [JsonIgnore]
    public float PrevAskPrice { get; set; }

    [JsonIgnore]
    public float PrevClosePrice { get; set; }

    [JsonIgnore]
    public float FirstPriceUpdateOfTheDay { get; set; }

    [JsonIgnore]
    public int NumPriceUpdatesToday { get; set; }

    public FinancialInstrument(string symbol)
    {
        Symbol = symbol;
    }

    public FinancialInstrument()
    {
    }

    public float GetCurrentPrice()
    {
        return (CurrentBidPrice + CurrentAskPrice) / 2;
    }

    public float GetPrevPrice()
    {
        return (PrevBidPrice + PrevAskPrice) / 2;
    }

    public bool HasPrice()
    {
        return CurrentBidPrice != 0;
    }
    public void UpdateCurrentPrices(PriceUpdateSocketData update)
    {
        NumPriceUpdatesToday++;
        if (CurrentBidPrice == 0)
        {
            FirstPriceUpdateOfTheDay = update.CurrentBidPrice;
        }
        PrevBidPrice = CurrentBidPrice;
        PrevAskPrice = CurrentAskPrice;
        CurrentBidPrice = update.CurrentBidPrice;
        CurrentAskPrice = update.CurrentAskPrice;

        if (CurrentBidPrice == 0 || CurrentAskPrice == 0)
        {
            throw new ArgumentException("cannot update prices to $0.00!");
        }
    }
}

