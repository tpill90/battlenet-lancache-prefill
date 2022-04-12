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
        public static ComparisonResult ProcessProduct(TactProduct product, IConsole console, 
            bool useDebugMode, bool writeOutputFiles, bool showDebugStats)
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
            var encodingFileHandler = new EncodingFileHandler(cdn);
            var downloadFileHandler = new DownloadFileHandler(cdn);
            var configFileHandler = new ConfigFileHandler(cdn);

            // Finding the latest version of the game
            VersionsEntry targetVersion = configFileHandler.GetLatestVersionEntry(product);
            // Getting other configuration files for this version, that detail where we can download the required files from.
            BuildConfigFile buildConfig = BuildConfigHandler.GetBuildConfig(targetVersion, cdn);
            CDNConfigFile cdnConfig = configFileHandler.GetCDNconfig(targetVersion);

            configFileHandler.GetBuildConfigAndEncryption(targetVersion);

            EncodingTable encodingTable = encodingFileHandler.BuildEncodingTable(buildConfig);
            downloadFileHandler.ParseDownloadFile(buildConfig);
            var archiveIndexDictionary = IndexParser.BuildArchiveIndexes(cdnConfig, cdn);

            // Starting the download
            var ribbit = new Ribbit(cdn);
            ribbit.HandleInstallFile(encodingTable, archiveIndexDictionary, product);
            downloadFileHandler.HandleDownloadFile(archiveIndexDictionary, cdnConfig, product);

            var patchLoader = new PatchLoader(cdn, cdnConfig);
            patchLoader.HandlePatches(buildConfig);

            if (buildConfig.vfsRoot != null)
            {
                cdn.QueueRequest(RootFolder.data, buildConfig.vfsRoot[1], 0, buildConfig.vfsRootSize[1] - 1, true);
            }

            cdn.DownloadQueuedRequests();

            timer.Stop();
            Console.WriteLine();
            Console.WriteLine($"{Shared.Colors.Cyan(product.DisplayName)} pre-loaded in {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");

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
