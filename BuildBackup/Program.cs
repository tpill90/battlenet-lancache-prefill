using System;
using System.Diagnostics;
using System.Linq;
using BuildBackup.DataAccess;
using BuildBackup.DebugUtil;
using BuildBackup.DebugUtil.Models;
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
    public class Program
    {
        private static TactProduct[] ProductsToProcess = new[]{ TactProducts.WorldOfWarcraft };

        public static bool UseCdnDebugMode = true;
        
        public static void Main()
        {
            foreach (var product in ProductsToProcess)
            {
                ProcessProduct(product, new Writer(), UseCdnDebugMode);
            }
            Console.WriteLine("Pre-load Complete!\n");

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press any key to continue . . .");
                Console.ReadLine();
            }
        }

        public static ComparisonResult ProcessProduct(TactProduct product, IConsole console, bool useDebugMode)
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

            // Finding the latest version of the game
            Logic logic = new Logic(cdn, Config.BattleNetPatchUri);
            VersionsEntry targetVersion = logic.GetVersionEntry(product);
            //logic.GetDecryptionKeyName(product, targetVersion);

            BuildConfigFile buildConfig = Requests.GetBuildConfig(targetVersion, cdn);
            //TODO put this into a method
            cdn.QueueRequest(RootFolder.data, buildConfig.size[1], writeToDevNull: true);

            CDNConfigFile cdnConfig = logic.GetCDNconfig(targetVersion);

            logic.GetBuildConfigAndEncryption(product, cdnConfig, targetVersion, cdn);
            
            EncodingTable encodingTable = encodingFileHandler.BuildEncodingTable(buildConfig);
            DownloadFile downloadFile = DownloadFileHandler.ParseDownloadFile(cdn, buildConfig);
            var archiveIndexDictionary = IndexParser.BuildArchiveIndexes(cdnConfig, cdn);

            // Starting the download
            var ribbit = new Ribbit(cdn);
            ribbit.HandleInstallFile(encodingTable, archiveIndexDictionary, product);
            DownloadFileHandler.HandleDownloadFile(downloadFile, archiveIndexDictionary, cdnConfig, cdn, product);

            var patchLoader = new PatchLoader(cdn, console, product, cdnConfig);
            PatchFile patch = patchLoader.DownloadPatchConfig(buildConfig);
            patchLoader.HandlePatches(patch);

            if (product == TactProducts.CodWarzone || product == TactProducts.CodBlackOpsColdWar 
                                                   || product == TactProducts.CodVanguard 
                                                   || product == TactProducts.WorldOfWarcraft)
            {
                var unarchivedHandler = new UnarchivedFileHandler(cdn, console);
                //unarchivedHandler.DownloadUnarchivedFiles(cdnConfig, encodingTable, archiveIndexDictionary);
                //unarchivedHandler.DownloadUnarchivedIndexFiles(cdnConfig, downloadFile, encodingTable);
            }
            
            cdn.DownloadQueuedRequests();

            Console.WriteLine();
            timer.Stop();
            Console.WriteLine($"{Colors.Cyan(product.DisplayName)} pre-loaded in {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");

            var comparisonUtil = new ComparisonUtil(console);
            ComparisonResult result = comparisonUtil.CompareAgainstRealRequests(cdn.allRequestsMade.ToList(), product);
            result.ElapsedTime = timer.Elapsed;

            return result;
        }
    }
}
