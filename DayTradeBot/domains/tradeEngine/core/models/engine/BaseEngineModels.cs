namespace DayTradeBot.domains.tradeEngine.core;

public class PositionsCollection
{
    private List<Position>? PositionList { get; set; }
    private Dictionary<string, Position> PositionsBySymbol { get; set; }

    public PositionsCollection()
    {
        PositionList = null;
        PositionsBySymbol = new Dictionary<string, Position>();
    }

    public Position GetBySymbol(string symbol)
    {
        return PositionsBySymbol[symbol];
    }

    public void SetPosition(Position position)
    {
        PositionsBySymbol[position.Symbol] = position;
    }

    public Dictionary<string, Position>.KeyCollection Keys()
    {
        return PositionsBySymbol.Keys;
    }

    public Dictionary<string, Position> ToDictionary()
    {
        return PositionsBySymbol;
    }

    public void Sort()
    {
        if (PositionList == null)
        {
            PositionList = PositionsBySymbol.Select(kvp => kvp.Value).ToList();
        }
        PositionList = PositionsBySymbol.Select(kvp => kvp.Value).OrderBy(pos => pos.GetCurrentPctChange()).ToList();
    }

    public IEnumerable<Position> GetEnumerable()
    {
        if (PositionList == null)
        {
            throw new Exception("PositionList is null");
        }
        return PositionList;
    }

    public Position GetPositionWithBiggestPctDrop()
    {
        if (PositionList == null)
        {
            throw new Exception("PositionList is null");
        }

        var firstEntry = PositionList.FirstOrDefault();
        if (firstEntry == null)
        {
            throw new Exception("No entries in the PositionCollection");
        }
        return firstEntry;
    }

    public IEnumerable<Position> GetFirstXPositions(int ct)
    {
        if (PositionList == null)
        {
            throw new Exception("PositionList is null");
        }
        return PositionList.Take(ct);
    }
}