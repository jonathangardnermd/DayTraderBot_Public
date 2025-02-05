namespace DayTradeBot.common.util.dataStructureUtil;

public class DictOfDictsOfLists<X, Y, Z>
    where X : notnull
    where Y : notnull
{
    protected Dictionary<X, Dictionary<Y, IEnumerable<Z>>> MapOfMapsOfLists { get; set; } = new Dictionary<X, Dictionary<Y, IEnumerable<Z>>>();

    public Dictionary<X, Dictionary<Y, IEnumerable<Z>>> GetData()
    {
        return MapOfMapsOfLists;
    }
    protected void Add(X primaryKey, Y secondaryKey, Z objToAdd)
    {
        AddToDictOfDictsOfLists(MapOfMapsOfLists, primaryKey, secondaryKey, objToAdd);
    }

    protected Dictionary<Y, IEnumerable<Z>> ToFilteredDictOfListsByConsolidatingPrimaryKey(Func<Z, bool> filterFxn)
    {
        var dictOfLists = MapOfMapsOfLists
            .SelectMany(outerKvp => outerKvp.Value)
            .GroupBy(innerKvp => innerKvp.Key, innerKvp => innerKvp.Value)
            .ToDictionary(g => g.Key, g => g.SelectMany(list => list).Where(filterFxn));

        return dictOfLists;
    }

    public IEnumerable<Z> Flatten()
    {
        return MapOfMapsOfLists.Values
            .SelectMany(innerDict => innerDict.Values)
            .SelectMany(innerEnumerable => innerEnumerable);
    }

    public static void AddToDictOfDictsOfLists(Dictionary<X, Dictionary<Y, IEnumerable<Z>>> dictOfDictsOfLists, X primaryKey, Y secondaryKey, Z objToAdd)
    {
        if (!dictOfDictsOfLists.TryGetValue(primaryKey, out var dictOfLists))
        {
            dictOfLists = new Dictionary<Y, IEnumerable<Z>>();
            dictOfDictsOfLists[primaryKey] = dictOfLists;
        }
        DictOfLists<Y, Z>.AddToDictOfLists(dictOfLists, secondaryKey, objToAdd);
    }
}