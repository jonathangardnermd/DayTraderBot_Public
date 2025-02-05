namespace DayTradeBot.common.util.dataStructureUtil;

public class DictOfLists<X, Y> where X : notnull
{
    protected Dictionary<X, IEnumerable<Y>> MapOfLists { get; set; } = new Dictionary<X, IEnumerable<Y>>();

    public DictOfLists()
    {

    }

    public DictOfLists(Dictionary<X, IEnumerable<Y>> mapOfLists)
    {
        MapOfLists = mapOfLists;
    }

    public Dictionary<X, IEnumerable<Y>> GetData()
    {
        return MapOfLists;
    }

    protected void Add(X key, Y item)
    {
        AddToDictOfLists(MapOfLists, key, objToAdd: item);
    }

    protected IEnumerable<P> ToListOfLambda<P>(Func<Y, P> lambda)
    {
        return DictOfListsToListOfLambda(MapOfLists, lambda);
    }

    public static void AddToDictOfLists(Dictionary<X, IEnumerable<Y>> dictOfLists, X key, Y objToAdd)
    {
        if (!dictOfLists.TryGetValue(key, out var someList))
        {
            someList = new List<Y>();
            dictOfLists[key] = someList;
        }

        var castedList = (List<Y>)someList;
        castedList.Add(objToAdd);
    }

    public static IEnumerable<P> DictOfListsToListOfLambda<P>(Dictionary<X, IEnumerable<Y>> dictOfLists, Func<Y, P> lambda)
    {
        return dictOfLists.SelectMany(kvp => kvp.Value.Select(lambda));
    }
}