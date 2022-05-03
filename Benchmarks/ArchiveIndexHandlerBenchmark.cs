using System.Threading.Tasks;
using BattleNetPrefill;
using BattleNetPrefill.Handlers;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Web;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public partial class Program
    {
        public class ArchiveIndexHandlerBenchmark
        {
            private readonly CdnRequestManager _cdnRequestManager;
            private readonly TactProduct _targetProduct = TactProduct.Starcraft2;
            private readonly CDNConfigFile _cdnConfig;

            public ArchiveIndexHandlerBenchmark()
            {
                _cdnRequestManager = new CdnRequestManager(Config.BattleNetPatchUri, useDebugMode: true);
                _cdnRequestManager.InitializeAsync(_targetProduct).Wait();

                var configFileHandler = new ConfigFileHandler(_cdnRequestManager);
                var targetVersion = configFileHandler.GetLatestVersionEntryAsync(_targetProduct).Result;
                _cdnConfig = configFileHandler.GetCdnConfigAsync(targetVersion).Result;
            }

            [Benchmark]
            public async Task NoPreallocation()
            {
                var archiveIndexHandler = new ArchiveIndexHandler(_cdnRequestManager, _targetProduct);
                await archiveIndexHandler.BuildArchiveIndexesAsync(_cdnConfig);
            }
        }
    }
}
