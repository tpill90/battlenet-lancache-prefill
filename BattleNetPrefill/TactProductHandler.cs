using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BattleNetPrefill.Handlers;
using BattleNetPrefill.Parsers;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Utils;
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
        /// <param name="forcePrefill">By default, a product will be skipped if it has already prefilled the latest product version.
        ///                             Setting this to true will force a prefill regardless of previous runs.</param>
        public async Task<ComparisonResult> ProcessProductAsync(bool skipDiskCache = false, bool forcePrefill = false)
        {
            var timer = Stopwatch.StartNew();
            AnsiConsole.MarkupLine($"Starting processing of : {Blue(_product.DisplayName)}");

            // Initializing classes, now that we have our CDN info loaded
            CdnRequestManager cdnRequestManager = new CdnRequestManager(Config.BattleNetPatchUri, _debugConfig.UseCdnDebugMode, skipDiskCache);
            var downloadFileHandler = new DownloadFileHandler(cdnRequestManager);
            var configFileHandler = new ConfigFileHandler(cdnRequestManager);
            var installFileHandler = new InstallFileHandler(cdnRequestManager);
            var archiveIndexHandler = new ArchiveIndexHandler(cdnRequestManager, _product);

            // Finding the latest version of the game
            VersionsEntry? targetVersion = null;
            await _ansiConsole.CreateSpectreStatusSpinner().StartAsync("Getting latest version info...", async ctx =>
            {
                await cdnRequestManager.InitializeAsync(_product);
                targetVersion = await configFileHandler.GetLatestVersionEntryAsync(_product);
            });

            // Skip prefilling if we've already prefilled the latest version 
            if (!forcePrefill && IsProductUpToDate(targetVersion.Value))
            {
                AnsiConsole.MarkupLine($"   {Green("Up to date! Skipping..")}");
                return null;
            }

            await _ansiConsole.CreateSpectreStatusSpinner().StartAsync("Start", async ctx =>
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
            var downloadSuccess = await cdnRequestManager.DownloadQueuedRequestsAsync(_ansiConsole);
            if (downloadSuccess)
            {
                SaveDownloadedProductVersion(cdnRequestManager, targetVersion.Value);
            }

            timer.Stop();
            AnsiConsole.MarkupLine($"{Blue(_product.DisplayName)} pre-loaded in {Yellow(timer.Elapsed.ToString(@"hh\:mm\:ss\.FFFF"))}\n\n");
            
            if (!_debugConfig.CompareAgainstRealRequests)
            {
                return null;
            }

            var comparisonUtil = new ComparisonUtil();
            var result = await comparisonUtil.CompareAgainstRealRequestsAsync(cdnRequestManager.allRequestsMade.ToList(), _product);
            result.ElapsedTime = timer.Elapsed;

            return result;
        }

        /// <summary>
        /// Checks to see if the previously prefilled version is up to date with the latest version on the CDN
        /// </summary>
        private bool IsProductUpToDate(VersionsEntry latestVersion)
        {
            // Checking to see if a file has been previously prefilled
            var versionFilePath = $"{Config.CacheDir}/prefilledVersion-{_product.ProductCode}.txt";
            if (!File.Exists(versionFilePath))
            {
                return false;
            }

            // Checking to see if the game version previously prefilled is up to date with the latest version on the CDN.
            var lastPrefilledVersion = File.ReadAllText(versionFilePath);
            return latestVersion.versionsName == lastPrefilledVersion;
        }

        private void SaveDownloadedProductVersion(CdnRequestManager cdn, VersionsEntry latestVersion)
        {
            var versionFilePath = $"{Config.CacheDir}/prefilledVersion-{_product.ProductCode}.txt";
            File.WriteAllText(versionFilePath, latestVersion.versionsName);
        }
    }
}
