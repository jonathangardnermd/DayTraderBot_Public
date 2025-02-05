namespace DayTradeBot.common.util.dataStructureUtil;

public class DictOfDictsOfSingleValues<X, Y, Z>
    where X : notnull
    where Y : notnull
{
    protected Dictionary<X, Dictionary<Y, Z>> MapOfMapsOfSingleValues { get; set; } = new Dictionary<X, Dictionary<Y, Z>>();

    public Dictionary<X, Dictionary<Y, Z>> GetData()
    {
        return MapOfMapsOfSingleValues;
    }

    public static void SetInDictOfDictsOfSingleValues(Dictionary<X, Dictionary<Y, Z>> dictOfDictsOfSingleValues, X primaryKey, Y secondaryKey, Z newValue)
    {
        if (!dictOfDictsOfSingleValues.TryGetValue(primaryKey, out var dictOfSingleValues))
        {
            dictOfSingleValues = new Dictionary<Y, Z>();
            dictOfDictsOfSingleValues[primaryKey] = dictOfSingleValues;
        }

        dictOfSingleValues[secondaryKey] = newValue;
    }
}