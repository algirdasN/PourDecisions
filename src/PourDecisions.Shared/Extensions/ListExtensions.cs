namespace PourDecisions.Shared.Extensions;

public static class ListExtensions
{
    extension<T>(List<T> list)
    {
        public void InsertIntoSorted(T item, Comparer<T>? comparer = null)
        {
            var index = list.BinarySearch(item, comparer);
            if (index < 0)
            {
                index = ~index;
            }

            list.Insert(index, item);
        }
    }

    extension<T>(IList<T> list)
    {
        public void InsertIntoSorted(T item, Comparer<T>? comparer = null)
        {
            var index = list.ToList().BinarySearch(item, comparer);
            if (index < 0)
            {
                index = ~index;
            }

            list.Insert(index, item);
        }
    }
}
