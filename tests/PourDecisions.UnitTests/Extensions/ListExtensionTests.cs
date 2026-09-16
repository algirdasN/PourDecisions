using System.Collections.ObjectModel;
using PourDecisions.Shared.Extensions;

namespace PourDecisions.UnitTests.Extensions;

public class ListExtensionsTests
{
    // -------------------------------------------------------------------------
    // List<T> path (uses BinarySearch internally)
    // -------------------------------------------------------------------------

    [Fact]
    public void InsertIntoSorted_ConcreteList_EmptyList_InsertsItem()
    {
        var list = new List<int>();

        list.InsertIntoSorted(5);

        Assert.Equal([5], list);
    }

    [Fact]
    public void InsertIntoSorted_ConcreteList_InsertAtBeginning()
    {
        var list = new List<int> { 3, 5, 7 };

        list.InsertIntoSorted(1);

        Assert.Equal([1, 3, 5, 7], list);
    }

    [Fact]
    public void InsertIntoSorted_ConcreteList_InsertAtEnd()
    {
        var list = new List<int> { 1, 3, 5 };

        list.InsertIntoSorted(9);

        Assert.Equal([1, 3, 5, 9], list);
    }

    [Fact]
    public void InsertIntoSorted_ConcreteList_InsertInMiddle()
    {
        var list = new List<int> { 1, 3, 7, 9 };

        list.InsertIntoSorted(5);

        Assert.Equal([1, 3, 5, 7, 9], list);
    }

    [Fact]
    public void InsertIntoSorted_ConcreteList_InsertDuplicate_ListRemainsOrdered()
    {
        var list = new List<int> { 1, 3, 5, 7 };

        list.InsertIntoSorted(3);

        Assert.Equal(5, list.Count);
        // Duplicate is inserted adjacent to the existing value.
        Assert.Equal(3, list[1]);
        Assert.Equal(3, list[2]);
        // Everything else stays sorted.
        for (var i = 1; i < list.Count; i++)
        {
            Assert.True(list[i] >= list[i - 1]);
        }
    }

    [Fact]
    public void InsertIntoSorted_ConcreteList_WithCustomComparer()
    {
        // Descending order comparer
        var descending = Comparer<int>.Create((a, b) => b.CompareTo(a));
        var list = new List<int> { 9, 7, 5, 3, 1 };

        list.InsertIntoSorted(6, descending);

        Assert.Equal([9, 7, 6, 5, 3, 1], list);
    }

    [Fact]
    public void InsertIntoSorted_ConcreteList_StringsAlphabetical()
    {
        var list = new List<string> { "apple", "cherry", "mango" };

        list.InsertIntoSorted("banana");

        Assert.Equal(["apple", "banana", "cherry", "mango"], list);
    }

    // -------------------------------------------------------------------------
    // IList<T> path (custom binary search) — via ObservableCollection<T>
    // -------------------------------------------------------------------------

    [Fact]
    public void InsertIntoSorted_ObservableCollection_EmptyList_InsertsItem()
    {
        IList<int> list = new ObservableCollection<int>();

        list.InsertIntoSorted(5);

        Assert.Equal([5], list);
    }

    [Fact]
    public void InsertIntoSorted_ObservableCollection_InsertAtBeginning()
    {
        IList<int> list = new ObservableCollection<int> { 3, 5, 7 };

        list.InsertIntoSorted(1);

        Assert.Equal([1, 3, 5, 7], list);
    }

    [Fact]
    public void InsertIntoSorted_ObservableCollection_InsertAtEnd()
    {
        IList<int> list = new ObservableCollection<int> { 1, 3, 5 };

        list.InsertIntoSorted(9);

        Assert.Equal([1, 3, 5, 9], list);
    }

    [Fact]
    public void InsertIntoSorted_ObservableCollection_InsertInMiddle()
    {
        IList<int> list = new ObservableCollection<int> { 1, 3, 7, 9 };

        list.InsertIntoSorted(5);

        Assert.Equal([1, 3, 5, 7, 9], list);
    }

    [Fact]
    public void InsertIntoSorted_ObservableCollection_InsertDuplicate_ListRemainsOrdered()
    {
        IList<int> list = new ObservableCollection<int> { 1, 3, 5, 7 };

        list.InsertIntoSorted(3);

        Assert.Equal(5, list.Count);
        for (var i = 1; i < list.Count; i++)
        {
            Assert.True(list[i] >= list[i - 1]);
        }
    }

    [Fact]
    public void InsertIntoSorted_ObservableCollection_WithCustomComparer()
    {
        var descending = Comparer<int>.Create((a, b) => b.CompareTo(a));
        IList<int> list = new ObservableCollection<int> { 9, 7, 5, 3, 1 };

        list.InsertIntoSorted(6, descending);

        Assert.Equal([9, 7, 6, 5, 3, 1], list);
    }

    [Fact]
    public void InsertIntoSorted_ObservableCollection_StringsAlphabetical()
    {
        IList<string> list = new ObservableCollection<string> { "apple", "cherry", "mango" };

        list.InsertIntoSorted("banana");

        Assert.Equal(["apple", "banana", "cherry", "mango"], list);
    }

    // -------------------------------------------------------------------------
    // Both paths produce identical results
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(new[] { 1, 3, 5, 7, 9 }, 0)]
    [InlineData(new[] { 1, 3, 5, 7, 9 }, 2)]
    [InlineData(new[] { 1, 3, 5, 7, 9 }, 4)]
    [InlineData(new[] { 1, 3, 5, 7, 9 }, 5)]
    [InlineData(new[] { 1, 3, 5, 7, 9 }, 6)]
    [InlineData(new[] { 1, 3, 5, 7, 9 }, 10)]
    public void InsertIntoSorted_BothPaths_ProduceSameResult(int[] initial, int item)
    {
        var concreteList = new List<int>(initial);
        IList<int> observableList = new ObservableCollection<int>(initial);

        concreteList.InsertIntoSorted(item);
        observableList.InsertIntoSorted(item);

        Assert.Equal(concreteList, observableList);
    }

    // -------------------------------------------------------------------------
    // ToObservableCollection tests
    // -------------------------------------------------------------------------

    [Fact]
    public void ToObservableCollection_EmptyEnumerable_ReturnsEmptyCollection()
    {
        var enumerable = Enumerable.Empty<int>();

        var result = enumerable.ToObservableCollection();

        Assert.Empty(result);
        Assert.IsType<ObservableCollection<int>>(result);
    }

    [Fact]
    public void ToObservableCollection_NonEmptyEnumerable_ReturnsCollectionWithSameElements()
    {
        var enumerable = new[] { 1, 2, 3 };

        var result = enumerable.ToObservableCollection();

        Assert.Equal([1, 2, 3], result);
        Assert.IsType<ObservableCollection<int>>(result);
    }

    [Fact]
    public void ToObservableCollection_NullEnumerable_ThrowsArgumentNullException()
    {
        IEnumerable<int> enumerable = null!;

        Assert.Throws<ArgumentNullException>(() => enumerable.ToObservableCollection());
    }

    [Fact]
    public void ToObservableCollection_FromObservableCollection_ReturnsNewInstance()
    {
        var original = new ObservableCollection<int> { 1, 2 };

        var result = original.ToObservableCollection();

        Assert.NotSame(original, result);
        Assert.Equal(original, result);
    }
}
