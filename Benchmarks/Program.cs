using Benchmarks.Benchmarks;

namespace Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<MD5ToString>();
        }
    }
}
