namespace TestTradeBot.mockServices.mockDataServices.dataLoaders;

using Newtonsoft.Json;
using TestTradeBot.mockServices.mockDataServices.common;

public class MockClosePriceBarLoader : MockDataHandlerBase
{
    public string DataSetDirectory { get; private set; }
    public MockClosePriceBarLoader(string dataSetFolder)
    {
        DataSetDirectory = Path.Combine(MockDataFolderPath, dataSetFolder, "closePrices");
    }

    public Dictionary<DateOnly, Dictionary<string, ClosePriceEntry>> LoadAllClosePricesFromFiles()
    {
        var allClosePrices = new List<ClosePriceEntry>();

        Directory.CreateDirectory(DataSetDirectory);

        string[] folderPaths = Directory.GetDirectories(DataSetDirectory);
        foreach (var folderPath in folderPaths)
        {
            string symbol = Path.GetFileName(folderPath);

            string[] filePaths = Directory.GetFiles(folderPath);

            foreach (var filePath in filePaths)
            {
                List<ClosePriceEntry>? loadedData = JsonConvert.DeserializeObject<List<ClosePriceEntry>>(File.ReadAllText(filePath));
                if (loadedData != null)
                {
                    allClosePrices.AddRange(loadedData);
                }
            }
        }

        return TransformListOfClosePriceEntries(allClosePrices);
    }

    private static Dictionary<DateOnly, Dictionary<string, ClosePriceEntry>> TransformListOfClosePriceEntries(List<ClosePriceEntry> entries)
    {
        var rslt = new Dictionary<DateOnly, Dictionary<string, ClosePriceEntry>>();

        foreach (var entry in entries)
        {
            if (!rslt.ContainsKey(entry.Date))
            {
                rslt[entry.Date] = new Dictionary<string, ClosePriceEntry>();
            }

            var closePricesOnDte = rslt[entry.Date];
            if (closePricesOnDte.ContainsKey(entry.Symbol))
            {
                throw new Exception($"Two close prices for the same symbol and dte: {entry.ToFormattedString()}");
            }

            closePricesOnDte[entry.Symbol] = entry;
        }
        return rslt;
    }
}