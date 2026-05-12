using System.IO;

namespace JsonFlatFileDataStore.Test;

/// <summary>
/// Newtonsoft is case-insensitive when binding JSON property names to typed model
/// properties. System.Text.Json is case-sensitive by default. These tests pin
/// the current lenient read behavior so the migration must explicitly opt in or document a break.
/// </summary>
public class CaseSensitivityTests
{
    [Fact]
    public void ReadExistingFile_PascalCaseKeys_WithCamelCaseSetting_StillReadable()
    {
        var path = UTHelpers.GetFullFilePath($"PascalIn_{DateTime.UtcNow.Ticks}");

        // File written outside this library, using Pascal case for property names
        File.WriteAllText(path, "{ \"user\": [ { \"Id\": 1, \"Name\": \"Alice\", \"Age\": 30 } ] }");

        var store = new DataStore(path, useLowerCamelCase: true);
        var collection = store.GetCollection<User>("user");
        var user = collection.AsQueryable().First();

        Assert.Equal(1, user.Id);
        Assert.Equal("Alice", user.Name);
        Assert.Equal(30, user.Age);

        store.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public void ReadExistingFile_MixedCaseKeys_BehaviorPinned()
    {
        var path = UTHelpers.GetFullFilePath($"MixedIn_{DateTime.UtcNow.Ticks}");

        File.WriteAllText(path, "{ \"user\": [ { \"id\": 1, \"NAME\": \"BOB\", \"age\": 40 } ] }");

        var store = new DataStore(path, useLowerCamelCase: true);
        var collection = store.GetCollection<User>("user");
        var user = collection.AsQueryable().First();

        Assert.Equal(1, user.Id);
        Assert.Equal("BOB", user.Name);
        Assert.Equal(40, user.Age);

        store.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public void ReadExistingFile_DynamicAccess_CaseSensitive()
    {
        var path = UTHelpers.GetFullFilePath($"DynCase_{DateTime.UtcNow.Ticks}");

        File.WriteAllText(path, "{ \"user\": [ { \"id\": 1, \"Name\": \"Alice\" } ] }");

        var store = new DataStore(path, useLowerCamelCase: true);
        var collection = store.GetCollection("user");
        var user = collection.AsQueryable().First();

        // Dynamic access reflects the JSON keys verbatim — no implicit case folding.
        var dict = user as IDictionary<string, object>;
        Assert.NotNull(dict);
        Assert.True(dict.ContainsKey("Name"));
        Assert.False(dict.ContainsKey("name"));

        store.Dispose();
        UTHelpers.Down(path);
    }
}
