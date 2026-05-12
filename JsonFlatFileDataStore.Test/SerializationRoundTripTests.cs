namespace JsonFlatFileDataStore.Test;

/// <summary>
/// Tests that validate serialization/deserialization behavior critical for
/// ensuring parity when switching JSON serializers (e.g., Newtonsoft → System.Text.Json).
/// </summary>
public class SerializationRoundTripTests
{
    [Fact]
    public void DynamicNumberTypes_IntegerPreservedAfterRoundTrip()
    {
        var newFilePath = UTHelpers.Up();
        var store = new DataStore(newFilePath);

        var collection = store.GetCollection("user");
        var firstUser = collection.AsQueryable().First();

        // Newtonsoft deserializes JSON integers as Int64 in dynamic/ExpandoObject mode.
        // System.Text.Json returns JsonElement instead — must ensure numeric types remain usable.
        long id = firstUser.id;
        long age = firstUser.age;
        Assert.Equal(1L, id);
        Assert.Equal(40L, age);

        // Verify arithmetic works on dynamic numeric values
        Assert.Equal(2L, id + 1);
        Assert.Equal(41L, age + 1);

        // Verify the underlying type is Int64, not JsonElement or other wrapper
        Assert.IsType<long>(firstUser.id);
        Assert.IsType<long>(firstUser.age);

        UTHelpers.Down(newFilePath);
    }

