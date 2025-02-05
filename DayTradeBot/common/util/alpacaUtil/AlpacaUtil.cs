namespace DayTradeBot.common.util.alpacaUtil;

using DayTradeBot.common.util.fileUtil;
using DayTradeBot.common.settings;
using Alpaca.Markets;

public static class AlpacaUtil
{
    public static AlpacaApiKeys GetAlpacaApiKeys()
    {
        if (AppSettings.BrokerageEnv == constants.BrokerageApiEnv.RealMoney)
        {
            return GetAlpacaApiKeys("apiKeysLive.json");
        }
        else
        {
            return GetAlpacaApiKeys("apiKeysPaper.json");
        }
    }

    public static IEnvironment GetAlpacaEnv()
    {
        if (AppSettings.BrokerageEnv == constants.BrokerageApiEnv.RealMoney)
        {
            return Environments.Live;
        }
        else
        {
            return Environments.Paper;
        }
    }

    public static IAlpacaTradingClient GetTradingClient()
    {
        var apiKeys = GetAlpacaApiKeys();
        IEnvironment env = GetAlpacaEnv();
        return env.GetAlpacaTradingClient(new SecretKey(apiKeys.ApiKey, apiKeys.ApiSecret));
    }

    private static AlpacaApiKeys GetAlpacaApiKeys(string fileName)
    {
        string currentDirectory = AppSettings.GetRootDirectory();
        string filePath = Path.Combine(currentDirectory, "DayTradeBot", "apiKeys", fileName);
        var json = FileUtil.LoadJsonFromFile(filePath);

        return new AlpacaApiKeys
        {
            ApiKey = json["API_KEY"]?.ToString() ?? "",
            ApiSecret = json["API_SECRET"]?.ToString() ?? ""
        };
    }
}

public class AlpacaApiKeys
{
    public string ApiKey { get; set; } = "";
    public string ApiSecret { get; set; } = "";
}