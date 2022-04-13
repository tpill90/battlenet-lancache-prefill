using System;
using System.Diagnostics;
using System.Linq;
using BuildBackup.DataAccess;
using BuildBackup.DebugUtil;
using BuildBackup.DebugUtil.Models;
using BuildBackup.Handlers;
using BuildBackup.Structs;
using Konsole;
using Colors = Shared.Colors;

namespace BuildBackup
{
    /// <summary>
    /// Documentation :
    ///   https://wowdev.wiki/TACT
    ///   https://github.com/d07RiV/blizzget/wiki
    /// </summary>
    public static class ProductHandler
    {
        //TODO comment parameters
        public static ComparisonResult ProcessProduct(TactProduct product, IConsole console, bool useDebugMode, bool writeOutputFiles, bool showDebugStats)
        {
            var timer = Stopwatch.StartNew();
            Console.WriteLine($"Now starting processing of : {Colors.Cyan(product.DisplayName)}");

            // Loading CDNs
            CDN cdn = new CDN(console, Config.BattleNetPatchUri)
            {
                DebugMode = useDebugMode
            };
            cdn.LoadCdnsFile(product);

            // Initializing other classes, now that we have our CDN info loaded
            
            var downloadFileHandler = new DownloadFileHandler(cdn);
            var configFileHandler = new ConfigFileHandler(cdn);
            var installFileHandler = new InstallFileHandler(cdn);

            // Finding the latest version of the game
            VersionsEntry targetVersion = configFileHandler.GetLatestVersionEntry(product);
            configFileHandler.QueueKeyRingFile(targetVersion);

            // Getting other configuration files for this version, that detail where we can download the required files from.
            BuildConfigFile buildConfig = BuildConfigHandler.GetBuildConfig(targetVersion, cdn, product);
            CDNConfigFile cdnConfig = configFileHandler.GetCDNconfig(targetVersion);

            downloadFileHandler.ParseDownloadFile(buildConfig);
            var archiveIndexDictionary = IndexParser.BuildArchiveIndexes(cdnConfig, cdn);

            // Start processing to determine which files need to be downloaded
            installFileHandler.HandleInstallFile(buildConfig, archiveIndexDictionary, product);
            downloadFileHandler.HandleDownloadFile(archiveIndexDictionary, cdnConfig, product);

            var patchLoader = new PatchLoader(cdn, cdnConfig);
            patchLoader.HandlePatches(buildConfig, product);

            // Actually start the download of any deferred requests
            cdn.DownloadQueuedRequests();

            timer.Stop();
            Console.WriteLine();
            Console.WriteLine($"{Colors.Cyan(product.DisplayName)} pre-loaded in {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}\n");

            if (showDebugStats)
            {
                ComparisonResult result = new ComparisonUtil(console).CompareAgainstRealRequests(cdn.allRequestsMade.ToList(), product, writeOutputFiles);
                result.ElapsedTime = timer.Elapsed;

                return result;
            }

            return null;
        }
    }
}
