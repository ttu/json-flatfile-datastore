using System.Text.RegularExpressions;

namespace JsonFlatFileDataStore.Test;

/// <summary>
/// Golden file / exact JSON output tests to catch formatting differences
/// between serializers (property order, number formatting, whitespace).
/// Some tests use byte-equivalent assertions — the migration to System.Text.Json must either
/// produce identical output for these inputs, or the assertion is updated and the diff is
/// consciously accepted as part of the migration PR.
/// </summary>
public class JsonOutputFormatTests
{
    [Fact]
    public async Task Golden_Minified_TypedModel_ExactBytes()
    {
        var path = UTHelpers.GetFullFilePath($"GoldMini_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path, useLowerCamelCase: true, minifyJson: true);

        var collection = store.GetCollection<Movie>("movie");
        await collection.InsertOneAsync(new Movie { Name = "Predator", Rating = 7.5 });
        await collection.InsertOneAsync(new Movie { Name = "Aliens", Rating = 8.4 });

        store.Dispose();

        var actual = UTHelpers.GetFileContent(path);
        const string expected = "{\"movie\":[{\"name\":\"Predator\",\"rating\":7.5},{\"name\":\"Aliens\",\"rating\":8.4}]}";

        Assert.Equal(expected, actual);

        UTHelpers.Down(path);
    }

    [Fact]
    public async Task Golden_Indented_SingleItem_ExactBytes()
    {
        var path = UTHelpers.GetFullFilePath($"GoldInd_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path, useLowerCamelCase: true);

        await store.InsertItemAsync("counter", 42);

        store.Dispose();

        var actual = UTHelpers.GetFileContent(path).Replace("\r\n", "\n").Replace("\r", "\n");

        // Indented output, lower camel case key. The two-space indent matches Newtonsoft's
        // Formatting.Indented default. Pin the structure here — body content, not just substrings.
        const string expected = "{\n  \"counter\": 42\n}";
        Assert.Equal(expected, actual);

        UTHelpers.Down(path);
    }

    [Fact]
    public async Task Golden_Minified_NestedDynamicObject_ExactBytes()
    {
        var path = UTHelpers.GetFullFilePath($"GoldNest_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path, useLowerCamelCase: true, minifyJson: true);

        var collection = store.GetCollection("user");
        await collection.InsertOneAsync(new
        {
            id = 1,
            name = "Alice",
            work = new { name = "ACME", location = "NY" }
        });

        store.Dispose();

        var actual = UTHelpers.GetFileContent(path);
        const string expected = "{\"user\":[{\"id\":1,\"name\":\"Alice\",\"work\":{\"name\":\"ACME\",\"location\":\"NY\"}}]}";

        Assert.Equal(expected, actual);

        UTHelpers.Down(path);
    }

    [Fact]
    public async Task GoldenFile_Minified()
    {
        var path = UTHelpers.GetFullFilePath($"Golden_Mini_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path, useLowerCamelCase: true, minifyJson: true);

        var collection = store.GetCollection<Movie>("movie");
        await collection.InsertOneAsync(new Movie { Name = "X", Rating = 1.0 });

        var json = UTHelpers.GetFileContent(path);

        // Minified should have no newlines or indentation
        Assert.DoesNotContain("\n", json);
        Assert.DoesNotContain("  ", json);

        // But should still have correct content
        Assert.Contains("\"movie\"", json);
        Assert.Contains("\"name\":\"X\"", json);

        store.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task GoldenFile_NestedObject_PropertyOrder()
    {
        var path = UTHelpers.GetFullFilePath($"Golden_Nested_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path, useLowerCamelCase: true);

        var collection = store.GetCollection<User>("user");
        await collection.InsertOneAsync(new User
        {
            Id = 1,
            Name = "Alice",
            Age = 30,
            Location = "NY",
            Work = new WorkPlace { Name = "ACME", Location = "NY" }
        });

        var json = UTHelpers.GetFileContent(path);
        var normalized = json.Replace("\r\n", "\n").Replace("\r", "\n");

        // Verify nested object structure
        Assert.Contains("\"work\":", normalized);
        Assert.Contains("\"name\": \"ACME\"", normalized);
        Assert.Contains("\"location\": \"NY\"", normalized);

        // Verify all property names are lowerCamelCase
        Assert.DoesNotContain("\"Id\":", normalized);
        Assert.DoesNotContain("\"Name\":", normalized);
        Assert.DoesNotContain("\"Age\":", normalized);
        Assert.DoesNotContain("\"Work\":", normalized);

        store.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task GoldenFile_IntegerFormatting()
    {
        var path = UTHelpers.GetFullFilePath($"Golden_Int_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path, useLowerCamelCase: true, minifyJson: true);

        var collection = store.GetCollection("items");
        await collection.InsertOneAsync(new { id = 1, count = 0, big = 999999 });

        var json = UTHelpers.GetFileContent(path);

        // Integers should not have decimal points
        Assert.Matches(new Regex("\"id\":1[,}\\]]"), json);
        Assert.Matches(new Regex("\"count\":0[,}\\]]"), json);
        Assert.Contains("999999", json);

        store.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task GoldenFile_BooleanFormatting()
    {
        var path = UTHelpers.GetFullFilePath($"Golden_Bool_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path, useLowerCamelCase: true, minifyJson: true);

        store.InsertItem("active", true);
        store.InsertItem("deleted", false);

        var json = UTHelpers.GetFileContent(path);

        // Booleans should be lowercase true/false (not True/False or 1/0)
        Assert.Contains("true", json);
        Assert.Contains("false", json);
        Assert.DoesNotContain("True", json);
        Assert.DoesNotContain("False", json);

        store.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task GoldenFile_NullFormatting()
    {
        var path = UTHelpers.GetFullFilePath($"Golden_Null_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path, useLowerCamelCase: true, minifyJson: true);

        var collection = store.GetCollection<User>("user");
        await collection.InsertOneAsync(new User
        {
            Id = 1,
            Name = "Test",
            Age = 25,
            Location = null,
            Work = null
        });

        var json = UTHelpers.GetFileContent(path);

        // Null values should be serialized as the JSON literal null
        Assert.Contains("null", json);

        store.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task GoldenFile_EmptyArray()
    {
        var path = UTHelpers.GetFullFilePath($"Golden_EmptyArr_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path, useLowerCamelCase: true, minifyJson: true);

        var collection = store.GetCollection<TestModelWithStringArray>("items");
        await collection.InsertOneAsync(new TestModelWithStringArray
        {
            Id = "1",
            Type = "test",
            Fragments = new List<string>()
        });

        var json = UTHelpers.GetFileContent(path);

        // Empty arrays should be serialized as []
        Assert.Contains("[]", json);

        store.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task GoldenFile_DoubleFormatting_FivePointZero_WrittenCorrectly()
    {
        var path = UTHelpers.GetFullFilePath($"DoubleFmt_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection<Movie>("movie");
        await collection.InsertOneAsync(new Movie { Name = "Test", Rating = 5.0 });

        var json = UTHelpers.GetFileContent(path);

        // System.Text.Json writes whole-number doubles without a trailing ".0" (e.g., 5 not 5.0).
        // Newtonsoft.Json preserved the trailing ".0". See README "Known Differences".
        Assert.Matches("\"rating\"\\s*:\\s*5(\\b|[^.0-9])", json);

        store.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task GoldenFile_EnumAsInteger()
    {
        var path = UTHelpers.GetFullFilePath($"Golden_Enum_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path, useLowerCamelCase: true);

        var collection = store.GetCollection<Family>("family");
        await collection.InsertOneAsync(new Family
        {
            Id = 1,
            FamilyName = "Test",
            Parents = new List<Parent>
            {
                new Parent { Id = 1, Name = "P1", Gender = Gender.Male, Age = 30 },
                new Parent { Id = 2, Name = "P2", Gender = Gender.Female, Age = 28 }
            },
            Children = new List<Child>()
        });

        var json = UTHelpers.GetFileContent(path);
        var normalized = json.Replace("\r\n", "\n").Replace("\r", "\n");

        // Enums should be serialized as integers by default
        Assert.Contains("\"gender\": 0", normalized);
        Assert.Contains("\"gender\": 1", normalized);

        // Should NOT be serialized as strings
        Assert.DoesNotContain("\"Male\"", normalized);
        Assert.DoesNotContain("\"Female\"", normalized);

        store.Dispose();
        UTHelpers.Down(path);
    }

}
