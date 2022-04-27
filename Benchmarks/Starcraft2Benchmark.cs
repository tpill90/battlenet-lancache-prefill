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
                await TactProductHandler.ProcessProductAsync(TactProduct.Starcraft2, new TestConsole() { EmitAnsiSequences = false}, true, false, false);
            }

        }
    }
}
