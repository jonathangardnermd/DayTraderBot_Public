namespace DayTradeBot.common.util.loggingUtil;

using ConsoleTables;

public abstract class TabularEntity
{
    public abstract List<TableColumn> GetColumnDefs();
    public abstract List<string> GetColumnValues(bool shouldTruncateGuids = false);

    public static ConsoleTable GetTable(string[] colHeaders, IEnumerable<TabularEntity> entities)
    {
        var table = new ConsoleTable(colHeaders);
        foreach (var entity in entities)
        {
            table.AddRow(entity.ToTableRow());
        }
        return table;
    }

    public string ToLogRow()
    {
        string logRowStr = "";

        var cols = this.GetColumnDefs();
        var colVals = this.GetColumnValues();
        for (int i = 0; i < cols.Count; i++)
        {
            var col = cols[i];
            var colVal = colVals[i];
            logRowStr += colVal.PadRight(col.ColWidth);
        }
        return logRowStr;
    }

    public string[] ToTableRow()
    {
        return this.GetColumnValues(shouldTruncateGuids: true).ToArray();
    }

}

public class TableColumn
{
    public int ColWidth { get; set; }
    public string ColName { get; set; } = "";
}