    [Fact]
    public void DynamicNumberTypes_IntegerPreservedAfterFileReload()
    {
        var path = UTHelpers.GetFullFilePath($"DynNumReload_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection("items");
        collection.InsertOne(new { id = 1, count = 42, score = 99 });
        store.Dispose();

        // Reload from file in a new DataStore instance
        var store2 = new DataStore(path);
        var collection2 = store2.GetCollection("items");
        var item = collection2.AsQueryable().First();

        // Newtonsoft returns Int64 for dynamic integer fields
        long id = item.id;
        long count = item.count;
        long score = item.score;
        Assert.Equal(1L, id);
        Assert.Equal(42L, count);
        Assert.Equal(99L, score);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public void DynamicNestedObject_PreservedAsExpandoObject()
    {
        var newFilePath = UTHelpers.Up();
        var store = new DataStore(newFilePath);

        var collection = store.GetCollection("user");
        var user = collection.AsQueryable().First();

        // Nested object should be accessible as dynamic, not as a raw JSON token
        Assert.NotNull(user.work);
        string workName = user.work.name;
        string workLocation = user.work.location;
        Assert.Equal("ACME", workName);
        Assert.Equal("NY", workLocation);

        UTHelpers.Down(newFilePath);
    }

    [Fact]
    public async Task DynamicNestedObject_RoundTrip_AfterInsertAndReload()
    {
        var path = UTHelpers.GetFullFilePath($"DynNested_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection("people");
        await collection.InsertOneAsync(new
        {
            id = 1,
            name = "Alice",
            address = new
            {
                street = "123 Main St",
                city = "Springfield",
                zip = 62704
            }
        });

        store.Dispose();

        // Reload and verify nested structure
        var store2 = new DataStore(path);
        var collection2 = store2.GetCollection("people");
        var person = collection2.AsQueryable().First();

        Assert.Equal("Alice", (string)person.name);
        Assert.Equal("123 Main St", (string)person.address.street);
        Assert.Equal("Springfield", (string)person.address.city);
        long zip = person.address.zip;
        Assert.Equal(62704L, zip);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task DynamicMixedTypes_AllTypesPreservedAfterRoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"DynMixed_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection("data");
        await collection.InsertOneAsync(new
        {
            id = 1,
            stringVal = "hello",
            intVal = 42,
            doubleVal = 3.14,
            boolVal = true,
            nullVal = (string)null
        });

        store.Dispose();

        var store2 = new DataStore(path);
        var collection2 = store2.GetCollection("data");
        var item = collection2.AsQueryable().First();

        Assert.Equal("hello", (string)item.stringVal);
        Assert.Equal(42, (int)(long)item.intVal);
        Assert.Equal(3.14, (double)item.doubleVal);
        Assert.True((bool)item.boolVal);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task TypedEnum_RoundTrip_PreservesValue()
    {
        var path = UTHelpers.GetFullFilePath($"EnumRT_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection<Family>("family");
        await collection.InsertOneAsync(new Family
        {
            Id = 100,
            FamilyName = "TestFamily",
            Parents = new List<Parent>
            {
                new Parent { Id = 1, Name = "Dad", Gender = Gender.Male, Age = 35 },
                new Parent { Id = 2, Name = "Mom", Gender = Gender.Female, Age = 33 }
            },
            Children = new List<Child>()
        });

        store.Dispose();

        // Reload and verify enum values
        var store2 = new DataStore(path);
        var collection2 = store2.GetCollection<Family>("family");
        var family = collection2.AsQueryable().First(f => f.Id == 100);

        Assert.Equal(Gender.Male, family.Parents[0].Gender);
        Assert.Equal(Gender.Female, family.Parents[1].Gender);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task DynamicArrayField_RoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"DynArr_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        store.InsertItem("tags", new List<string> { "alpha", "beta", "gamma" });
        store.InsertItem("numbers", new List<int> { 10, 20, 30 });
        store.Dispose();

        var store2 = new DataStore(path);

        var tags = store2.GetItem<List<string>>("tags");
        Assert.Equal(3, tags.Count);
        Assert.Equal("alpha", tags[0]);
        Assert.Equal("beta", tags[1]);
        Assert.Equal("gamma", tags[2]);

        var numbers = store2.GetItem<List<int>>("numbers");
        Assert.Equal(3, numbers.Count);
        Assert.Equal(10, numbers[0]);
        Assert.Equal(20, numbers[1]);
        Assert.Equal(30, numbers[2]);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task AnonymousType_InsertAndRead_RoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"AnonRT_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection("things");
        collection.InsertOne(new { id = 1, label = "Widget", quantity = 5, active = true });
        store.Dispose();

        var store2 = new DataStore(path);
        var collection2 = store2.GetCollection("things");
        var item = collection2.AsQueryable().First();

        Assert.Equal(1, (int)(long)item.id);
        Assert.Equal("Widget", (string)item.label);
        Assert.Equal(5, (int)(long)item.quantity);
        Assert.True((bool)item.active);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task BooleanValue_RoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"BoolRT_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        store.InsertItem("flag", true);
        store.Dispose();

        var store2 = new DataStore(path);
        var flag = store2.GetItem<bool>("flag");
        Assert.True(flag);

        var dynamicFlag = store2.GetItem("flag");
        Assert.True((bool)dynamicFlag);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task StringEscaping_RoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"Escape_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path, useLowerCamelCase: true);

        var collection = store.GetCollection("items");
        await collection.InsertOneAsync(new { id = 1, text = "He said \"hello\"" });
        await collection.InsertOneAsync(new { id = 2, text = "path\\to\\file" });
        await collection.InsertOneAsync(new { id = 3, text = "line1\nline2" });

        store.Dispose();

        var store2 = new DataStore(path);
        var collection2 = store2.GetCollection("items");
        var items = collection2.AsQueryable().ToList();

        Assert.Equal("He said \"hello\"", (string)items[0].text);
        Assert.Equal("path\\to\\file", (string)items[1].text);
        Assert.Equal("line1\nline2", (string)items[2].text);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task UnicodeStrings_RoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"Unicode_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path, useLowerCamelCase: true);

        var collection = store.GetCollection("items");
        await collection.InsertOneAsync(new { id = 1, text = "日本語テスト" });
        await collection.InsertOneAsync(new { id = 2, text = "émojis: 🎉" });
        await collection.InsertOneAsync(new { id = 3, text = "Ñoño" });

        store.Dispose();

        var store2 = new DataStore(path);
        var collection2 = store2.GetCollection("items");
        var items = collection2.AsQueryable().ToList();

        Assert.Equal("日本語テスト", (string)items[0].text);
        Assert.Equal("émojis: 🎉", (string)items[1].text);
        Assert.Equal("Ñoño", (string)items[2].text);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task EncryptedCollection_RoundTrip_PreservesData()
    {
        const string encryptionKey = "t3stK3y139";
        var path = UTHelpers.GetFullFilePath($"EncColl_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path, encryptionKey: encryptionKey);

        var collection = store.GetCollection<User>("user");
        await collection.InsertOneAsync(new User
        {
            Id = 1,
            Name = "Secret",
            Age = 42,
            Work = new WorkPlace { Name = "Vault", Location = "Underground" }
        });

        store.Dispose();

        // File on disk must not contain plaintext
        var raw = UTHelpers.GetFileContent(path);
        Assert.DoesNotContain("Secret", raw);

        // Reload with key — data must survive round-trip
        var store2 = new DataStore(path, encryptionKey: encryptionKey);
        var collection2 = store2.GetCollection<User>("user");
        var user = collection2.AsQueryable().First();

        Assert.Equal("Secret", user.Name);
        Assert.Equal(42, user.Age);
        Assert.Equal("Vault", user.Work.Name);

        store2.Dispose();
        UTHelpers.Down(path);
    }
}
