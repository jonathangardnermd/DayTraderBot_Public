namespace TestTradeBot.mockServices.mockDataServices.dataLoaders;

using Newtonsoft.Json;
using TestTradeBot.mockServices.mockDataServices.common;

public class MockPriceUpdateLoader : MockDataHandlerBase
{
    public string DataSetDirectory { get; private set; }
    public MockPriceUpdateLoader(string dataSetFolder)
    {
        DataSetDirectory = Path.Combine(MockDataFolderPath, dataSetFolder, "priceUpdates");
    }

    public Dictionary<DateOnly, List<PriceUpdateEntry>> LoadAllPriceUpdatesFromFiles()
    {
        var allClosePrices = new List<PriceUpdateEntry>();

        Directory.CreateDirectory(DataSetDirectory);

        string[] folderPaths = Directory.GetDirectories(DataSetDirectory);
        foreach (var folderPath in folderPaths)
        {
            string symbol = Path.GetFileName(folderPath);

            string[] filePaths = Directory.GetFiles(folderPath);

            foreach (var filePath in filePaths)
            {
                List<PriceUpdateEntry>? loadedData = JsonConvert.DeserializeObject<List<PriceUpdateEntry>>(File.ReadAllText(filePath));
                if (loadedData != null)
                {
                    allClosePrices.AddRange(loadedData);
                }
            }
        }

        var transformed = TransformListOfPriceUpdatesEntries(allClosePrices);

        // combine all symbols under one date to simulate concurrent price updates from all symbols throughout the day
        var combined = new Dictionary<DateOnly, List<PriceUpdateEntry>>();
        foreach (var dte in transformed.Keys)
        {
            var bySymbol = transformed[dte];

            var allPriceUpdatesOnDte = new List<PriceUpdateEntry>();

            foreach (var symbol in bySymbol.Keys)
            {
                var priceUpdates = bySymbol[symbol];
                allPriceUpdatesOnDte.AddRange(priceUpdates);
            }
            combined[dte] = allPriceUpdatesOnDte.OrderBy(entry => entry.TimeOfDay).ToList();
        }

        return combined;
    }

    private static Dictionary<DateOnly, Dictionary<string, List<PriceUpdateEntry>>> TransformListOfPriceUpdatesEntries(List<PriceUpdateEntry> entries)
    {
        var rslt = new Dictionary<DateOnly, Dictionary<string, List<PriceUpdateEntry>>>();

        foreach (var entry in entries)
        {
            if (!rslt.ContainsKey(entry.Date))
            {
                rslt[entry.Date] = new Dictionary<string, List<PriceUpdateEntry>>();
            }

            var onDte = rslt[entry.Date];

            if (!onDte.ContainsKey(entry.Symbol))
            {
                onDte[entry.Symbol] = new List<PriceUpdateEntry>();
            }

            onDte[entry.Symbol].Add(entry);
        }
        return rslt;
    }
}