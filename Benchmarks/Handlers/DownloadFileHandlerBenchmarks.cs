using System.Threading.Tasks;
using BattleNetPrefill;
using BattleNetPrefill.Handlers;
using BattleNetPrefill.Parsers;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Web;
using BenchmarkDotNet.Attributes;
using Spectre.Console.Testing;

namespace Benchmarks.Handlers
{
    [MemoryDiagnoser]
    public class DownloadFileHandlerBenchmarks
    {
        private readonly CdnRequestManager _cdnRequestManager;
        private readonly BuildConfigFile _buildConfig;

        private readonly TactProduct _targetProduct = TactProduct.WorldOfWarcraft;

        public DownloadFileHandlerBenchmarks()
        {
            // Initialize the CdnRequestManager once.  We don't want to benchmark this initialization
            _cdnRequestManager = new CdnRequestManager(AppConfig.BattleNetPatchUri, new TestConsole(), useDebugMode: true);
            _cdnRequestManager.InitializeAsync(_targetProduct).Wait();

            // Load the latest version info once, don't want to repeatedly run this code either
            ConfigFileHandler configFileHandler = new ConfigFileHandler(_cdnRequestManager);
            VersionsEntry targetVersion = configFileHandler.GetLatestVersionEntryAsync(_targetProduct).Result;

            _buildConfig = BuildConfigParser.GetBuildConfigAsync(targetVersion, _cdnRequestManager, _targetProduct).Result;
        }

        //[Benchmark(Baseline = true)]
        //public async Task ParseDownloadFile_Original()
        //{
        //    var downloadFileHandler = new DownloadFileHandler_Original(_cdnRequestManager);
        //    await downloadFileHandler.ParseDownloadFileAsync(_buildConfig);
        //}

        [Benchmark]
        public async Task ParseDownloadFile_New()
        {
            var downloadFileHandler = new DownloadFileHandler(_cdnRequestManager);
            await downloadFileHandler.ParseDownloadFileAsync(_buildConfig);
        }
    }
}
