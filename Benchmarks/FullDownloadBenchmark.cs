using System.Threading.Tasks;
using BattleNetPrefill;
using BenchmarkDotNet.Attributes;
using Spectre.Console.Testing;

namespace Benchmarks
{
    public partial class Program
    {
        [MemoryDiagnoser]
        public class FullDownloadBenchmark
        {
            public TactProduct targetProduct = TactProduct.WorldOfWarcraft;

            [Benchmark(Baseline = true)]
            public async Task Current()
            {
                var debugConfig = new DebugConfig { UseCdnDebugMode = true };
                var tactProductHandler = new TactProductHandler(targetProduct, new TestConsole(), debugConfig);

                await tactProductHandler.ProcessProductAsync(forcePrefill: true);
            }

            [Benchmark]
            public async Task NewCode()
            {
                var debugConfig = new DebugConfig { UseCdnDebugMode = true };
                var tactProductHandler = new TactProductHandler(targetProduct, new TestConsole(), debugConfig);

                await tactProductHandler.ProcessProductAsync(forcePrefill: true);
            }
        }
    }
}
