namespace TestTradeBot.mockServices.mockDataServices.dataPullers;

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Alpaca.Markets;
using TestTradeBot.mockServices.mockDataServices.common;

public class AlpacaMockPriceUpdatePuller : AlpacaDataPullerBase
{
    public async Task Run(string dataSetFolder, List<string> symbols, DateOnly startDte, DateOnly endDte)
    {
        foreach (var symbol in symbols)
        {
            Console.WriteLine($"Pulling data for {symbol} from {startDte:yyyy-MM-dd} to {endDte:yyyy-MM-dd}");
            var data = await Pull(symbol, startDte, endDte);
            if (data.Count > 0)
            {
                SaveDataToFile(dataSetFolder, symbol, startDte, endDte, data);
            }
        }
    }

    private async Task<List<PriceUpdateEntry>> Pull(string symbol, DateOnly startDate, DateOnly endDate)
    {
        var endDt = endDate.ToDateTime(new TimeOnly()).Date.AddDays(1).AddTicks(-1);
        var startDt = startDate.ToDateTime(new TimeOnly());
        var timeframe = BarTimeFrame.Minute;

        var bars = await DataClient.ListHistoricalBarsAsync(
            new HistoricalBarsRequest(symbol, startDt, endDt, timeframe));

        var priceUpdateEntries = bars.Items.Select(bar => new PriceUpdateEntry
        {
            Date = DateOnly.FromDateTime(bar.TimeUtc),
            TimeOfDay = bar.TimeUtc.TimeOfDay,
            Price = (float)bar.Close,
            Symbol = bar.Symbol
        }).ToList();
        return priceUpdateEntries;
    }

    private static void SaveDataToFile(string dataSetFolder, string symbol, DateOnly startDate, DateOnly endDate, List<PriceUpdateEntry> data)
    {
        string dataSetDirectory = Path.Combine(MockDataFolderPath, dataSetFolder, "priceUpdates");

        // Save the relevant data to a file
        string fileName = $"{symbol}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.json";

        string directoryPath = Path.Combine(dataSetDirectory, symbol);
        Directory.CreateDirectory(directoryPath);
        string filePath = Path.Combine(directoryPath, fileName);
        File.WriteAllText(filePath, JsonConvert.SerializeObject(data));
    }
}

