namespace JsonFlatFileDataStore.Test;

/// <summary>
/// Tests for using Guid as the collection's id-field. Unlike integer and string id-fields,
/// Guid values are not incremented — a new value is generated when the caller has not set one,
/// and a value the caller has set is kept as is.
/// </summary>
public class GuidIdFieldTests
{
    public class GuidIdModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public Guid OtherGuid { get; set; }
    }

    public class NullableGuidIdModel
    {
        public Guid? Id { get; set; }

        public string Name { get; set; }
    }

    [Fact]
    public void InsertOne_Typed_KeepsCallerDefinedId()
    {
        var newFilePath = UTHelpers.Up();
        var store = new DataStore(newFilePath);

        var collection = store.GetCollection<GuidIdModel>("guidIds");

        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        collection.InsertOne(new GuidIdModel { Id = first, Name = "Jim" });
        collection.InsertOne(new GuidIdModel { Id = second, Name = "Barry" });

        var items = collection.AsQueryable().ToList();

        Assert.Equal(2, items.Count);
        Assert.Equal(first, items[0].Id);
        Assert.Equal(second, items[1].Id);

        UTHelpers.Down(newFilePath);
    }

    [Fact]
    public void InsertOne_Typed_GeneratesIdWhenEmpty()
    {
        var newFilePath = UTHelpers.Up();
        var store = new DataStore(newFilePath);

        var collection = store.GetCollection<GuidIdModel>("guidIds");

        collection.InsertOne(new GuidIdModel { Name = "Jim" });
        collection.InsertOne(new GuidIdModel { Name = "Barry" });

        var items = collection.AsQueryable().ToList();

        Assert.All(items, i => Assert.NotEqual(Guid.Empty, i.Id));
        Assert.NotEqual(items[0].Id, items[1].Id);

        UTHelpers.Down(newFilePath);
    }

    [Fact]
    public void InsertMany_Typed_GeneratesUniqueIds()
    {
        var newFilePath = UTHelpers.Up();
        var store = new DataStore(newFilePath);

        var collection = store.GetCollection<GuidIdModel>("guidIds");

        collection.InsertMany(new[]
        {
            new GuidIdModel { Name = "Jim" },
            new GuidIdModel { Name = "Barry" },
            new GuidIdModel { Name = "Sandels" }
        });

        var ids = collection.AsQueryable().Select(e => e.Id).ToList();

        Assert.Equal(3, ids.Distinct().Count());
        Assert.DoesNotContain(Guid.Empty, ids);

        UTHelpers.Down(newFilePath);
    }

    [Fact]
    public void GetNextIdValue_Typed_ReturnsNewGuid()
    {
        var newFilePath = UTHelpers.Up();
        var store = new DataStore(newFilePath);

        var collection = store.GetCollection<GuidIdModel>("guidIds");

        var emptyCollectionId = collection.GetNextIdValue();
        Assert.IsType<Guid>(emptyCollectionId);
        Assert.NotEqual(Guid.Empty, emptyCollectionId);

        collection.InsertOne(new GuidIdModel { Name = "Jim" });

        var nextId = collection.GetNextIdValue();
        Assert.IsType<Guid>(nextId);
        Assert.NotEqual(Guid.Empty, nextId);
        Assert.NotEqual(collection.AsQueryable().First().Id, (Guid)nextId);

        UTHelpers.Down(newFilePath);
    }

    [Fact]
    public void UpdateOne_ReplaceOne_DeleteOne_Typed_WithGuidId()
    {
        var newFilePath = UTHelpers.Up();
        var store = new DataStore(newFilePath);

        var collection = store.GetCollection<GuidIdModel>("guidIds");

        var itemId = Guid.NewGuid();
        var secondGuid = Guid.NewGuid();
        var thirdGuid = Guid.NewGuid();

        collection.InsertOne(new GuidIdModel { Id = itemId, Name = "Jim", OtherGuid = secondGuid });

        Assert.True(collection.ReplaceOne(e => e.Id == itemId, new GuidIdModel { Id = itemId, Name = "Barry", OtherGuid = secondGuid }));
        Assert.True(collection.UpdateOne(e => e.Id == itemId, new { Name = "Sandels" }));
        Assert.True(collection.UpdateOne(e => e.Id == itemId, new { OtherGuid = thirdGuid }));

        var updated = collection.AsQueryable().Single();

        Assert.Equal(itemId, updated.Id);
        Assert.Equal("Sandels", updated.Name);
        Assert.Equal(thirdGuid, updated.OtherGuid);

        Assert.True(collection.DeleteOne(e => e.Id == itemId));
        Assert.Empty(collection.AsQueryable());

        UTHelpers.Down(newFilePath);
    }

    [Fact]
    public void ReplaceOne_Typed_WithIdValue()
    {
        var newFilePath = UTHelpers.Up();
        var store = new DataStore(newFilePath);

        var collection = store.GetCollection<GuidIdModel>("guidIds");

        var itemId = Guid.NewGuid();
        collection.InsertOne(new GuidIdModel { Id = itemId, Name = "Jim" });

        Assert.True(collection.ReplaceOne(itemId, new GuidIdModel { Id = itemId, Name = "Barry" }));
        Assert.Equal("Barry", collection.AsQueryable().Single().Name);

        Assert.True(collection.DeleteOne(itemId));
        Assert.Empty(collection.AsQueryable());

        UTHelpers.Down(newFilePath);
    }

    [Fact]
    public async Task InsertOneAsync_Typed_PersistsGuidId()
    {
        var newFilePath = UTHelpers.Up();

        var store = new DataStore(newFilePath);
        var collection = store.GetCollection<GuidIdModel>("guidIds");

        await collection.InsertOneAsync(new GuidIdModel { Name = "Jim" });
        var inserted = collection.AsQueryable().Single();

        store.Dispose();

        var store2 = new DataStore(newFilePath);
        var reloaded = store2.GetCollection<GuidIdModel>("guidIds").AsQueryable().Single();

        Assert.Equal(inserted.Id, reloaded.Id);
        Assert.NotEqual(Guid.Empty, reloaded.Id);

        store2.Dispose();
        UTHelpers.Down(newFilePath);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InsertOne_Typed_BothCasingModes(bool useLowerCamelCase)
    {
        var newFilePath = UTHelpers.GetFullFilePath($"GuidCasing_{useLowerCamelCase}");
        var store = new DataStore(newFilePath, useLowerCamelCase);

        var collection = store.GetCollection<GuidIdModel>("guidIds");

        var itemId = Guid.NewGuid();
        collection.InsertOne(new GuidIdModel { Id = itemId, Name = "Jim" });
        collection.InsertOne(new GuidIdModel { Name = "Barry" });

        store.Dispose();

        var store2 = new DataStore(newFilePath, useLowerCamelCase);
        var items = store2.GetCollection<GuidIdModel>("guidIds").AsQueryable().ToList();

        Assert.Equal(itemId, items[0].Id);
        Assert.NotEqual(Guid.Empty, items[1].Id);

        store2.Dispose();
        UTHelpers.Down(newFilePath);
    }

    [Fact]
    public void InsertOne_Typed_NullableGuidId()
    {
        var newFilePath = UTHelpers.Up();
        var store = new DataStore(newFilePath);

        var collection = store.GetCollection<NullableGuidIdModel>("nullableGuidIds");

        var itemId = Guid.NewGuid();
        collection.InsertOne(new NullableGuidIdModel { Id = itemId, Name = "Jim" });
        collection.InsertOne(new NullableGuidIdModel { Name = "Barry" });

        var items = collection.AsQueryable().ToList();

        Assert.Equal(itemId, items[0].Id);
        Assert.NotNull(items[1].Id);
        Assert.NotEqual(Guid.Empty, items[1].Id.Value);

        UTHelpers.Down(newFilePath);
    }

    [Fact]
    public void InsertOne_Dynamic_GeneratesNewGuidWhenLastIdIsGuid()
    {
        var newFilePath = UTHelpers.Up();
        var store = new DataStore(newFilePath);

        var collection = store.GetCollection("dynamicGuidIds");

        var itemId = Guid.NewGuid();
        collection.InsertOne(new { id = itemId, name = "Jim" });
        collection.InsertOne(new { name = "Barry" });

        var items = collection.AsQueryable().ToList();

        Assert.Equal(itemId.ToString(), (string)items[0].id);

        var generated = (string)items[1].id;
        Assert.True(Guid.TryParse(generated, out var parsed));
        Assert.NotEqual(Guid.Empty, parsed);
        Assert.NotEqual(itemId, parsed);

        UTHelpers.Down(newFilePath);
    }

    [Fact]
    public void GetNextIdValue_Dynamic_StringIdIsStillIncremented()
    {
        var newFilePath = UTHelpers.Up();
        var store = new DataStore(newFilePath);

        var collection = store.GetCollection("dynamicStringIds");

        collection.InsertOne(new { id = "item1", name = "Jim" });

        Assert.Equal("item2", collection.GetNextIdValue());

        UTHelpers.Down(newFilePath);
    }

    [Fact]
    public void InsertOne_Typed_StringIdWithGuidValuesGeneratesNewGuid()
    {
        var newFilePath = UTHelpers.Up();
        var store = new DataStore(newFilePath);

        var collection = store.GetCollection<StringIdModel>("stringGuidIds");

        var itemId = Guid.NewGuid();
        collection.InsertOne(new StringIdModel { Id = itemId.ToString(), Name = "Jim" });
        collection.InsertOne(new StringIdModel { Name = "Barry" });

        var items = collection.AsQueryable().ToList();

        Assert.Equal(itemId.ToString(), items[0].Id);
        Assert.True(Guid.TryParse(items[1].Id, out var generated));
        Assert.NotEqual(Guid.Empty, generated);
        Assert.NotEqual(itemId, generated);

        UTHelpers.Down(newFilePath);
    }

    [Fact]
    public void InsertOne_Dynamic_KeepsCallerDefinedIdFormat()
    {
        var newFilePath = UTHelpers.Up();
        var store = new DataStore(newFilePath);

        var collection = store.GetCollection("dynamicGuidIds");

        collection.InsertOne(new { id = Guid.NewGuid().ToString(), name = "Jim" });

        var nonCanonicalId = "{AB6E9F13-4E33-4D2A-9C31-2B1E3C0A5F77}";

        dynamic item = new System.Dynamic.ExpandoObject();
        item.id = nonCanonicalId;
        item.name = "Barry";

        collection.InsertOne(item);

        var items = collection.AsQueryable().ToList();

        Assert.Equal(nonCanonicalId, (string)items[1].id);

        UTHelpers.Down(newFilePath);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void InsertOne_Dynamic_GeneratesNewGuidWhenCallerIdIsNotUsable(string callerId)
    {
        var newFilePath = UTHelpers.Up($"DynamicGuidFallback_{callerId.Length}");
        var store = new DataStore(newFilePath);

        var collection = store.GetCollection("dynamicGuidIds");

        collection.InsertOne(new { id = Guid.NewGuid().ToString(), name = "Jim" });

        dynamic item = new System.Dynamic.ExpandoObject();
        item.id = callerId;
        item.name = "Barry";

        collection.InsertOne(item);

        var inserted = (string)collection.AsQueryable().ToList()[1].id;

        Assert.True(Guid.TryParse(inserted, out var generated));
        Assert.NotEqual(Guid.Empty, generated);

        store.Dispose();
        UTHelpers.Down(newFilePath);
    }

    [Fact]
    public void InsertOne_Typed_StringIdKeepsCallerDefinedIdFormat()
    {
        var newFilePath = UTHelpers.Up();
        var store = new DataStore(newFilePath);

        var collection = store.GetCollection<StringIdModel>("stringGuidIds");

        collection.InsertOne(new StringIdModel { Id = Guid.NewGuid().ToString(), Name = "Jim" });

        var nonCanonicalId = "AB6E9F13-4E33-4D2A-9C31-2B1E3C0A5F77";
        collection.InsertOne(new StringIdModel { Id = nonCanonicalId, Name = "Barry" });

        Assert.Equal(nonCanonicalId, collection.AsQueryable().ToList()[1].Id);

        UTHelpers.Down(newFilePath);
    }

    public class StringIdModel
    {
        public string Id { get; set; }

        public string Name { get; set; }
    }
}
