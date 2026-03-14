namespace PourDecisions.Shared.Extensions;

/// <summary>
/// Provides extension methods for list and collection types.
/// </summary>
public static class ListExtensions
{
    extension<T>(List<T> list)
    {
        /// <summary>
        /// Inserts an item into a <see cref="List{T}"/> while maintaining sorted order. The list is expected to already be sorted before calling this method.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="item">The item to insert.</param>
        /// <param name="comparer">An optional <see cref="Comparer{T}"/> to use for comparison. If null, the default comparer for type T is used.</param>
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
        /// <summary>
        /// Inserts an item into an <see cref="IList{T}"/> while maintaining sorted order. The list is expected to already be sorted before calling this method. 
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="item">The item to insert.</param>
        /// <param name="comparer">An optional <see cref="Comparer{T}"/> to use for comparison. If null, the default comparer for type T is used.</param>
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
