using System.Collections.ObjectModel;

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

        /// <summary>
        /// Moves an item to its correct position within an <see cref="IList{T}"/> while maintaining sorted order.
        /// The item must already exist in the list, and the rest of the list is expected to be sorted before calling this method.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="item">The item to move to its sorted position.</param>
        /// <param name="comparer"> An optional <see cref="Comparer{T}"/> to use for comparison. If null, the default comparer for type T is used. </param>
        public void MoveInSorted(T item, Comparer<T>? comparer = null)
        {
            var oldIndex = list.IndexOf(item);
            if (oldIndex < 0)
            {
                return;
            }

            comparer ??= Comparer<T>.Default;
            var lo = 0;
            var hi = list.Count - 1;

            while (lo <= hi)
            {
                var mid = (lo + hi) / 2;

                if (mid == oldIndex)
                {
                    if (mid == hi)
                    {
                        hi--;
                        continue;
                    }

                    mid++;
                }

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

            var newIndex = lo > oldIndex ? lo - 1 : lo;
            if (newIndex == oldIndex)
            {
                return;
            }

            if (list is ObservableCollection<T> observableList)
            {
                observableList.Move(oldIndex, newIndex);
            }
            else
            {
                list.RemoveAt(oldIndex);
                list.Insert(newIndex, item);
            }
        }
    }

    extension<T>(IEnumerable<T> enumerable)
    {
        /// <summary>
        /// Converts an <see cref="IEnumerable{T}"/> to an <see cref="ObservableCollection{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the enumerable.</typeparam>
        /// <returns>A new <see cref="ObservableCollection{T}"/> containing the elements from the enumerable.</returns>
        public ObservableCollection<T> ToObservableCollection()
        {
            return new ObservableCollection<T>(enumerable);
        }
    }
}
