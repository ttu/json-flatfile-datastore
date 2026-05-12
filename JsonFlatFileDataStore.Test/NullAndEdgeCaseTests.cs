namespace JsonFlatFileDataStore.Test;

/// <summary>
/// Tests for null handling, empty collections, and edge cases that may differ
/// between Newtonsoft and System.Text.Json serializers.
/// </summary>
public class NullAndEdgeCaseTests
{
    [Fact]
    public async Task NullNestedObject_Typed_RoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"NullNested_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection<User>("user");
        await collection.InsertOneAsync(new User
        {
            Id = 1,
            Name = "NoWork",
            Age = 25,
            Location = null,
            Work = null
        });

        store.Dispose();

        var store2 = new DataStore(path);
        var collection2 = store2.GetCollection<User>("user");
        var user = collection2.AsQueryable().First();

        Assert.Equal("NoWork", user.Name);
        Assert.Null(user.Location);
        Assert.Null(user.Work);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task NullNestedObject_ThenUpdate_ToNonNull()
    {
        var path = UTHelpers.GetFullFilePath($"NullToNon_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection<User>("user");
        await collection.InsertOneAsync(new User
        {
            Id = 1,
            Name = "Test",
            Age = 30,
            Work = null
        });

        await collection.UpdateOneAsync(
            e => e.Id == 1,
            new { Work = new WorkPlace { Name = "NewCo", Location = "SF" } });

        store.Dispose();

        var store2 = new DataStore(path);
        var collection2 = store2.GetCollection<User>("user");
        var user = collection2.AsQueryable().First();

        Assert.NotNull(user.Work);
        Assert.Equal("NewCo", user.Work.Name);
        Assert.Equal("SF", user.Work.Location);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task EmptyCollection_RoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"EmptyCol_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        // Create collection by inserting then deleting
        var collection = store.GetCollection<Movie>("movie");
        await collection.InsertOneAsync(new Movie { Name = "Temp", Rating = 1.0 });
        await collection.DeleteOneAsync(e => e.Name == "Temp");

        store.Dispose();

        var store2 = new DataStore(path);
        var collection2 = store2.GetCollection<Movie>("movie");
        Assert.Equal(0, collection2.Count);

        // Verify it's detected as a collection (empty array in JSON)
        var keys = store2.GetKeys(ValueType.Collection);
        Assert.True(keys.ContainsKey("movie"));

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task EmptyStringField_RoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"EmptyStr_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection<User>("user");
        await collection.InsertOneAsync(new User
        {
            Id = 1,
            Name = "",
            Age = 0,
            Location = "",
            Work = new WorkPlace { Name = "", Location = "" }
        });

        store.Dispose();

        var store2 = new DataStore(path);
        var collection2 = store2.GetCollection<User>("user");
        var user = collection2.AsQueryable().First();

        Assert.Equal("", user.Name);
        Assert.Equal("", user.Location);
        Assert.Equal("", user.Work.Name);
        Assert.Equal(0, user.Age);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task NullListProperty_Typed_RoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"NullList_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection<Family>("family");
        await collection.InsertOneAsync(new Family
        {
            Id = 1,
            FamilyName = "NullLists",
            Parents = null,
            Children = null,
            Address = null,
            BankAccount = null
        });

        store.Dispose();

        var store2 = new DataStore(path);
        var collection2 = store2.GetCollection<Family>("family");
        var family = collection2.AsQueryable().First();

        Assert.Equal("NullLists", family.FamilyName);
        Assert.Null(family.Parents);
        Assert.Null(family.Children);
        Assert.Null(family.Address);
        Assert.Null(family.BankAccount);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task EmptyListProperty_Typed_RoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"EmptyList_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection<Family>("family");
        await collection.InsertOneAsync(new Family
        {
            Id = 1,
            FamilyName = "EmptyLists",
            Parents = new List<Parent>(),
            Children = new List<Child>()
        });

        store.Dispose();

        var store2 = new DataStore(path);
        var collection2 = store2.GetCollection<Family>("family");
        var family = collection2.AsQueryable().First();

        Assert.NotNull(family.Parents);
        Assert.Empty(family.Parents);
        Assert.NotNull(family.Children);
        Assert.Empty(family.Children);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task DeeplyNestedObject_RoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"DeepNest_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection<Family>("family");
        await collection.InsertOneAsync(new Family
        {
            Id = 1,
            FamilyName = "DeepFamily",
            Parents = new List<Parent>
            {
                new Parent
                {
                    Id = 1,
                    Name = "Parent1",
                    Gender = Gender.Female,
                    Age = 35,
                    Email = "p@test.com",
                    Phone = "123",
                    Work = new Work { CompanyName = "BigCorp", Address = "100 Main St" },
                    FavouriteMovie = "Matrix"
                }
            },
            Children = new List<Child>
            {
                new Child
                {
                    Name = "Kid1",
                    Gender = Gender.Male,
                    Age = 8,
                    Friends = new List<Friend>
                    {
                        new Friend { Name = "Buddy1", Age = 9 },
                        new Friend { Name = "Buddy2", Age = 7 }
                    }
                }
            },
            Address = new Address
            {
                Street = "456 Oak Ave",
                PostNumber = 12345,
                City = "Springfield",
                Age = 50,
                Country = new Country { Name = "USA", Code = 1 }
            },
            BankAccount = new BankAccount
            {
                Opened = new DateTime(2020, 1, 1),
                Balance = "5000.50",
                Active = true
            },
            Notes = "Some notes"
        });

        store.Dispose();

        // Verify entire deeply nested structure survives round-trip
        var store2 = new DataStore(path);
        var collection2 = store2.GetCollection<Family>("family");
        var family = collection2.AsQueryable().First();

        Assert.Equal("DeepFamily", family.FamilyName);
        Assert.Equal("Parent1", family.Parents[0].Name);
        Assert.Equal(Gender.Female, family.Parents[0].Gender);
        Assert.Equal("BigCorp", family.Parents[0].Work.CompanyName);
        Assert.Equal("Matrix", family.Parents[0].FavouriteMovie);
        Assert.Equal("Kid1", family.Children[0].Name);
        Assert.Equal(2, family.Children[0].Friends.Count);
        Assert.Equal("Buddy1", family.Children[0].Friends[0].Name);
        Assert.Equal(9, family.Children[0].Friends[0].Age);
        Assert.Equal("456 Oak Ave", family.Address.Street);
        Assert.Equal("USA", family.Address.Country.Name);
        Assert.Equal(1, family.Address.Country.Code);
        Assert.True(family.BankAccount.Active);
        Assert.Equal("5000.50", family.BankAccount.Balance);
        Assert.Equal("Some notes", family.Notes);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task DeeplyNestedObject_Dynamic_RoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"DeepDyn_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection("complex");
        await collection.InsertOneAsync(new
        {
            id = 1,
            level1 = new
            {
                level2 = new
                {
                    level3 = new
                    {
                        value = "deep"
                    }
                }
            }
        });

        store.Dispose();

        var store2 = new DataStore(path);
        var collection2 = store2.GetCollection("complex");
        var item = collection2.AsQueryable().First();

        Assert.Equal("deep", (string)item.level1.level2.level3.value);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task NestedArrays_Typed_RoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"NestedArr_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection<TestModelWithNestedArray>("items");
        await collection.InsertOneAsync(new TestModelWithNestedArray
        {
            Id = "1",
            Type = "matrix",
            NestedLists = new List<List<int>>
            {
                new List<int> { 1, 2, 3 },
                new List<int> { 4, 5, 6 },
                new List<int> { 7, 8, 9 }
            }
        });

        store.Dispose();

        var store2 = new DataStore(path);
        var collection2 = store2.GetCollection<TestModelWithNestedArray>("items");
        var item = collection2.AsQueryable().First();

        Assert.Equal(3, item.NestedLists.Count);
        Assert.Equal(new List<int> { 1, 2, 3 }, item.NestedLists[0]);
        Assert.Equal(new List<int> { 4, 5, 6 }, item.NestedLists[1]);
        Assert.Equal(new List<int> { 7, 8, 9 }, item.NestedLists[2]);

        store2.Dispose();
        UTHelpers.Down(path);
    }

}
