using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuildBackup.DataAccess;
using BuildBackup.DebugUtil;
using BuildBackup.DebugUtil.Models;
using BuildBackup.Handlers;
using BuildBackup.Structs;
using Konsole;
using Colors = Shared.Colors;

namespace BuildBackup
{
    public static class ProductHandler
    {
        public static ComparisonResult ProcessProduct(TactProduct product, IConsole console, bool useDebugMode, bool writeOutputFiles)
        {
            var timer = Stopwatch.StartNew();
            Console.WriteLine($"Now starting processing of : {Shared.Colors.Cyan(product.DisplayName)}");

            // Loading CDNs
            CDN cdn = new CDN(console, Config.BattleNetPatchUri)
            {
                DebugMode = useDebugMode
            };
            cdn.LoadCdnsFile(product);

            // Initializing other classes, now that we have our CDN info loaded
            var encodingFileHandler = new EncodingFileHandler(cdn);
            var downloadFileHandler = new DownloadFileHandler(cdn);

            // Finding the latest version of the game
            Logic logic = new Logic(cdn);
            VersionsEntry targetVersion = logic.GetVersionEntry(product);

            BuildConfigFile buildConfig = BuildConfigHandler.GetBuildConfig(targetVersion, cdn);
            
            CDNConfigFile cdnConfig = logic.GetCDNconfig(targetVersion);

            logic.GetBuildConfigAndEncryption(product, cdnConfig, targetVersion, cdn);

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

            ComparisonResult result = new ComparisonUtil(console).CompareAgainstRealRequests(cdn.allRequestsMade.ToList(), product, writeOutputFiles);
            result.ElapsedTime = timer.Elapsed;

            return result;
        }
    }
}
