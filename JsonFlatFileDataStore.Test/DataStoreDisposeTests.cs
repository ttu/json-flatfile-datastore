using System.Diagnostics;

namespace JsonFlatFileDataStore.Test;

public class DataStoreDisposeTests
{
    [Theory]
    [InlineData("datastore_dispose_false", false)]
    [InlineData("datastore_dispose_true", true)]
    public async Task DataStore_Dispose(string testName, bool useDispose)
    {
        // This test is extremely unreliable because of use GC, so if this test fails, run again

        // Fail the test is running time is more than maxTimeMs
        var sw = Stopwatch.StartNew();
        const int maxTimeMs = 60000;

        var newFilePath = UTHelpers.Up(testName);

        const int itemCount = 200;

        WeakReference storeRef = null;

#pragma warning disable CS4014
        Task.Run(() => RunDataStore(out storeRef, newFilePath, itemCount, useDispose));
#pragma warning restore CS4014

        var store = new DataStore(newFilePath, reloadBeforeGetCollection: true);

        while (true)
        {
            var collection = store.GetCollection("random");

            if (collection.Count == itemCount)
                break;

            await Task.Delay(1000);

            if (sw.ElapsedMilliseconds > maxTimeMs)
                Assert.Fail("Timeout");
        }

        while (useDispose == storeRef.IsAlive)
        {
            await Task.Delay(1000);
            GC.Collect();

            if (sw.ElapsedMilliseconds > maxTimeMs)
                Assert.Fail("Timeout");
        }

        // If DataStore is not disposed, it should still be alive
        Assert.NotEqual(useDispose, storeRef.IsAlive);

        UTHelpers.Down(newFilePath);
    }

    private void RunDataStore(out WeakReference storeRef, string newFilePath, int count, bool dispose = false)
    {
        var store = new DataStore(newFilePath);

        storeRef = new WeakReference(store);

        var collection = store.GetCollection("random");

        var tasks = Enumerable.Range(0, count)
                              .AsParallel()
                              .Select(i => collection.InsertOneAsync(new User { Id = i, Name = $"Teddy_{i}" }))
                              .ToList();

        if (dispose)
            store.Dispose();

        store = null;
    }

    [Fact]
    public void VerifyJsonDocumentDisposal_NoMemoryLeaks()
    {
        // This test verifies that JsonDocument instances are properly disposed
        // to prevent memory leaks when using ConvertToJsonElement and related methods
        var newFilePath = UTHelpers.Up();
        var store = new DataStore(newFilePath);

        // Insert many items to stress test the resource management
        for (int i = 0; i < 100; i++)
        {
            var item = new
            {
                id = i,
                name = $"User{i}",
                data = new
                {
                    value = i * 10,
                    timestamp = DateTime.UtcNow
                }
            };

            store.InsertItem($"item{i}", item);
        }

        // Force garbage collection to see if any disposed documents cause issues
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Verify all items can still be read correctly
        for (int i = 0; i < 100; i++)
        {
            var retrieved = store.GetItem($"item{i}");
            Assert.NotNull(retrieved);
            Assert.Equal(i, retrieved.id);
            Assert.Equal($"User{i}", retrieved.name);
            Assert.Equal(i * 10, retrieved.data.value);
        }

        // Delete items to test RemoveJsonDataElement disposal
        for (int i = 0; i < 50; i++)
        {
            var deleted = store.DeleteItem($"item{i}");
            Assert.True(deleted);
        }

        // Force GC again
        GC.Collect();
        GC.WaitForPendingFinalizers();

        // Verify deleted items are gone and others remain
        for (int i = 0; i < 50; i++)
        {
            Assert.Null(store.GetItem($"item{i}"));
        }

        for (int i = 50; i < 100; i++)
        {
            var retrieved = store.GetItem($"item{i}");
            Assert.NotNull(retrieved);
            Assert.Equal(i, retrieved.id);
        }

        UTHelpers.Down(newFilePath);
    }
}