using System.Threading.Tasks;
using BattleNetPrefill;
using BattleNetPrefill.Handlers;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Web;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    /// <summary>
    /// Benchmarks how long it takes to build the archive indexes.
    /// </summary>
    public partial class Program
    {
        [MemoryDiagnoser]
        public class ArchiveIndexHandlerBenchmark
        {
            private readonly CdnRequestManager _cdnRequestManager;
            private readonly CDNConfigFile _cdnConfig;

            private readonly TactProduct _targetProduct = TactProduct.WorldOfWarcraft;

            public ArchiveIndexHandlerBenchmark()
            {
                // Initialize the CdnRequestManager once.  We don't want to benchmark this initialization
                _cdnRequestManager = new CdnRequestManager(AppConfig.BattleNetPatchUri, useDebugMode: true);
                _cdnRequestManager.InitializeAsync(_targetProduct).Wait();

                // Load the latest version info once, don't want to repeatedly run this code either
                var configFileHandler = new ConfigFileHandler(_cdnRequestManager);
                var targetVersion = configFileHandler.GetLatestVersionEntryAsync(_targetProduct).Result;
                _cdnConfig = configFileHandler.GetCdnConfigAsync(targetVersion).Result;
            }

            //[Benchmark(Baseline = true)]
            //public async Task BuildArchiveIndexes_Original()
            //{
            //    Config.UseNewMethod = false;

            //    var archiveIndexHandler = new ArchiveIndexHandler_Original(_cdnRequestManager, _targetProduct);
            //    await archiveIndexHandler.BuildArchiveIndexesAsync(_cdnConfig);
            //}

            [Benchmark]
            public async Task BuildArchiveIndexes_New()
            {
                var archiveIndexHandler = new ArchiveIndexHandler(_cdnRequestManager, _targetProduct);
                await archiveIndexHandler.BuildArchiveIndexesAsync(_cdnConfig);
            }
        }
    }
}
