namespace JsonFlatFileDataStore.Test;

/// <summary>
/// Round-trip tests for temporal types (DateTimeOffset, TimeSpan, DateTime.Kind)
/// and identifier types (Guid). These have known representational differences
/// between Newtonsoft and System.Text.Json — pin current Newtonsoft behavior.
/// </summary>
public class TemporalAndIdentifierTests
{
    public class TemporalModel
    {
        public int Id { get; set; }
        public DateTimeOffset Offset { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime Stamp { get; set; }
    }

    public class GuidModel
    {
        public int Id { get; set; }
        public Guid Token { get; set; }
        public string Name { get; set; }
    }

    [Fact]
    public async Task DateTimeOffset_RoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"DTO_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var original = new DateTimeOffset(2024, 6, 15, 14, 30, 45, TimeSpan.FromHours(3));

        var collection = store.GetCollection<TemporalModel>("temporal");
        await collection.InsertOneAsync(new TemporalModel
        {
            Id = 1,
            Offset = original,
            Duration = TimeSpan.Zero,
            Stamp = DateTime.UtcNow
        });

        store.Dispose();

        var store2 = new DataStore(path);
        var item = store2.GetCollection<TemporalModel>("temporal").AsQueryable().First();

        Assert.Equal(original, item.Offset);
        Assert.Equal(original.UtcDateTime, item.Offset.UtcDateTime);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task TimeSpan_RoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"TS_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var duration = TimeSpan.FromMinutes(90).Add(TimeSpan.FromMilliseconds(123));

        var collection = store.GetCollection<TemporalModel>("temporal");
        await collection.InsertOneAsync(new TemporalModel
        {
            Id = 1,
            Offset = DateTimeOffset.UtcNow,
            Duration = duration,
            Stamp = DateTime.UtcNow
        });

        store.Dispose();

        var store2 = new DataStore(path);
        var item = store2.GetCollection<TemporalModel>("temporal").AsQueryable().First();

        Assert.Equal(duration, item.Duration);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task Guid_Typed_RoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"GuidT_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var token = Guid.NewGuid();

        var collection = store.GetCollection<GuidModel>("guidModel");
        await collection.InsertOneAsync(new GuidModel { Id = 1, Token = token, Name = "G" });

        store.Dispose();

        var store2 = new DataStore(path);
        var item = store2.GetCollection<GuidModel>("guidModel").AsQueryable().First();

        Assert.Equal(token, item.Token);
        Assert.Equal("G", item.Name);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public void Guid_AsIdField_NotSupported_BehaviorPinned()
    {
        // Documented limitation: a typed model with a Guid as the key property fails on insert
        // because the library's dynamic-dispatch path through ObjectExtensions.AddDataToField
        // cannot set a Guid value via reflection in this code path.
        //
        // Migration note: STJ may or may not surface this same failure. If it does not,
        // update this test to assert success.
        var path = UTHelpers.GetFullFilePath($"GuidIdLim_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection<GuidIdModel>("guidIdModel");

        Assert.Throws<ArgumentException>(() =>
            collection.InsertOne(new GuidIdModel { Id = Guid.NewGuid(), Name = "X" }));

        store.Dispose();
        UTHelpers.Down(path);
    }

    public class GuidIdModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    [Fact]
    public void Guid_SingleItem_TypedRoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"GuidItem_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var guid = Guid.NewGuid();
        store.InsertItem("token", guid);

        store.Dispose();

        var store2 = new DataStore(path);
        Assert.Equal(guid, store2.GetItem<Guid>("token"));

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public void DateTime_UtcKind_PreservedAfterRoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"DTUtc_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var utc = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        store.InsertItem("when", utc);

        store.Dispose();

        var store2 = new DataStore(path);
        var read = store2.GetItem<DateTime>("when");

        Assert.Equal(DateTimeKind.Utc, read.Kind);
        Assert.Equal(utc, read);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task DateTime_InTypedModel_RoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"DateTimeModel_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var testDate = new DateTime(2023, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        var collection = store.GetCollection<Family>("family");
        await collection.InsertOneAsync(new Family
        {
            Id = 1,
            FamilyName = "TestFamily",
            Parents = new List<Parent>(),
            Children = new List<Child>(),
            BankAccount = new BankAccount
            {
                Opened = testDate,
                Balance = "1000.00",
                Active = true
            }
        });

        store.Dispose();

        var store2 = new DataStore(path);
        var collection2 = store2.GetCollection<Family>("family");
        var family = collection2.AsQueryable().First();

        Assert.Equal(testDate.Year, family.BankAccount.Opened.Year);
        Assert.Equal(testDate.Month, family.BankAccount.Opened.Month);
        Assert.Equal(testDate.Day, family.BankAccount.Opened.Day);
        Assert.True(family.BankAccount.Active);
        Assert.Equal("1000.00", family.BankAccount.Balance);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public void DateTime_LocalKind_PreservedAfterRoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"DTLocal_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var local = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Local);
        store.InsertItem("when", local);

        store.Dispose();

        var store2 = new DataStore(path);
        var read = store2.GetItem<DateTime>("when");

        // Newtonsoft default DateTimeZoneHandling = RoundtripKind preserves Local.
        Assert.Equal(DateTimeKind.Local, read.Kind);
        Assert.Equal(local, read);

        store2.Dispose();
        UTHelpers.Down(path);
    }
}
