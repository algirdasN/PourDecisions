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

    [Fact]
    public void MoveInSorted_ItemNotFound_ListRemainsUnchanged()
    {
        var item1 = new Item("Apple");
        var item2 = new Item("Banana");
        var notInList = new Item("Cherry");
        var list = new ObservableCollection<Item> { item1, item2 };
        var comparer = Comparer<Item>.Create((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

        list.MoveInSorted(notInList, comparer);

        Assert.Equal([item1, item2], list);
    }

    [Fact]
    public void MoveInSorted_AlreadyInCorrectPosition_ListRemainsUnchanged()
    {
        var item1 = new Item("Apple");
        var item2 = new Item("Banana");
        var item3 = new Item("Cherry");
        var list = new ObservableCollection<Item> { item1, item2, item3 };
        var comparer = Comparer<Item>.Create((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

        list.MoveInSorted(item2, comparer);

        Assert.Equal([item1, item2, item3], list);
    }

    [Fact]
    public void MoveInSorted_ObservableCollection_MoveToEarlierPosition()
    {
        var item1 = new Item("Apple");
        var item2 = new Item("Banana");
        var item3 = new Item("Cherry");
        var list = new ObservableCollection<Item> { item1, item2, item3 };
        var comparer = Comparer<Item>.Create((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

        // Mutate item3 so it belongs before item1
        item3.Name = "Aardvark";
        list.MoveInSorted(item3, comparer);

        Assert.Equal([item3, item1, item2], list);
    }

    [Fact]
    public void MoveInSorted_ObservableCollection_MoveToLaterPosition()
    {
        var item1 = new Item("Apple");
        var item2 = new Item("Banana");
        var item3 = new Item("Cherry");
        var list = new ObservableCollection<Item> { item1, item2, item3 };
        var comparer = Comparer<Item>.Create((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

        // Mutate item1 so it belongs after item3
        item1.Name = "Date";
        list.MoveInSorted(item1, comparer);

        Assert.Equal([item2, item3, item1], list);
    }

    [Fact]
    public void MoveInSorted_ConcreteList_MoveToEarlierPosition()
    {
        var item1 = new Item("Apple");
        var item2 = new Item("Banana");
        var item3 = new Item("Cherry");
        IList<Item> list = new List<Item> { item1, item2, item3 };
        var comparer = Comparer<Item>.Create((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

        item3.Name = "Aardvark";
        list.MoveInSorted(item3, comparer);

        Assert.Equal([item3, item1, item2], list);
    }

    [Fact]
    public void MoveInSorted_ConcreteList_MoveToLaterPosition()
    {
        var item1 = new Item("Apple");
        var item2 = new Item("Banana");
        var item3 = new Item("Cherry");
        IList<Item> list = new List<Item> { item1, item2, item3 };
        var comparer = Comparer<Item>.Create((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

        item1.Name = "Date";
        list.MoveInSorted(item1, comparer);

        Assert.Equal([item2, item3, item1], list);
    }

    [Fact]
    public void MoveInSorted_MoveToMiddle()
    {
        var item1 = new Item("Apple");
        var item2 = new Item("Cherry");
        var item3 = new Item("Elderberry");
        var list = new ObservableCollection<Item> { item1, item2, item3 };
        var comparer = Comparer<Item>.Create((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

        // Mutate item3 so it belongs between Apple and Cherry
        item3.Name = "Banana";
        list.MoveInSorted(item3, comparer);

        Assert.Equal([item1, item3, item2], list);
    }

    [Fact]
    public void MoveInSorted_WithDefaultComparer_Ints()
    {
        var list = new ObservableCollection<int> { 10, 20, 30, 40 };
        // Replace element at index 3 with 15 (simulated by having 15 in the collection)
        list[3] = 15;
        list.MoveInSorted(15);

        Assert.Equal([10, 15, 20, 30], list);
    }

    [Fact]
    public void MoveInSorted_WithCustomDescendingComparer()
    {
        var descending = Comparer<int>.Create((a, b) => b.CompareTo(a));
        var list = new ObservableCollection<int> { 40, 30, 20, 10 };
        list[0] = 15;
        list.MoveInSorted(15, descending);

        Assert.Equal([30, 20, 15, 10], list);
    }

    // -------------------------------------------------------------------------
    // MoveInSorted tests
    // -------------------------------------------------------------------------

    private class Item(string name)
    {
        public string Name { get; set; } = name;
    }
}
