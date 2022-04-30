using System.Threading.Tasks;
using BattleNetPrefill;
using BenchmarkDotNet.Attributes;
using Spectre.Console.Testing;

namespace Benchmarks
{
    public partial class Program
    {
        public class Starcraft2Benchmark
        {
            [Benchmark]
            public async Task Current()
            {
                var debugConfig = new DebugConfig { UseCdnDebugMode = true };
                await TactProductHandler.ProcessProductAsync(TactProduct.Starcraft2, new TestConsole() {}, debugConfig);
            }
        }
    }
}
