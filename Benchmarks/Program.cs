using BenchmarkDotNet.Running;

namespace Benchmarks
{
    public partial class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<BuildConfigParserBenchmarks>();
        }
    }
}
