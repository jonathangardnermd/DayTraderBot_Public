namespace TestTradeBot.mockServices.mockDataServices.dataPullers;

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Alpaca.Markets;
using TestTradeBot.mockServices.mockDataServices.common;


public class AlpacaMockClosePriceBarPuller : AlpacaDataPullerBase
{
    public async Task Run(string dataSetFolder, List<string> symbols, DateOnly startDte, DateOnly endDte)
    {
        foreach (var symbol in symbols)
        {
            Console.WriteLine($"Pulling data for {symbol} from {startDte:yyyy-MM-dd} to {endDte:yyyy-MM-dd}");
            var data = await Pull(symbol, startDte, endDte);
            SaveDataToFile(dataSetFolder, symbol, startDte, endDte, data);
        }
    }

    private async Task<List<ClosePriceEntry>> Pull(string symbol, DateOnly startDate, DateOnly endDate)
    {
        var endDt = endDate.ToDateTime(new TimeOnly());
        var startDt = startDate.ToDateTime(new TimeOnly());
        var timeframe = BarTimeFrame.Day;

        var bars = await DataClient.ListHistoricalBarsAsync(
            new HistoricalBarsRequest(symbol, startDt, endDt, timeframe));

        var closePriceEntries = bars.Items.Select(bar => new ClosePriceEntry
        {
            Date = DateOnly.FromDateTime(bar.TimeUtc),
            ClosePrice = (float)bar.Close,
            Symbol = bar.Symbol
        }).ToList();
        return closePriceEntries;
    }

    private static void SaveDataToFile(string dataSetFolder, string symbol, DateOnly startDate, DateOnly endDate, List<ClosePriceEntry> data)
    {
        string dataSetDirectory = Path.Combine(MockDataFolderPath, dataSetFolder, "closePrices");

        // Save the relevant data to a file
        string fileName = $"{symbol}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.json";

        string directoryPath = Path.Combine(dataSetDirectory, symbol);
        Directory.CreateDirectory(directoryPath);
        string filePath = Path.Combine(directoryPath, fileName);
        File.WriteAllText(filePath, JsonConvert.SerializeObject(data));
    }
}


