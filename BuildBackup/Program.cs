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
        private static TactProduct[] ProductsToProcess = new[]{ TactProducts.Starcraft2 };

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

            CDN cdn = new CDN(console, Config.BattleNetPatchUri) 
            { 
                DebugMode = useDebugMode
            };
            Logic logic = new Logic(cdn, Config.BattleNetPatchUri);

            // Loading CDNs
            var cdnFileHandler = new CdnFileHandler(cdn, Config.BattleNetPatchUri);
            CdnsFile cdnsFile = cdnFileHandler.ParseCdnsFile(product);
            

            // Initializing other classes, now that we have our CDN info loaded
            var encodingFileHandler = new EncodingFileHandler(cdnsFile, cdn);

            // Finding the latest version of the game
            VersionsEntry targetVersion = logic.GetVersionEntry(product);
            BuildConfigFile buildConfig = Requests.GetBuildConfig(cdnsFile.entries[0].path, targetVersion, cdn);
            //TODO put this into a method
            cdn.QueueRequest($"{cdnsFile.entries[0].path}/data/", buildConfig.size[1], writeToDevNull: true);

            CDNConfigFile cdnConfig = logic.GetCDNconfig(cdnsFile.entries[0].path, targetVersion);

            logic.GetBuildConfigAndEncryption(product, cdnConfig, targetVersion, cdn, cdnsFile);
            
            EncodingTable encodingTable = encodingFileHandler.BuildEncodingTable(buildConfig);
            DownloadFile downloadFile = DownloadFileHandler.ParseDownloadFile(cdn, cdnsFile.entries[0].path, buildConfig.download[1].ToString());
            var archiveIndexDictionary = IndexParser.BuildArchiveIndexes(cdnsFile.entries[0].path, cdnConfig, cdn, product, new Uri("http://level3.blizzard.com"));

            // Starting the download
            var ribbit = new Ribbit(cdn, cdnsFile);
            ribbit.HandleInstallFile(cdnConfig, encodingTable, cdn, cdnsFile, archiveIndexDictionary);
            ribbit.HandleDownloadFile(cdn, cdnsFile, downloadFile, archiveIndexDictionary, cdnConfig, encodingTable);

            var patchLoader = new PatchLoader(cdn, cdnsFile, console, product, cdnConfig);
            PatchFile patch = patchLoader.DownloadPatchConfig(buildConfig);
            patchLoader.HandlePatches(patch);

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
