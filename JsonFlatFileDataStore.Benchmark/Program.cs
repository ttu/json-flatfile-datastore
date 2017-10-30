using BenchmarkDotNet.Running;

namespace JsonFlatFileDataStore.Benchmark
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var switcher = new BenchmarkSwitcher(new[] {
                typeof(TypedCollectionBenchmark),
                typeof(DynamicCollectionBenchmark),
                typeof(ObjectExtensionsBenchmark)
            });

            switcher.Run(args);
        }
    }
}