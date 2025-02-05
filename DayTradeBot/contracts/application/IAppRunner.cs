namespace DayTradeBot.contracts.application;

using DayTradeBot.domains.app;

public interface IAppRunner
{
    Task Run(App app);
}