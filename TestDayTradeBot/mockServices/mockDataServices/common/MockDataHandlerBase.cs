namespace TestTradeBot.mockServices.mockDataServices.common;

using DayTradeBot.common.settings;

public class MockDataHandlerBase
{
    public static string MockDataFolderPath = Path.Combine(AppSettings.GetRootDirectory(), "TestDayTradeBot", "mockData");
}