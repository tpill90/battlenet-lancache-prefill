using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BattleNetPrefill.Handlers;
using BattleNetPrefill.Parsers;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Utils.Debug;
using BattleNetPrefill.Utils.Debug.Models;
using BattleNetPrefill.Web;
using Spectre.Console;
using static BattleNetPrefill.Utils.SpectreColors;

namespace BattleNetPrefill
{
    public class TactProductHandler
    {
        private readonly TactProduct _product;
        private readonly IAnsiConsole _ansiConsole;
        private readonly DebugConfig _debugConfig;

        /// <summary>
        /// Creates a new TactProductHandler for the specified product.
        /// </summary>
        /// <param name="product">The targeted game that should be downloaded</param>
        /// <param name="ansiConsole"></param>
        /// <param name="debugConfig"></param>
        public TactProductHandler(TactProduct product, IAnsiConsole ansiConsole, DebugConfig debugConfig)
        {
            _product = product;
            _ansiConsole = ansiConsole;
            _debugConfig = debugConfig;
        }

        /// <summary>
        /// Downloads a specified game, in the same manner that Battle.net does.  Should be used to pre-fill a LanCache with game data from Blizzard's CDN.
        /// </summary>
        /// <param name="skipDiskCache">If set to true, then no cache files will be written to disk.  Every run will re-request the required files</param>
        /// <returns></returns>
        public async Task<ComparisonResult> ProcessProductAsync(bool skipDiskCache = false)
        {
            var spectreStatus = _ansiConsole.Status()
                                            .AutoRefresh(true)
                                            .SpinnerStyle(Style.Parse("green"))
                                            .Spinner(Spinner.Known.Dots2);

            var timer = Stopwatch.StartNew();
            AnsiConsole.MarkupLine($"Now starting processing of : {Blue(_product.DisplayName)}");

            // Initializing classes, now that we have our CDN info loaded
            CdnRequestManager cdnRequestManager = new CdnRequestManager(Config.BattleNetPatchUri, _debugConfig.UseCdnDebugMode, skipDiskCache);
            var downloadFileHandler = new DownloadFileHandler(cdnRequestManager);
            var configFileHandler = new ConfigFileHandler(cdnRequestManager);
            var installFileHandler = new InstallFileHandler(cdnRequestManager);
            var archiveIndexHandler = new ArchiveIndexHandler(cdnRequestManager, _product);

            // Finding the latest version of the game
            VersionsEntry? targetVersion = null;
            await spectreStatus.StartAsync("Getting latest version info...", async ctx =>
            {
                await cdnRequestManager.InitializeAsync(_product);
                targetVersion = await configFileHandler.GetLatestVersionEntryAsync(_product);
            });

            // Skip prefilling if we've already prefilled the latest version 
            if (!_debugConfig.UseCdnDebugMode && IsProductUpToDate(_product, targetVersion.Value))
            {
                AnsiConsole.MarkupLine($"{Green(_product.DisplayName)} already up to date!  Skipping..");
                return null;
            }

            await spectreStatus.StartAsync("Start", async ctx =>
            {
                // Getting other configuration files for this version, that detail where we can download the required files from.
                ctx.Status("Getting latest config files...");
                BuildConfigFile buildConfig = await BuildConfigParser.GetBuildConfigAsync(targetVersion.Value, cdnRequestManager, _product);
                CDNConfigFile cdnConfig = await configFileHandler.GetCdnConfigAsync(targetVersion.Value);

                ctx.Status("Building Archive Indexes...");
                await archiveIndexHandler.BuildArchiveIndexesAsync(cdnConfig);
                await downloadFileHandler.ParseDownloadFileAsync(buildConfig);

                // Start processing to determine which files need to be downloaded
                ctx.Status("Determining files to download...");
                await installFileHandler.HandleInstallFileAsync(buildConfig, archiveIndexHandler, cdnConfig, _product);
                await downloadFileHandler.HandleDownloadFileAsync(archiveIndexHandler, cdnConfig, _product);

                var patchLoader = new PatchLoader(cdnRequestManager, cdnConfig);
                await patchLoader.HandlePatchesAsync(buildConfig, _product);
            });

            // Actually start the download of any deferred requests
            await cdnRequestManager.DownloadQueuedRequestsAsync(_ansiConsole);

            timer.Stop();
            AnsiConsole.MarkupLine($"{Blue(_product.DisplayName)} pre-loaded in {Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}\n\n");
            
            SaveDownloadedProductVersion(_product, cdnRequestManager, targetVersion.Value);

            if (!_debugConfig.CompareAgainstRealRequests)
            {
                return null;
            }

            var comparisonUtil = new ComparisonUtil();
            var result = await comparisonUtil.CompareAgainstRealRequestsAsync(cdnRequestManager.allRequestsMade.ToList(), _product);
            result.ElapsedTime = timer.Elapsed;

            return result;
        }

        public static void SaveDownloadedProductVersion(TactProduct product, CdnRequestManager cdn, VersionsEntry latestVersion, bool force = false)
        {
            if (cdn.ErrorCount != 0)
            {
                return;
            }

            var versionFile = $"{Config.CacheDir}/prefilledVersion-{product.ProductCode}.txt";
            File.WriteAllText(versionFile, latestVersion.versionsName);
        }

        private static bool IsProductUpToDate(TactProduct product, VersionsEntry latestVersion)
        {
            var versionFile = $"{Config.CacheDir}/prefilledVersion-{product.ProductCode}.txt";
            if (!File.Exists(versionFile))
            {
                return false;
            }

            var currentLogVersion = File.ReadAllText(versionFile);
            return latestVersion.versionsName == currentLogVersion;
        }
    }
}
