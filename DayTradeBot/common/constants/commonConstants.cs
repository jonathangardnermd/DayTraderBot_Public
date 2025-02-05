namespace DayTradeBot.common.constants;

public enum AppRunMode
{
    RunMultipleTestSimulations,
    RunSingleTestSimulation,
    RunTests,
    RunProd,
    PullData
}
public enum OrderDirection
{
    Buy,
    Sell
}

public enum SubscribableTopic
{
    PriceUpdate,
    OrderAction,
    PositionRenewalAction,
    BeforeEndOfDaySave,
    AfterStartOfDayLoad,
    EndOfPriceUpdateRound,
    AccountBalanceUpdate,
    ImmediateFillBuyPlacement,
}

public enum Env
{
    Test,
    Prod
}

public enum LogMode
{
    Verbose,
    Talkative,
    Quiet,
    Default
}

public enum BrokerageApiEnv
{
    RealMoney,
    PlayMoney
}

public enum LogType
{
    Main,
    Debug,
    Error,
    ApiPlaceOrder,
    ApiGetOrderStatus,
    ApiGetAccount,
    ApiCancelOrder,
    OrderAction,
    PositionRenewalAction,
    PriceUpdateRound,
    ManagedResources,
    OrderChanges,
    SimulationResults,
    DailySummary
}
public enum TimeFrame
{
    OneDay
}

public enum OrderStatus
{
    Open,
    Filled,
    Cancelled,

    NotPlacedYet,
    ZeroQuantity
}

public enum DeterminationType
{
    PriceCrossed,
    BrokerageUpdate
}

public enum ApiActionName
{
    BuyOrderPlaced,
    BuyOrderCanceled,
    SellOrderPlaced,
    SellOrderCanceled,
}

public enum PositionLoadState
{
    New,
    Renewed,
    Ongoing
}