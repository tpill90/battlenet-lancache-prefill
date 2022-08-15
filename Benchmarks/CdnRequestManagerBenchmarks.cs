using BattleNetPrefill;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Structs.Enums;
using BattleNetPrefill.Web;
using BenchmarkDotNet.Attributes;
using Spectre.Console.Testing;

namespace Benchmarks
{
    public partial class Program
    {
        [MemoryDiagnoser]
        public class CdnRequestManagerBenchmarks
        {
            private int iterations = 2000000;
            public CdnRequestManagerBenchmarks()
            {
            }

            [Benchmark(Baseline = true)]
            public void QueueRequests_List()
            {
                var cdnRequestManager = new CdnRequestManager(AppConfig.BattleNetPatchUri, new TestConsole(), useDebugMode: true);

                for (int i = 0; i < iterations; i++)
                {
                    cdnRequestManager.QueueRequest(RootFolder.data, new MD5Hash(5, 5), 44, 55, false);
                }
            }
        }
    }
}
