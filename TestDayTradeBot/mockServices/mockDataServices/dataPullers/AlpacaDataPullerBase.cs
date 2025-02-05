
namespace TestTradeBot.mockServices.mockDataServices.dataPullers;

using Alpaca.Markets;
using DayTradeBot.common.util.alpacaUtil;
using TestTradeBot.mockServices.mockDataServices.common;

public class AlpacaDataPullerBase : MockDataHandlerBase
{
    public IAlpacaDataClient DataClient { get; set; }
    public AlpacaDataPullerBase()
    {
        var apiKeys = AlpacaUtil.GetAlpacaApiKeys();
        DataClient = Environments.Live.GetAlpacaDataClient(new SecretKey(apiKeys.ApiKey, apiKeys.ApiSecret));
    }
}