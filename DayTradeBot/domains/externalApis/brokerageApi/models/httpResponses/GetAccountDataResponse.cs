
namespace DayTradeBot.domains.externalApis.brokerageApi;

using DayTradeBot.common.util.loggingUtil;

public class GetAccountDataResponse : TabularEntity
{
    public AccountData? Data { get; set; }
    public bool WasSuccessfulResp { get; set; }

    public override List<string> GetColumnValues(bool shouldTruncateGuids = false)
    {
        return new List<string>{
            LoggingUtil.UsdToString(this.Data?.FreeUsdBalance),
            WasSuccessfulResp.ToString()
        };
    }

    public override List<TableColumn> GetColumnDefs()
    {
        return GetColumnDefinitons();
    }

    public static List<TableColumn> GetColumnDefinitons()
    {
        return new List<TableColumn>{
            new TableColumn
            {
                ColName = "FreeUsdBal",
                ColWidth = 15
            },
            new TableColumn
            {
                ColName = "Success",
                ColWidth = 20
            }
        };
    }

    public static string[] GetColHeaders()
    {
        return GetColumnDefinitons().Select(col => col.ColName).ToArray();
    }
}

public class AccountData
{
    public float FreeUsdBalance { get; set; }
}