using System.Text.Json.Serialization;

namespace JsonFlatFileDataStore.Test;

/// <summary>
/// Pin current Newtonsoft-specific quirks of the library so the migration to
/// System.Text.Json must explicitly match or document a deliberate change.
/// </summary>
public class BehaviorPinningTests
{
    [Fact]
    public async Task UpdateAll_ExistingCollectionRef_BehaviorPinned()
    {
        var path = UTHelpers.GetFullFilePath($"UpdAllRef_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection("user");
        await collection.InsertOneAsync(new { id = 1, name = "Original" });
        Assert.Equal(1, collection.Count);

        store.UpdateAll("{ \"user\": [ { \"id\": 99, \"name\": \"Replaced\" } ] }");

        // The previously-obtained collection reference uses a Lazy<List<T>> snapshot
        // and does not see UpdateAll changes. A freshly obtained collection does.
        var fresh = store.GetCollection("user");
        Assert.Equal(1, fresh.Count);
        Assert.Equal(99, (int)(long)fresh.AsQueryable().First().id);

        // Pin: the previously-obtained reference still holds the pre-UpdateAll snapshot.
        Assert.Equal(1, collection.Count);
        Assert.Equal(1, (int)(long)collection.AsQueryable().First().id);

        store.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public void AnonymousType_IdZero_BehaviorPinned()
    {
        // Anonymous type with explicit id = 0 (default int). Pin whether the library
        // overwrites this or treats 0 as a "real" id and keeps it.
        var path = UTHelpers.GetFullFilePath($"AnonIdZero_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection("items");
        collection.InsertOne(new { id = 0, name = "First" });
        collection.InsertOne(new { id = 0, name = "Second" });

        var items = collection.AsQueryable().ToList();
        Assert.Equal(2, items.Count);

        // Both items keep id = 0 (the user-provided value is trusted for anonymous types
        // that already include the id field).
        Assert.All(items, e => Assert.Equal(0, (int)(long)e.id));

        store.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public void AnonymousType_StringIdNonNumeric_BehaviorPinned()
    {
        // ParseNextIntegerToKeyValue handles non-numeric string ids by appending "0".
        var path = UTHelpers.GetFullFilePath($"AnonStrId_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path, keyProperty: "code");

        var collection = store.GetCollection("items");
        collection.InsertOne(new { code = "abc", label = "First" });

        var nextId = collection.GetNextIdValue();
        Assert.Equal("abc0", nextId);

        collection.InsertOne(new { label = "Second" });

        var nextId2 = collection.GetNextIdValue();
        Assert.Equal("abc1", nextId2);

        store.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task EnumStringConverter_BehaviorPinned()
    {
        // A typed model attribute-decorated to serialize an enum as a string.
        // Pin whether such a custom converter survives a round trip via the library.
        var path = UTHelpers.GetFullFilePath($"EnumStr_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection<ModelWithStringEnum>("model");
        await collection.InsertOneAsync(new ModelWithStringEnum { Id = 1, Status = StatusValue.Active });

        var json = UTHelpers.GetFileContent(path);

        // Newtonsoft honors the [JsonConverter] attribute — value is written as a string.
        Assert.Contains("\"Active\"", json);

        store.Dispose();

        var store2 = new DataStore(path);
        var item = store2.GetCollection<ModelWithStringEnum>("model").AsQueryable().First();
        Assert.Equal(StatusValue.Active, item.Status);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    public enum StatusValue
    {
        Inactive,
        Active,
        Pending
    }

    public class ModelWithStringEnum
    {
        public int Id { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public StatusValue Status { get; set; }
    }
}