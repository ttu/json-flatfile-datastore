using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace JsonFlatFileDataStore.Test
{
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

            Task.Run(() => RunDataStore(out storeRef, newFilePath, itemCount, useDispose));

            var store = new DataStore(newFilePath, reloadBeforeGetCollection: true);

            while (true)
            {
                var collection = store.GetCollection("random");

                if (collection.Count == itemCount)
                    break;

                await Task.Delay(1000);

                if (sw.ElapsedMilliseconds > maxTimeMs)
                    Assert.False(true, "Timeout");
            }

            while (useDispose == storeRef.IsAlive)
            {
                await Task.Delay(1000);
                GC.Collect();

                if (sw.ElapsedMilliseconds > maxTimeMs)
                    Assert.False(true, "Timeout");
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
    }
}
