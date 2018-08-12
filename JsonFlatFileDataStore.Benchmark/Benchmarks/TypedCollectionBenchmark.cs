using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using JsonFlatFileDataStore.Test;
using System.Threading.Tasks;

namespace JsonFlatFileDataStore.Benchmark
{
    [SimpleJob(RunStrategy.Monitoring, launchCount: 1, warmupCount: 5, targetCount: 50)]
    public class TypedCollectionBenchmark
    {
        private string _newFilePath;
        private IDataStore _store;
        private IDocumentCollection<User> _collection;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _newFilePath = UTHelpers.Up();
            _store = new DataStore(_newFilePath);
            _collection = _store.GetCollection<User>("user");
        }

        [GlobalCleanup]
        public void GlobalCleanup() => UTHelpers.Down(_newFilePath);

        [Benchmark]
        public async Task InsertOneAsync()
        {
            await _collection.InsertOneAsync(new User { Name = "Teddy" });
        }

        [Benchmark]
        public void InsertOne()
        {
            _collection.InsertOne(new User { Name = "Teddy" });
        }
    }
}