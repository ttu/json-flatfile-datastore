using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using JsonFlatFileDataStore.Test;

namespace JsonFlatFileDataStore.Benchmark
{
    [SimpleJob(RunStrategy.Throughput, launchCount: 1, warmupCount: 5, targetCount: 50)]
    public class ObjectExtensionsBenchmark
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
}