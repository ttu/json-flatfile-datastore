namespace JsonFlatFileDataStore.Test;

public class SingleItemTests
{
    private const string EncryptionPassword = "t3stK3y139";

    private (string, DataStore) InitializeFileAndStore(string encryptionKey = null, bool reloadBeforeGetCollection = false)
    {
        var newFilePath = string.IsNullOrEmpty(encryptionKey) ? UTHelpers.Up() : UTHelpers.Up(encryptionKey: encryptionKey);
        var store = new DataStore(newFilePath, encryptionKey: encryptionKey, reloadBeforeGetCollection: reloadBeforeGetCollection);
        return (newFilePath, store);
    }

    private DataStore CreateStore(string filePath, string encryptionKey = null) => new DataStore(filePath, encryptionKey: encryptionKey);

    [Theory]
    [InlineData(null)]
    [InlineData(EncryptionPassword)]
    public void GetItem_DynamicAndTyped(string encryptionPassword)
    {
        var (newFilePath, store) = InitializeFileAndStore(encryptionPassword);

        var itemDynamic = store.GetItem("myUser");
        var itemTyped = store.GetItem<User>("myUser");

        Assert.Equal("Hank", itemDynamic.name);
        Assert.Equal("SF", itemDynamic.work.location);
        Assert.Equal("Hank", itemTyped.Name);
        Assert.Equal("SF", itemTyped.Work.Location);
        Assert.Equal(6.7, itemDynamic.value);

        UTHelpers.Down(newFilePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EncryptionPassword)]
    public void GetItem_DynamicAndTyped_SimpleType(string encryptionPassword)
    {
        var (newFilePath, store) = InitializeFileAndStore(encryptionPassword);

        var itemDynamic = store.GetItem("myValue");
        var itemTyped = store.GetItem<double>("myValue");

        Assert.Equal(2.1, itemDynamic);
        Assert.Equal(2.1, itemTyped);

        UTHelpers.Down(newFilePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EncryptionPassword)]
    public void GetItem_DynamicAndTyped_ArrayType(string encryptionPassword)
    {
        var (newFilePath, store) = InitializeFileAndStore(encryptionPassword);

        var itemDynamic = store.GetItem("myValues");
        var itemTyped = store.GetItem<List<double>>("myValues");

        Assert.Equal(3, itemDynamic.Count);
        Assert.Equal(2.1, itemDynamic[0]);
        Assert.Equal(3, itemTyped.Count);
        Assert.Equal(2.1, itemTyped.First());

        UTHelpers.Down(newFilePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EncryptionPassword)]
    public void GetItem_DynamicAndTyped_DateType(string encryptionPassword)
    {
        var (newFilePath, store) = InitializeFileAndStore(encryptionPassword);

        var test = DateTime.Now.ToShortDateString();

        // Typed: System.Text.Json deserializes date strings to DateTime
        var itemTyped = store.GetItem<DateTime>("myDate_string");
        Assert.Equal(2009, itemTyped.Year);

        // Dynamic: Now automatically parses date strings to DateTime (Newtonsoft.Json compatibility)
        var itemDynamic = store.GetItem("myDate_string");
        Assert.IsType<DateTime>(itemDynamic);
        Assert.Equal(2009, itemDynamic.Year);

        // Typed: System.Text.Json deserializes ISO date strings to DateTime
        var itemTyped2 = store.GetItem<DateTime>("myDate_date");
        Assert.Equal(2015, itemTyped2.Year);

        // Dynamic: Now automatically parses ISO date strings to DateTime (Newtonsoft.Json compatibility)
        var itemDynamic2 = store.GetItem("myDate_date");
        Assert.IsType<DateTime>(itemDynamic2);
        Assert.Equal(2015, itemDynamic2.Year);

        UTHelpers.Down(newFilePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EncryptionPassword)]
    public void GetItem_Nullable_NotFound(string encryptionPassword)
    {
        var (newFilePath, store) = InitializeFileAndStore(encryptionPassword);

        var result = store.GetItem<int?>("notFound");
        Assert.False(result.HasValue);
        Assert.Null(result);

        var result2 = store.GetItem<Guid?>("notFound");
        Assert.False(result2.HasValue);
        Assert.Null(result2);

        UTHelpers.Down(newFilePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EncryptionPassword)]
    public void NotFound_Exception(string encryptionPassword)
    {
        var (newFilePath, store) = InitializeFileAndStore(encryptionPassword);

        Assert.Throws<KeyNotFoundException>(() => store.GetItem<User>("notFound"));
        Assert.Throws<KeyNotFoundException>(() => store.GetItem<Guid>("notFound"));

        Assert.Null(store.GetItem("notFound"));
        Assert.Null(store.GetItem<Guid?>("notFound"));

        UTHelpers.Down(newFilePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EncryptionPassword)]
    public void InsertItem_TypedUser(string encryptionPassword)
    {
        var (newFilePath, store) = InitializeFileAndStore(encryptionPassword);

        var result = store.InsertItem("myUser2", new User { Id = 12, Name = "Teddy" });
        Assert.True(result);

        var user = store.GetItem<User>("myUser2");
        Assert.Equal("Teddy", user.Name);

        var store2 = CreateStore(newFilePath, encryptionPassword);

        var user2 = store2.GetItem<User>("myUser2");
        Assert.Equal("Teddy", user2.Name);

        var userDynamic = store2.GetItem("myUser2");
        Assert.Equal("Teddy", userDynamic.name);

        UTHelpers.Down(newFilePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EncryptionPassword)]
    public async Task InsertItem_DynamicUser(string encryptionPassword)
    {
        var (newFilePath, store) = InitializeFileAndStore(encryptionPassword);

        var result = await store.InsertItemAsync("myUser2", new { id = 12, name = "Teddy" });
        Assert.True(result);

        var user = store.GetItem("myUser2");
        Assert.Equal("Teddy", user.name);

        var store2 = CreateStore(newFilePath, encryptionPassword);

        var user2 = store2.GetItem("myUser2");
        Assert.Equal("Teddy", user2.name);

        UTHelpers.Down(newFilePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EncryptionPassword)]
    public async Task UpdateItem_DynamicUser(string encryptionPassword)
    {
        var (newFilePath, store) = InitializeFileAndStore(encryptionPassword);

        var result = await store.InsertItemAsync("myUser2", new { id = 12, name = "Teddy", Value = 2.1 });
        Assert.True(result);

        var user = store.GetItem("myUser2");
        Assert.Equal("Teddy", user.name);
        Assert.Equal(2.1, user.value);

        var updateResult = await store.UpdateItemAsync("myUser2", new { name = "Harold" });

        var store2 = CreateStore(newFilePath, encryptionPassword);

        var user2 = store2.GetItem("myUser2");
        Assert.Equal("Harold", user2.name);

        UTHelpers.Down(newFilePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EncryptionPassword)]
    public async Task UpdateItem_TypedUser(string encryptionPassword)
    {
        var (newFilePath, store) = InitializeFileAndStore(encryptionPassword);

        var result = await store.InsertItemAsync("myUser2", new User { Id = 12, Name = "Teddy" });
        Assert.True(result);

        var user = store.GetItem<User>("myUser2");
        Assert.Equal("Teddy", user.Name);

        var updateResult = await store.UpdateItemAsync("myUser2", new { name = "Harold" });

        var store2 = CreateStore(newFilePath, encryptionPassword);

        var user2 = store2.GetItem<User>("myUser2");
        Assert.Equal("Harold", user2.Name);

        UTHelpers.Down(newFilePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EncryptionPassword)]
    public async Task UpdateItem_ValueType(string encryptionPassword)
    {
        var (newFilePath, store) = InitializeFileAndStore(encryptionPassword);

        var result = await store.InsertItemAsync("counter", 2);
        Assert.True(result);

        var counter = store.GetItem<int>("counter");
        Assert.Equal(2, counter);

        var updateResult = await store.UpdateItemAsync("counter", "2");
        Assert.True(result);

        var store2 = CreateStore(newFilePath, encryptionPassword);

        var c2 = store2.GetItem("counter");
        Assert.Equal("2", c2);

        UTHelpers.Down(newFilePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EncryptionPassword)]
    public async Task ReplaceItem_ValueType(string encryptionPassword)
    {
        var (newFilePath, store) = InitializeFileAndStore(encryptionPassword);

        var result = await store.ReplaceItemAsync("counter", 2);
        Assert.False(result);

        result = await store.ReplaceItemAsync("counter", 2, true);
        Assert.True(result);

        var counter = store.GetItem<int>("counter");
        Assert.Equal(2, counter);

        var updateResult = await store.ReplaceItemAsync<string>("counter", "2");
        Assert.True(updateResult);

        var store2 = CreateStore(newFilePath, encryptionPassword);

        var c2 = store2.GetItem("counter");
        Assert.Equal("2", c2);

        updateResult = await store2.ReplaceItemAsync("counter", "4");
        Assert.True(updateResult);

        c2 = store2.GetItem("counter");
        Assert.Equal("4", c2);

        UTHelpers.Down(newFilePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EncryptionPassword)]
    public async Task DeleteItem_DynamicUser_NotFound(string encryptionPassword)
    {
        var (newFilePath, store) = InitializeFileAndStore(encryptionPassword, reloadBeforeGetCollection: true);

        var result = await store.DeleteItemAsync("myUser2");
        Assert.False(result);

        UTHelpers.Down(newFilePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EncryptionPassword)]
    public async Task UpdateItem_DynamicUser_NotFound(string encryptionPassword)
    {
        var (newFilePath, store) = InitializeFileAndStore(encryptionPassword, reloadBeforeGetCollection: true);

        var result = await store.UpdateItemAsync("myUser2", new { name = "James" });
        Assert.False(result);

        UTHelpers.Down(newFilePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EncryptionPassword)]
    public async Task ReplaceItem_DynamicUser_NotFound(string encryptionPassword)
    {
        var (newFilePath, store) = InitializeFileAndStore(encryptionPassword);

        var result = await store.ReplaceItemAsync("myUser2", new { id = 2, name = "James" });
        Assert.False(result);

        UTHelpers.Down(newFilePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EncryptionPassword)]
    public async Task ReplaceItem_DynamicUser_Upsert(string encryptionPassword)
    {
        var (newFilePath, store) = InitializeFileAndStore(encryptionPassword);

        var result = await store.ReplaceItemAsync("myUser2", new { id = 2, name = "James" }, true);
        Assert.True(result);

        var user = store.GetItem("myUser2");
        Assert.Equal("James", user.name);

        UTHelpers.Down(newFilePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EncryptionPassword)]
    public async Task ReplaceItem_TypedUser_Upsert(string encryptionPassword)
    {
        var (newFilePath, store) = InitializeFileAndStore(encryptionPassword);

        var result = await store.ReplaceItemAsync("myUser2", new User { Id = 2, Name = "James" }, true);
        Assert.True(result);

        var user = store.GetItem<User>("myUser2");
        Assert.Equal("James", user.Name);

        UTHelpers.Down(newFilePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EncryptionPassword)]
    public async Task ReplaceItem_DynamicUser(string encryptionPassword)
    {
        var (newFilePath, store) = InitializeFileAndStore(encryptionPassword);

        var result = store.InsertItem("myUser2", new { id = 12, name = "Teddy" });
        Assert.True(result);

        result = await store.ReplaceItemAsync("myUser2", new { id = 2, name = "James" });
        Assert.True(result);

        var user = store.GetItem("myUser2");
        Assert.Equal("James", user.name);

        UTHelpers.Down(newFilePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EncryptionPassword)]
    public async Task ReplaceItem_TypedUser(string encryptionPassword)
    {
        var (newFilePath, store) = InitializeFileAndStore(encryptionPassword);

        var result = store.InsertItem<User>("myUser2", new User { Id = 12, Name = "Teddy" });
        Assert.True(result);

        result = await store.ReplaceItemAsync("myUser2", new User { Id = 2, Name = "James" });
        Assert.True(result);

        var user = store.GetItem("myUser2");
        Assert.Equal("James", user.name);

        UTHelpers.Down(newFilePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EncryptionPassword)]
    public void DeleteItem_DynamicUser(string encryptionPassword)
    {
        var (newFilePath, store) = InitializeFileAndStore(encryptionPassword, reloadBeforeGetCollection: true);

        var user = store.GetItem("myUser2");
        Assert.Null(user);

        var result = store.InsertItem("myUser2", new { id = 12, name = "Teddy" });
        Assert.True(result);

        user = store.GetItem("myUser2");
        Assert.Equal("Teddy", user.name);

        var store2 = CreateStore(newFilePath, encryptionPassword);

        var deleteResult = store2.DeleteItem("myUser2");
        Assert.True(deleteResult);

        var user2 = store2.GetItem("myUser2");
        Assert.Null(user2);

        user = store.GetItem("myUser2");
        Assert.Null(user);

        UTHelpers.Down(newFilePath);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EncryptionPassword)]
    public void InsertItem_ComplexTypes_VerifiesJsonElementSerialization(string encryptionPassword)
    {
        // This test verifies that SetJsonDataElement and RemoveJsonDataElement work correctly
        // with Dictionary<string, JsonElement> serialization/deserialization in System.Text.Json
        var (newFilePath, store) = InitializeFileAndStore(encryptionPassword);

        // Test 1: Insert nested object
        var nestedUser = new
        {
            id = 100,
            name = "Complex User",
            metadata = new
            {
                tags = new[] { "admin", "developer" },
                settings = new Dictionary<string, object>
                {
                    { "theme", "dark" },
                    { "notifications", true },
                    { "maxItems", 50 }
                }
            }
        };

        var result1 = store.InsertItem("complexUser", nestedUser);
        Assert.True(result1);

        var retrieved1 = store.GetItem("complexUser");
        Assert.Equal("Complex User", retrieved1.name);
        Assert.Equal("admin", retrieved1.metadata.tags[0]);
        Assert.Equal("dark", retrieved1.metadata.settings.theme);
        Assert.True(retrieved1.metadata.settings.notifications);

        // Test 2: Insert array
        var arrayData = new[] { 1, 2, 3, 4, 5 };
        var result2 = store.InsertItem("numbers", arrayData);
        Assert.True(result2);

        var retrieved2 = store.GetItem("numbers");
        Assert.Equal(5, retrieved2.Count);
        Assert.Equal(3, (int)retrieved2[2]);

        // Test 3: Insert mixed type object
        var mixedData = new
        {
            stringVal = "test",
            intVal = 42,
            doubleVal = 3.14,
            boolVal = true,
            nullVal = (string)null,
            dateVal = new DateTime(2023, 1, 15)
        };

        var result3 = store.InsertItem("mixedTypes", mixedData);
        Assert.True(result3);

        var retrieved3 = store.GetItem("mixedTypes");
        Assert.Equal("test", retrieved3.stringVal);
        Assert.Equal(42, retrieved3.intVal);
        Assert.Equal(3.14, (double)retrieved3.doubleVal);
        Assert.True(retrieved3.boolVal);
        Assert.Null(retrieved3.nullVal);
        Assert.Equal(2023, ((DateTime)retrieved3.dateVal).Year);

        // Test 4: Delete items (tests RemoveJsonDataElement)
        var deleteResult1 = store.DeleteItem("complexUser");
        Assert.True(deleteResult1);
        Assert.Null(store.GetItem("complexUser"));

        var deleteResult2 = store.DeleteItem("numbers");
        Assert.True(deleteResult2);
        Assert.Null(store.GetItem("numbers"));

        var deleteResult3 = store.DeleteItem("mixedTypes");
        Assert.True(deleteResult3);
        Assert.Null(store.GetItem("mixedTypes"));

        UTHelpers.Down(newFilePath);
    }
}