using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using JsonFlatFileDataStore.Test;
using System.Linq;
using System.Threading.Tasks;

namespace JsonFlatFileDataStore.Benchmark
{
    [SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 5, targetCount: 50)]
    public class DynamicCollectionBenchmark
    {
        private string _newFilePath;
        private IDataStore _store;
        private IDocumentCollection<dynamic> _collection;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _newFilePath = UTHelpers.Up();
            _store = new DataStore(_newFilePath);
            _collection = _store.GetCollection("user");
        }

        [GlobalCleanup]
        public void GlobalCleanup() => UTHelpers.Down(_newFilePath);

        [Benchmark]
        public void AsQueryable_Single()
        {
            var item = _collection.AsQueryable().Single(e => e.id == 1);
        }

        [Benchmark]
        public async Task InsertOneAsync()
        {
            await _collection.InsertOneAsync(new { name = "Teddy" });
        }

        [Benchmark]
        public void InsertOne()
        {
            _collection.InsertOne(new { name = "Teddy" });
        }

        [Benchmark]
        public async Task InsertManyAsync()
        {
            var items = Enumerable.Range(0, 100).Select(e => new { id = e, name = $"Teddy_{e}" });
            await _collection.InsertManyAsync(items);
        }

        [Benchmark]
        public void InsertMany()
        {
            var items = Enumerable.Range(0, 100).Select(e => new { id = e, name = $"Teddy_{e}" });
            _collection.InsertMany(items);
        }

        [Benchmark]
        public async Task DeleteOneAsync_With_Id()
        {
            await _collection.DeleteOneAsync(1);
        }

        [Benchmark]
        public async Task DeleteOneAsync_With_Predicate()
        {
            await _collection.DeleteOneAsync(e => e.id == 1);
        }

        [Benchmark]
        public void DeleteMany()
        {
            _collection.DeleteMany(e => true);
        }

        [Benchmark]
        public async Task DeleteManyAsync()
        {
            await _collection.DeleteManyAsync(e => true);
        }

        [Benchmark]
        public void ReplaceOne_With_Predicate()
        {
            _collection.ReplaceOne(e => e.id == 1, new { id = 1, name = "Teddy" });
        }

        [Benchmark]
        public void ReplaceOne_With_Id()
        {
            _collection.ReplaceOne(1, new { id = 1, name = "Teddy" });
        }

        [Benchmark]
        public async Task ReplaceOneAsync_With_Predicate()
        {
            await _collection.ReplaceOneAsync(e => e.id == 1, new { id = 1, name = "Teddy" });
        }

        [Benchmark]
        public async Task ReplaceOneAsync_With_Id()
        {
            await _collection.ReplaceOneAsync(1, new { id = 1, name = "Teddy" });
        }

        [Benchmark]
        public void ReplaceMany()
        {
            _collection.ReplaceMany(e => true, new { id = 1, name = "Teddy" });
        }

        [Benchmark]
        public async Task ReplaceManyAsync()
        {
            await _collection.ReplaceManyAsync(e => true, new { id = 1, name = "Teddy" });
        }

        [Benchmark]
        public void UpdateOne()
        {
            _collection.UpdateOne(e => e.id == 1, new { name = "Teddy" });
        }

        [Benchmark]
        public async Task UpdateOneAsync()
        {
            await _collection.UpdateOneAsync(e => e.id == 1, new { name = "Teddy" });
        }

        [Benchmark]
        public void UpdateMany()
        {
            _collection.UpdateMany(e => true, new { name = "Teddy" });
        }

        [Benchmark]
        public async Task UpdateManyAsync()
        {
            await _collection.UpdateManyAsync(e => true, new { name = "Teddy" });
        }

        [Benchmark]
        public void Find_With_Predicate()
        {
            _collection.Find(e => e.id == 1);
        }

        [Benchmark]
        public void Find_With_Text()
        {
            _collection.Find("James");
        }

        [Benchmark]
        public void GetNextIdValue()
        {
            _collection.GetNextIdValue();
        }
    }
}