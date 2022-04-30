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
            private readonly CDN _cdn;
            private readonly TactProduct _targetProduct = TactProduct.Starcraft2;
            private readonly CDNConfigFile _cdnConfig;

            public ArchiveIndexHandlerBenchmark()
            {
                _cdn = new CDN(Config.BattleNetPatchUri, useDebugMode: true);
                _cdn.LoadCdnsFileAsync(_targetProduct).Wait();

                var configFileHandler = new ConfigFileHandler(_cdn);
                var targetVersion = configFileHandler.GetLatestVersionEntryAsync(_targetProduct).Result;
                _cdnConfig = configFileHandler.GetCdnConfigAsync(targetVersion).Result;
            }

            [Benchmark]
            public async Task NoPreallocation()
            {
                var archiveIndexHandler = new ArchiveIndexHandler(_cdn, _targetProduct);
                await archiveIndexHandler.BuildArchiveIndexesAsync(_cdnConfig);
            }
        }
    }
}
