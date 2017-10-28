using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
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
        public async Task InsertOneAsync()
        {
            await _collection.InsertOneAsync(new { Name = "Teddy" });
        }

        [Benchmark]
        public void InsertOne()
        {
            _collection.InsertOne(new { Name = "Teddy" });
        }
    }

    [SimpleJob(RunStrategy.Throughput, launchCount: 1, warmupCount: 5, targetCount: 50)]
    public class CopyPropertiesBenchmark

    {
        private CopyPropertiesTests _test;

        [GlobalSetup]
        public void GlobalSetup() => _test = new CopyPropertiesTests();

        [Benchmark]
        public void CopyProperties_TypedFamilyParents()
        {
            _test.CopyProperties_TypedFamilyParents();
        }

        [Benchmark]
        public void CopyProperties_DynamicWithInnerExpandos()
        {
            _test.CopyProperties_DynamicWithInnerExpandos();
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            var switcher = new BenchmarkSwitcher(new[] {
                typeof(TypedCollectionBenchmark),
                typeof(DynamicCollectionBenchmark),
                typeof(CopyPropertiesBenchmark)
            });

            switcher.Run(args);
        }
    }
}
