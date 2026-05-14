using System.Text.Json.Nodes;

namespace JsonFlatFileDataStore.Test;

/// <summary>
/// Pin behavior of public APIs that accept System.Text.Json.Nodes JsonNode/JsonObject/JsonArray
/// as input. The original tests targeted Newtonsoft's JToken/JObject/JArray; after the
/// Newtonsoft → System.Text.Json migration the equivalent contract is exercised here.
/// </summary>
public class JTokenInputTests
{
    [Fact]
    public async Task InsertOne_JsonNode_AnonymousObject_RoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"JTInsert_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var node = JsonNode.Parse("{ \"id\": 1, \"name\": \"Alice\", \"age\": 30 }");

        var collection = store.GetCollection("user");
        await collection.InsertOneAsync(node);

        store.Dispose();

        var store2 = new DataStore(path);
        var user = store2.GetCollection("user").AsQueryable().First();

        Assert.Equal(1, (int)(long)user.id);
        Assert.Equal("Alice", (string)user.name);
        Assert.Equal(30, (int)(long)user.age);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task UpdateOne_JsonNodePatch_AppliesCorrectly()
    {
        var path = UTHelpers.GetFullFilePath($"JTUpdate_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection("user");
        await collection.InsertOneAsync(new { id = 1, name = "Original", age = 20, location = "NY" });

        var patch = JsonNode.Parse("{ \"name\": \"Patched\", \"age\": 25 }");
        var updated = await collection.UpdateOneAsync((Predicate<dynamic>)(e => e.id == 1), patch);
        Assert.True(updated);

        store.Dispose();

        var store2 = new DataStore(path);
        var user = store2.GetCollection("user").AsQueryable().First();

        Assert.Equal("Patched", (string)user.name);
        Assert.Equal(25, (int)(long)user.age);
        Assert.Equal("NY", (string)user.location);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task ReplaceOne_JsonObject_NestedJsonArray()
    {
        var path = UTHelpers.GetFullFilePath($"JTReplace_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection("things");
        await collection.InsertOneAsync(new { id = 1, label = "old", tags = new[] { "a" } });

        var replacement = new JsonObject
        {
            ["id"] = 1,
            ["label"] = "new",
            ["tags"] = new JsonArray("x", "y", "z"),
            ["nested"] = new JsonArray(new JsonArray(1, 2), new JsonArray(3, 4))
        };

        var ok = await collection.ReplaceOneAsync((Predicate<dynamic>)(e => e.id == 1), replacement);
        Assert.True(ok);

        store.Dispose();

        var store2 = new DataStore(path);
        var item = store2.GetCollection("things").AsQueryable().First();

        Assert.Equal("new", (string)item.label);
        Assert.Equal(3, ((System.Collections.IList)item.tags).Count);
        Assert.Equal("x", (string)item.tags[0]);
        Assert.Equal(2, ((System.Collections.IList)item.nested).Count);
        Assert.Equal(2, (int)(long)item.nested[0][1]);
        Assert.Equal(4, (int)(long)item.nested[1][1]);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task InsertMany_JsonArray_AllItemsInserted()
    {
        var path = UTHelpers.GetFullFilePath($"JTMany_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var json = "[ { \"id\": 1, \"name\": \"A\" }, { \"id\": 2, \"name\": \"B\" }, { \"id\": 3, \"name\": \"C\" } ]";
        var array = (JsonArray)JsonNode.Parse(json);

        var collection = store.GetCollection("items");
        await collection.InsertManyAsync(array);

        store.Dispose();

        var store2 = new DataStore(path);
        Assert.Equal(3, store2.GetCollection("items").Count);

        store2.Dispose();
        UTHelpers.Down(path);
    }
}
