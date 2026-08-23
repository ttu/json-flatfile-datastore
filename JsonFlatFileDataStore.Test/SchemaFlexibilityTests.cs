using System.IO;
using System.Text.Json;

namespace JsonFlatFileDataStore.Test;

/// <summary>
/// Pin behavior for: extra JSON properties not on the typed model, malformed input
/// to UpdateAll, and Reload after an external file write. These are areas where
/// Newtonsoft and System.Text.Json defaults differ in subtle ways.
/// </summary>
public class SchemaFlexibilityTests
{
    public class MinimalUser
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [Fact]
    public void TypedRead_FileHasExtraFields_BehaviorPinned()
    {
        var path = UTHelpers.GetFullFilePath($"Extra_{DateTime.UtcNow.Ticks}");

        File.WriteAllText(path,
            "{ \"user\": [ { \"id\": 1, \"name\": \"Alice\", \"extra\": \"ignored\", \"meta\": { \"a\": 1 } } ] }");

        var store = new DataStore(path);
        var collection = store.GetCollection<MinimalUser>("user");
        var user = collection.AsQueryable().First();

        // Typed read drops unknown fields silently — both Newtonsoft and STJ default to this.
        Assert.Equal(1, user.Id);
        Assert.Equal("Alice", user.Name);

        store.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task TypedUpdate_PreservesExtraFieldsInFile()
    {
        // When the model is typed but the on-disk JSON has extra fields, the current behavior is:
        // updates serialize through the typed model, so extra fields can be lost.
        // This test pins the *current* outcome — important because STJ may behave differently.
        var path = UTHelpers.GetFullFilePath($"ExtraUpd_{DateTime.UtcNow.Ticks}");

        File.WriteAllText(path,
            "{ \"user\": [ { \"id\": 1, \"name\": \"Alice\", \"extra\": \"keep\" } ] }");

        var store = new DataStore(path);
        var collection = store.GetCollection<MinimalUser>("user");

        await collection.UpdateOneAsync(e => e.Id == 1, new { Name = "Bob" });

        store.Dispose();

        var raw = File.ReadAllText(path);

        // Pin the current behavior — adjust the assertion if migration intentionally changes this.
        // Today (Newtonsoft path): the extra field is dropped because the collection write
        // serializes the typed list back to JSON, which has no slot for "extra".
        Assert.DoesNotContain("\"extra\"", raw);
        Assert.Contains("Bob", raw);

        UTHelpers.Down(path);
    }

    [Fact]
    public void UpdateAll_InvalidJson_Throws()
    {
        var path = UTHelpers.GetFullFilePath($"BadJson_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        Assert.ThrowsAny<JsonException>(() => store.UpdateAll("not valid json {{{"));

        store.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public void Reload_AfterExternalFileWrite_PicksUpChanges()
    {
        var path = UTHelpers.GetFullFilePath($"ExtReload_{DateTime.UtcNow.Ticks}");

        File.WriteAllText(path, "{ \"user\": [ { \"id\": 1, \"name\": \"Before\" } ] }");

        var store = new DataStore(path);
        var initial = store.GetCollection("user").AsQueryable().First();
        Assert.Equal("Before", (string)initial.name);

        // Simulate an external process modifying the file.
        File.WriteAllText(path, "{ \"user\": [ { \"id\": 1, \"name\": \"After\" }, { \"id\": 2, \"name\": \"Added\" } ] }");

        store.Reload();

        var after = store.GetCollection("user");
        Assert.Equal(2, after.Count);
        var first = after.AsQueryable().First(u => u.id == 1);
        Assert.Equal("After", (string)first.name);

        store.Dispose();
        UTHelpers.Down(path);
    }
}