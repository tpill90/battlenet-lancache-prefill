using System.Diagnostics;
using System.Linq;
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
    public static class TactProductHandler
    {
        /// <summary>
        /// Downloads a specified game, in the same manner that Battle.net does.  
        /// </summary>
        /// <param name="product">The targeted game that should be downloaded</param>
        /// <param name="ansiConsole"></param>
        /// <param name="debugConfig"></param>
        /// <param name="skipDiskCache">If set to true, then no cache files will be written to disk.  Every run will re-request the required files</param>
        /// <returns></returns>
        public static async Task<ComparisonResult> ProcessProductAsync(TactProduct product, IAnsiConsole ansiConsole, DebugConfig debugConfig, bool skipDiskCache = false)
        {
            var timer = Stopwatch.StartNew();
            AnsiConsole.MarkupLine($"Now starting processing of : {Blue(product.DisplayName)}");

            // Initializing classes, now that we have our CDN info loaded
            CdnRequestManager cdnRequestManager = new CdnRequestManager(Config.BattleNetPatchUri, debugConfig.UseCdnDebugMode, skipDiskCache);
            var downloadFileHandler = new DownloadFileHandler(cdnRequestManager);
            var configFileHandler = new ConfigFileHandler(cdnRequestManager);
            var installFileHandler = new InstallFileHandler(cdnRequestManager);
            var archiveIndexHandler = new ArchiveIndexHandler(cdnRequestManager, product);

            var spectreStatus = ansiConsole.Status()
                       .AutoRefresh(true)
                       .SpinnerStyle(Style.Parse("green"))
                       .Spinner(Spinner.Known.Dots2);

            //TODO not a fan of this startAsync pushing everything over 1 tab
            await spectreStatus.StartAsync("Start", async ctx =>
            {
                // Finding the latest version of the game
                ctx.Status("Getting latest version info...");
                await cdnRequestManager.InitializeAsync(product);
                var targetVersion = await configFileHandler.GetLatestVersionEntryAsync(product);
                configFileHandler.QueueKeyRingFile(targetVersion);

                // Getting other configuration files for this version, that detail where we can download the required files from.
                ctx.Status("Getting latest config files...");
                BuildConfigFile buildConfig = await BuildConfigParser.GetBuildConfigAsync(targetVersion, cdnRequestManager, product);
                CDNConfigFile cdnConfig = await configFileHandler.GetCdnConfigAsync(targetVersion);

                ctx.Status("Building Archive Indexes...");
                await archiveIndexHandler.BuildArchiveIndexesAsync(cdnConfig);
                await downloadFileHandler.ParseDownloadFileAsync(buildConfig);

                   // Start processing to determine which files need to be downloaded
                   ctx.Status("Determining files to download...");
                   await installFileHandler.HandleInstallFileAsync(buildConfig, archiveIndexHandler, cdnConfig, product);
                   await downloadFileHandler.HandleDownloadFileAsync(archiveIndexHandler, cdnConfig, product);

                var patchLoader = new PatchLoader(cdnRequestManager, cdnConfig);
                await patchLoader.HandlePatchesAsync(buildConfig, product);
            });

            // Actually start the download of any deferred requests
            await cdnRequestManager.DownloadQueuedRequestsAsync(ansiConsole);

            timer.Stop();
            AnsiConsole.MarkupLine($"{Blue(product.DisplayName)} pre-loaded in {Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}\n\n");

            if (debugConfig.CompareAgainstRealRequests)
            {
                var comparisonUtil = new ComparisonUtil();
                var result = await comparisonUtil.CompareAgainstRealRequestsAsync(cdnRequestManager.allRequestsMade.ToList(), product);
                result.ElapsedTime = timer.Elapsed;

                return result;
            }

            return null;
        }
    }
}
