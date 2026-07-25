namespace JsonFlatFileDataStore.Test;

/// <summary>
/// Numeric edge cases that commonly differ between Newtonsoft and System.Text.Json:
/// decimal precision, long boundaries, integers beyond double precision, and special floats.
/// These tests pin current Newtonsoft behavior.
/// </summary>
public class NumericEdgeCaseTests
{
    public class DecimalModel
    {
        public int Id { get; set; }
        public decimal Price { get; set; }
        public decimal? OptionalAmount { get; set; }
    }

    [Fact]
    public async Task Decimal_RoundTrip_PreservesPrecision()
    {
        var path = UTHelpers.GetFullFilePath($"DecRT_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection<DecimalModel>("decModel");
        await collection.InsertOneAsync(new DecimalModel { Id = 1, Price = 19.95m, OptionalAmount = 0.0000001m });
        await collection.InsertOneAsync(new DecimalModel { Id = 2, Price = 9999999999.99m, OptionalAmount = null });
        await collection.InsertOneAsync(new DecimalModel { Id = 3, Price = -0.01m, OptionalAmount = 12345678.99m });

        store.Dispose();

        var store2 = new DataStore(path);
        var items = store2.GetCollection<DecimalModel>("decModel").AsQueryable().OrderBy(e => e.Id).ToList();

        Assert.Equal(19.95m, items[0].Price);
        Assert.Equal(0.0000001m, items[0].OptionalAmount);
        Assert.Equal(9999999999.99m, items[1].Price);
        Assert.Null(items[1].OptionalAmount);
        Assert.Equal(-0.01m, items[2].Price);
        Assert.Equal(12345678.99m, items[2].OptionalAmount);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Decimal_BeyondDoublePrecision_RoundTrip(bool useLowerCamelCase)
    {
        // Values with more significant digits than a double can hold. Both the read path and the
        // camelCase write path used to materialize them as double, which corrupted them.
        var path = UTHelpers.GetFullFilePath($"DecPrecision_{useLowerCamelCase}_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path, useLowerCamelCase);

        var collection = store.GetCollection<DecimalModel>("decModel");
        await collection.InsertOneAsync(new DecimalModel { Id = 1, Price = 123456789012345678.5m });
        await collection.InsertOneAsync(new DecimalModel { Id = 2, Price = 79228162514264337593543950m });
        await collection.InsertOneAsync(new DecimalModel { Id = 3, Price = decimal.MaxValue });

        store.Dispose();

        var store2 = new DataStore(path, useLowerCamelCase);
        var items = store2.GetCollection<DecimalModel>("decModel").AsQueryable().OrderBy(e => e.Id).ToList();

        Assert.Equal(123456789012345678.5m, items[0].Price);
        Assert.Equal(79228162514264337593543950m, items[1].Price);
        Assert.Equal(decimal.MaxValue, items[2].Price);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task Decimal_UpdatedAfterReload_KeepsPrecision()
    {
        // The reload path parses the file again, so precision must survive a write that happens
        // after the store has read its own output.
        var path = UTHelpers.GetFullFilePath($"DecUpdate_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection<DecimalModel>("decModel");
        await collection.InsertOneAsync(new DecimalModel { Id = 1, Price = 123456789012345678.5m });
        await collection.UpdateOneAsync(1, new { OptionalAmount = 12345678901234567890.5m });

        store.Dispose();

        var store2 = new DataStore(path);
        var item = store2.GetCollection<DecimalModel>("decModel").AsQueryable().First();

        Assert.Equal(123456789012345678.5m, item.Price);
        Assert.Equal(12345678901234567890.5m, item.OptionalAmount);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Theory]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    [InlineData(double.Epsilon)]
    [InlineData(1e-30)]
    public async Task Double_OutsideDecimalRange_RoundTrip(double value)
    {
        // Values outside decimal's range must not be read as one: too large throws, too small
        // rounds to zero without any error. Both fall back to double parsing.
        var path = UTHelpers.GetFullFilePath($"DoubleRange_{value}_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection<Movie>("movie");
        await collection.InsertOneAsync(new Movie { Name = "Extreme", Rating = value });

        store.Dispose();

        var store2 = new DataStore(path);
        var movie = store2.GetCollection<Movie>("movie").AsQueryable().First();

        Assert.Equal(value, movie.Rating);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task Decimal_DynamicAccess()
    {
        var path = UTHelpers.GetFullFilePath($"DecDyn_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection<DecimalModel>("decModel");
        await collection.InsertOneAsync(new DecimalModel { Id = 1, Price = 42.50m });

        store.Dispose();

        var store2 = new DataStore(path);
        var dyn = store2.GetCollection("decModel").AsQueryable().First();

        // Dynamic access to a JSON number that originated as decimal.
        // Newtonsoft surfaces it as either decimal or double depending on the literal form.
        var price = (decimal)dyn.price;
        Assert.Equal(42.50m, price);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public void Int64_MaxAndMin_RoundTrip_Typed()
    {
        var path = UTHelpers.GetFullFilePath($"Int64Typed_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        store.InsertItem("max", long.MaxValue);
        store.InsertItem("min", long.MinValue);

        store.Dispose();

        var store2 = new DataStore(path);
        Assert.Equal(long.MaxValue, store2.GetItem<long>("max"));
        Assert.Equal(long.MinValue, store2.GetItem<long>("min"));

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public void Int64_MaxAndMin_RoundTrip_Dynamic()
    {
        var path = UTHelpers.GetFullFilePath($"Int64Dyn_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        store.InsertItem("max", long.MaxValue);
        store.InsertItem("min", long.MinValue);

        store.Dispose();

        var store2 = new DataStore(path);
        var dynMax = store2.GetItem("max");
        var dynMin = store2.GetItem("min");

        Assert.Equal(long.MaxValue, (long)dynMax);
        Assert.Equal(long.MinValue, (long)dynMin);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task Int64_BeyondDoublePrecision_RoundTrip()
    {
        // 2^53 + 1 — first integer that loses precision when stored as double.
        const long beyondDoublePrecision = 9007199254740993L;

        var path = UTHelpers.GetFullFilePath($"Int64Beyond_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection("items");
        await collection.InsertOneAsync(new { id = 1, big = beyondDoublePrecision });

        store.Dispose();

        var store2 = new DataStore(path);
        var item = store2.GetCollection("items").AsQueryable().First();

        // Newtonsoft preserves Int64 in dynamic/ExpandoObject, so exact equality holds.
        long big = item.big;
        Assert.Equal(beyondDoublePrecision, big);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public void Double_NaN_RoundTrip_BehaviorPinned()
    {
        var path = UTHelpers.GetFullFilePath($"NaN_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        store.InsertItem("nan", double.NaN);
        store.Dispose();

        var store2 = new DataStore(path);
        var value = store2.GetItem<double>("nan");

        Assert.True(double.IsNaN(value));

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public void Double_Infinity_RoundTrip_BehaviorPinned()
    {
        var path = UTHelpers.GetFullFilePath($"Inf_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        store.InsertItem("posInf", double.PositiveInfinity);
        store.InsertItem("negInf", double.NegativeInfinity);
        store.Dispose();

        var store2 = new DataStore(path);
        var pos = store2.GetItem<double>("posInf");
        var neg = store2.GetItem<double>("negInf");

        Assert.True(double.IsPositiveInfinity(pos));
        Assert.True(double.IsNegativeInfinity(neg));

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public void Int32_MaxAndMin_RoundTrip()
    {
        var path = UTHelpers.GetFullFilePath($"Int32_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        store.InsertItem("maxInt", int.MaxValue);
        store.InsertItem("minInt", int.MinValue);
        store.InsertItem("zero", 0);
        store.Dispose();

        var store2 = new DataStore(path);
        Assert.Equal(int.MaxValue, store2.GetItem<int>("maxInt"));
        Assert.Equal(int.MinValue, store2.GetItem<int>("minInt"));
        Assert.Equal(0, store2.GetItem<int>("zero"));

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task Double_RoundTrip_PreservesPrecision()
    {
        var path = UTHelpers.GetFullFilePath($"DoubleRT_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection<Movie>("movie");
        await collection.InsertOneAsync(new Movie { Name = "Test Movie", Rating = 7.123456789 });
        await collection.InsertOneAsync(new Movie { Name = "Exact Five", Rating = 5.0 });
        await collection.InsertOneAsync(new Movie { Name = "Zero Point One", Rating = 0.1 });

        store.Dispose();

        var store2 = new DataStore(path);
        var collection2 = store2.GetCollection<Movie>("movie");
        var movies = collection2.AsQueryable().ToList();

        Assert.Equal(7.123456789, movies.First(m => m.Name == "Test Movie").Rating);
        Assert.Equal(5.0, movies.First(m => m.Name == "Exact Five").Rating);
        Assert.Equal(0.1, movies.First(m => m.Name == "Zero Point One").Rating);

        store2.Dispose();
        UTHelpers.Down(path);
    }

    [Fact]
    public async Task FloatArray_RoundTrip_PreservesPrecision()
    {
        var path = UTHelpers.GetFullFilePath($"FloatArr_{DateTime.UtcNow.Ticks}");
        var store = new DataStore(path);

        var collection = store.GetCollection<World>("world");
        await collection.InsertOneAsync(new World
        {
            Id = 1,
            Position = new float[] { 1.5f, 2.75f, -3.125f },
            CameraRotationX = 45.5f,
            CameraRotationY = -90.25f
        });

        store.Dispose();

        var store2 = new DataStore(path);
        var collection2 = store2.GetCollection<World>("world");
        var world = collection2.AsQueryable().First();

        Assert.Equal(1.5f, world.Position[0]);
        Assert.Equal(2.75f, world.Position[1]);
        Assert.Equal(-3.125f, world.Position[2]);
        Assert.Equal(45.5f, world.CameraRotationX);
        Assert.Equal(-90.25f, world.CameraRotationY);

        store2.Dispose();
        UTHelpers.Down(path);
    }
}
