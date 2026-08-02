using System.Collections;

namespace InfraBot.Helpers;

internal static class EnumerableExtension
{
    internal static IEnumerable? GetBatchByNumber(this IEnumerable collection, int batchSize, int batchNumber)
    {
        if (collection == null)
            return null;

        var firstItemIndex = batchSize * batchNumber;
        var list = collection.Cast<object>().ToList();
        if (list.Count < firstItemIndex)
            return null;

        if (list.Count < firstItemIndex + batchSize)
            batchSize = list.Count - firstItemIndex;

        return list.GetRange(firstItemIndex, batchSize);
    }
}
