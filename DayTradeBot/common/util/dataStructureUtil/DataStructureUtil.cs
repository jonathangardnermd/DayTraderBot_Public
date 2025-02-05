namespace DayTradeBot.common.util.dataStructureUtil;

public static class DataStructureUtil
{
    public static int IndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        int index = 0;
        foreach (var item in source)
        {
            if (predicate(item))
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    public static void PerformActionOnEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }

}

