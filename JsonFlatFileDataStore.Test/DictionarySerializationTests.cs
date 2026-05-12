namespace JsonFlatFileDataStore.Test;

/// <summary>
/// Tests for Dictionary serialization, especially Dictionary with non-string keys
/// (int, Guid, enum) which System.Text.Json does NOT support by default.
/// Pin the current Newtonsoft round-trip behavior here.
/// </summary>
public class DictionarySerializationTests
{
    public class GuidKeyModel
    {
        public int Id { get; set; }
        public Dictionary<Guid, string> Lookup { get; set; }
    }

    public class EnumKeyModel
    {
        public int Id { get; set; }
        public Dictionary<Gender, int> Counts { get; set; }
    }

    [Fact]
    public async Task DictionaryStringString_RoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"DictStrStr_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection<PrivateOwner>("owner");
        await collection.InsertOneAsync(new PrivateOwner
        {
            FirstName = "Alice",
            OwnerLongTestProperty = "Test",
            MyValues = new List<int> { 1, 2, 3 },
            MyStrings = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" }
            },
            MyIntegers = new Dictionary<int, int>()
        });

        store.Dispose();

        var store2 = new DataStore(path);
        var collection2 = store2.GetCollection<PrivateOwner>("owner");
        var owner = collection2.AsQueryable().First();

        Assert.Equal("Alice", owner.FirstName);
        Assert.Equal(3, owner.MyStrings.Count);
        Assert.Equal("value1", owner.MyStrings["key1"]);
        Assert.Equal("value2", owner.MyStrings["key2"]);
        Assert.Equal("value3", owner.MyStrings["key3"]);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task DictionaryIntInt_RoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"DictIntInt_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection<PrivateOwner>("owner");
        await collection.InsertOneAsync(new PrivateOwner
        {
            FirstName = "Bob",
            OwnerLongTestProperty = "Test",
            MyValues = new List<int> { 10, 20 },
            MyStrings = new Dictionary<string, string>(),
            MyIntegers = new Dictionary<int, int>
            {
                { 1, 100 },
                { 2, 200 },
                { 3, 300 }
            }
        });

        store.Dispose();

        // This is the critical test: System.Text.Json does not natively support
        // Dictionary<int, int> keys — they must be serialized as strings in JSON
        var store2 = new DataStore(path);
        var collection2 = store2.GetCollection<PrivateOwner>("owner");
        var owner = collection2.AsQueryable().First();

        Assert.Equal("Bob", owner.FirstName);
        Assert.Equal(3, owner.MyIntegers.Count);
        Assert.Equal(100, owner.MyIntegers[1]);
        Assert.Equal(200, owner.MyIntegers[2]);
        Assert.Equal(300, owner.MyIntegers[3]);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task DictionaryIntInt_JsonRepresentation()
    {
        var path = UTHelpers.GetFullFilePath($"DictIntJson_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection<PrivateOwner>("owner");
        await collection.InsertOneAsync(new PrivateOwner
        {
            FirstName = "Charlie",
            OwnerLongTestProperty = "X",
            MyValues = new List<int>(),
            MyStrings = new Dictionary<string, string>(),
            MyIntegers = new Dictionary<int, int> { { 42, 999 } }
        });

        // Verify the JSON file has the dictionary serialized
        var json = UTHelpers.GetFileContent(path);
        Assert.Contains("42", json);
        Assert.Contains("999", json);

        store.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task DictionaryUpdate_CopyProperties_Works()
    {
        var path = UTHelpers.GetFullFilePath($"DictUpdate_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection<PrivateOwner>("owner");
        await collection.InsertOneAsync(new PrivateOwner
        {
            FirstName = "Dave",
            OwnerLongTestProperty = "Original",
            MyValues = new List<int> { 1 },
            MyStrings = new Dictionary<string, string> { { "a", "1" } },
            MyIntegers = new Dictionary<int, int> { { 1, 10 } }
        });

        // Update with new dictionary values
        await collection.UpdateOneAsync(
            e => e.FirstName == "Dave",
            new
            {
                MyStrings = new Dictionary<string, string> { { "b", "2" }, { "c", "3" } },
                MyIntegers = new Dictionary<int, int> { { 5, 50 }, { 6, 60 } }
            });

        store.Dispose();

        var store2 = new DataStore(path);
        var collection2 = store2.GetCollection<PrivateOwner>("owner");
        var owner = collection2.AsQueryable().First();

        Assert.Equal(2, owner.MyStrings.Count);
        Assert.Equal("2", owner.MyStrings["b"]);
        Assert.Equal("3", owner.MyStrings["c"]);
        Assert.Equal(2, owner.MyIntegers.Count);
        Assert.Equal(50, owner.MyIntegers[5]);
        Assert.Equal(60, owner.MyIntegers[6]);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task EmptyDictionaries_RoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"DictEmpty_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection<PrivateOwner>("owner");
        await collection.InsertOneAsync(new PrivateOwner
        {
            FirstName = "Eve",
            OwnerLongTestProperty = "Test",
            MyValues = new List<int>(),
            MyStrings = new Dictionary<string, string>(),
            MyIntegers = new Dictionary<int, int>()
        });

        store.Dispose();

        var store2 = new DataStore(path);
        var collection2 = store2.GetCollection<PrivateOwner>("owner");
        var owner = collection2.AsQueryable().First();

        Assert.NotNull(owner.MyStrings);
        Assert.Empty(owner.MyStrings);
        Assert.NotNull(owner.MyIntegers);
        Assert.Empty(owner.MyIntegers);
        Assert.NotNull(owner.MyValues);
        Assert.Empty(owner.MyValues);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task Dictionary_GuidKey_RoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"DictGuid_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var k1 = Guid.NewGuid();
        var k2 = Guid.NewGuid();

        var collection = store.GetCollection<GuidKeyModel>("model");
        await collection.InsertOneAsync(new GuidKeyModel
        {
            Id = 1,
            Lookup = new Dictionary<Guid, string>
            {
                { k1, "first" },
                { k2, "second" }
            }
        });

        store.Dispose();

        var store2 = new DataStore(path);
        var item = store2.GetCollection<GuidKeyModel>("model").AsQueryable().First();

        Assert.Equal(2, item.Lookup.Count);
        Assert.Equal("first", item.Lookup[k1]);
        Assert.Equal("second", item.Lookup[k2]);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task Dictionary_EnumKey_RoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"DictEnum_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection<EnumKeyModel>("model");
        await collection.InsertOneAsync(new EnumKeyModel
        {
            Id = 1,
            Counts = new Dictionary<Gender, int>
            {
                { Gender.Male, 10 },
                { Gender.Female, 20 }
            }
        });

        store.Dispose();

        var store2 = new DataStore(path);
        var item = store2.GetCollection<EnumKeyModel>("model").AsQueryable().First();

        Assert.Equal(2, item.Counts.Count);
        Assert.Equal(10, item.Counts[Gender.Male]);
        Assert.Equal(20, item.Counts[Gender.Female]);

        store2.Dispose();
        UTHelpers.Down(path);
    }
}
