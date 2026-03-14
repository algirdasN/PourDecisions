namespace PourDecisions.Shared.Extensions;

/// <summary>
/// Provides extension methods for list and collection types.
/// </summary>
public static class ListExtensions
{
    extension<T>(IList<T> list)
    {
        /// <summary>
        /// Inserts an item into an <see cref="IList{T}"/> while maintaining sorted order. The list is expected to already be sorted before calling this method. 
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="item">The item to insert.</param>
        /// <param name="comparer">An optional <see cref="Comparer{T}"/> to use for comparison. If null, the default comparer for type T is used.</param>
        public void InsertIntoSorted(T item, Comparer<T>? comparer = null)
        {
            int index;
            if (list is List<T> concreteList)
            {
                index = concreteList.BinarySearch(item, comparer);
            }
            else
            {
                comparer ??= Comparer<T>.Default;
                var lo = 0;
                var hi = list.Count - 1;
                while (lo <= hi)
                {
                    var mid = (lo + hi) / 2;
                    var cmp = comparer.Compare(list[mid], item);

                    if (cmp < 0)
                    {
                        lo = mid + 1;
                    }
                    else if (cmp > 0)
                    {
                        hi = mid - 1;
                    }
                    else
                    {
                        lo = mid;
                        break;
                    }
                }

                index = lo;
            }

            if (index < 0)
            {
                index = ~index;
            }

            list.Insert(index, item);
        }
    }
}
