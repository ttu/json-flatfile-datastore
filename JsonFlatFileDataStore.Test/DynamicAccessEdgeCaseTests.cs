namespace JsonFlatFileDataStore.Test;

/// <summary>
/// Edge cases for DataStore.GetItem when the value is an array (of objects, of arrays, mixed).
/// SingleDynamicItemReadConverter handles JArray by returning List&lt;object&gt; — pin the resulting
/// element types so the migration produces compatible structures.
/// </summary>
public class DynamicAccessEdgeCaseTests
{
    [Fact]
    public void GetItem_DynamicArrayOfObjects()
    {
        var path = UTHelpers.GetFullFilePath($"DynArrObj_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var things = new[]
        {
            new { id = 1, label = "A" },
            new { id = 2, label = "B" },
            new { id = 3, label = "C" }
        };
        store.InsertItem("things", things);

        store.Dispose();

        var store2 = new DataStore(path);
        var dyn = store2.GetItem("things");

        var list = dyn as System.Collections.IList;
        Assert.NotNull(list);
        Assert.Equal(3, list.Count);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public void GetItem_DynamicNestedArray()
    {
        var path = UTHelpers.GetFullFilePath($"DynNestArr_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        store.InsertItem("matrix", new[]
        {
            new[] { 1, 2, 3 },
            new[] { 4, 5, 6 },
            new[] { 7, 8, 9 }
        });

        store.Dispose();

        var store2 = new DataStore(path);
        var dyn = store2.GetItem("matrix");

        var outer = dyn as System.Collections.IList;
        Assert.NotNull(outer);
        Assert.Equal(3, outer.Count);

        // Round-trip through typed read for the actual value check (more stable).
        var typed = store2.GetItem<List<List<int>>>("matrix");
        Assert.Equal(3, typed.Count);
        Assert.Equal(new List<int> { 1, 2, 3 }, typed[0]);
        Assert.Equal(new List<int> { 7, 8, 9 }, typed[2]);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public void GetItem_EmptyArray_ReturnsEmptyList()
    {
        var path = UTHelpers.GetFullFilePath($"DynEmptyArr_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        store.InsertItem("empty", new int[] { });
        store.Dispose();

        var store2 = new DataStore(path);
        var dyn = store2.GetItem("empty");

        var list = dyn as System.Collections.IList;
        Assert.NotNull(list);
        Assert.Empty(list);

        store2.Dispose();
        UTHelpers.Down(path);
    }
}