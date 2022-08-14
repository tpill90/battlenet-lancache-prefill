using System.Threading.Tasks;
using BattleNetPrefill;
using BattleNetPrefill.Handlers;
using BattleNetPrefill.Parsers;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Web;
using BenchmarkDotNet.Attributes;
using Spectre.Console;

namespace Benchmarks
{
    public partial class Program
    {
        [MemoryDiagnoser]
        public class BuildConfigParserBenchmarks
        {
            private readonly CdnRequestManager _cdnRequestManager;
            private readonly VersionsEntry _targetVersion;

            private readonly TactProduct _targetProduct = TactProduct.WorldOfWarcraft;

            public BuildConfigParserBenchmarks()
            {
                // Initialize the CdnRequestManager once.  We don't want to benchmark this initialization
                _cdnRequestManager = new CdnRequestManager(AppConfig.BattleNetPatchUri, AnsiConsole.Console, useDebugMode: true);
                _cdnRequestManager.InitializeAsync(_targetProduct).Wait();

                // Load the latest version info once, don't want to repeatedly run this code either
                var configFileHandler = new ConfigFileHandler(_cdnRequestManager);
                _targetVersion = configFileHandler.GetLatestVersionEntryAsync(_targetProduct).Result;
            }

            [Benchmark(Baseline = true)]
            public async Task<BuildConfigFile> GetBuildConfig()
            {
                return await BuildConfigParser.GetBuildConfigAsync(_targetVersion, _cdnRequestManager, _targetProduct);
            }
        }
    }
}
