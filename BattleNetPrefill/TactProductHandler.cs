using System.Diagnostics;
using System.Linq;
using BattleNetPrefill.DataAccess;
using BattleNetPrefill.DebugUtil;
using BattleNetPrefill.DebugUtil.Models;
using BattleNetPrefill.Handlers;
using BattleNetPrefill.Parsers;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Web;
using Spectre.Console;
using Colors = BattleNetPrefill.Utils.Colors;

namespace BattleNetPrefill
{
    /// <summary>
    /// Documentation :
    ///   https://wowdev.wiki/TACT
    ///   https://github.com/d07RiV/blizzget/wiki
    /// </summary>
    public static class TactProductHandler
    {
        //TODO comment parameters
        public static ComparisonResult ProcessProduct(TactProduct product, IAnsiConsole ansiConsole, 
            bool useDebugMode = false, bool writeOutputFiles = false, bool showDebugStats = false, bool skipDiskCache = false)
        {
            var timer = Stopwatch.StartNew();
            AnsiConsole.WriteLine($"Now starting processing of : {Colors.Cyan(product.DisplayName)}");

            // Initializing classes, now that we have our CDN info loaded
            CDN cdn = new CDN(Config.BattleNetPatchUri, useDebugMode, skipDiskCache);
            var downloadFileHandler = new DownloadFileHandler(cdn);
            var configFileHandler = new ConfigFileHandler(cdn);
            var installFileHandler = new InstallFileHandler(cdn);
            var archiveIndexHandler = new ArchiveIndexHandler(cdn, product);

            var spectreStatus = ansiConsole.Status()
                       .AutoRefresh(true)
                       .SpinnerStyle(Style.Parse("green"))
                       .Spinner(Spinner.Known.Dots2);
            spectreStatus.Start("Start", ctx =>
               {
                   // Finding the latest version of the game
                   ctx.Status("Getting latest version info...");
                   cdn.LoadCdnsFile(product);
                   VersionsEntry targetVersion = configFileHandler.GetLatestVersionEntry(product);
                   configFileHandler.QueueKeyRingFile(targetVersion);

                   // Getting other configuration files for this version, that detail where we can download the required files from.
                   ctx.Status("Getting latest config files...");
                   BuildConfigFile buildConfig = BuildConfigParser.GetBuildConfig(targetVersion, cdn, product);
                   CDNConfigFile cdnConfig = configFileHandler.GetCDNconfig(targetVersion);

                   ctx.Status("Building Archive Indexes...");
                   archiveIndexHandler.BuildArchiveIndexes(cdnConfig);
                   downloadFileHandler.ParseDownloadFile(buildConfig);

                   // Start processing to determine which files need to be downloaded
                   ctx.Status("Determining files to download...");
                   installFileHandler.HandleInstallFile(buildConfig, archiveIndexHandler, cdnConfig, product);
                   downloadFileHandler.HandleDownloadFile(archiveIndexHandler, cdnConfig, product);

                   var patchLoader = new PatchLoader(cdn, cdnConfig);
                   patchLoader.HandlePatches(buildConfig, product);
               });
            
            // Actually start the download of any deferred requests
            cdn.DownloadQueuedRequestsAsync(ansiConsole).Wait();

            timer.Stop();
            AnsiConsole.WriteLine($"{Colors.Cyan(product.DisplayName)} pre-loaded in {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}\n\n");

            if (showDebugStats)
            {
                ComparisonResult result = new ComparisonUtil().CompareAgainstRealRequests(cdn.allRequestsMade.ToList(), product, writeOutputFiles);
                result.ElapsedTime = timer.Elapsed;

                return result;
            }

            return null;
        }
    }
}
