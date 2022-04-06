using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BuildBackup.DataAccess;
using BuildBackup.DebugUtil;
using BuildBackup.Structs;
using Konsole;
using Newtonsoft.Json;
using Shared;
using Shared.Models;
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

            CDN cdn = new CDN(console) { DebugMode = useDebugMode };
            Logic logic = new Logic(cdn, Config.BattleNetPatchUri);

            // Loading CDNs
            var timer2 = Stopwatch.StartNew();
            CdnsFile cdns = logic.GetCDNs(product);
            Console.WriteLine($"GetCDNs loaded in {Colors.Yellow(timer2.Elapsed.ToString(@"mm\:ss\.FFFF"))}");

            // Initializing other classes, now that we have our CDN info loaded
            var patchLoader = new PatchLoader(cdn, cdns, console, product);
            var unarchivedFileHandler = new UnarchivedFileHandler(cdn, cdns, console);
            var ribbit = new Ribbit(cdn, cdns);
            var encodingFileHandler = new EncodingFileHandler(cdns, cdn);

            // Finding the latest version of the game
            VersionsEntry targetVersion = logic.GetVersionEntry(product);
            BuildConfigFile buildConfig = Requests.GetBuildConfig(cdns.entries[0].path, targetVersion, cdn);
            CDNConfigFile cdnConfig = logic.GetCDNconfig(cdns.entries[0].path, targetVersion);

            GetBuildConfigAndEncryption(product, cdnConfig, targetVersion, cdn, cdns, logic);

            EncodingTable encodingTable = encodingFileHandler.BuildEncodingTable(buildConfig);
            var downloadFile = DownloadFileHandler.ParseDownloadFile(cdn, cdns.entries[0].path, encodingTable.downloadKey);
            var archiveIndexDictionary = IndexParser.BuildArchiveIndexes(cdns.entries[0].path, cdnConfig, cdn);

            // Starting the download
            ribbit.HandleInstallFile(cdnConfig, encodingTable, cdn, cdns, archiveIndexDictionary);
            ribbit.HandleDownloadFile(cdn, cdns, downloadFile, archiveIndexDictionary);

            unarchivedFileHandler.DownloadUnarchivedFiles(cdnConfig, encodingTable);

            //DownloadFileHandler.DownloadFullArchives(cdnConfig, cdn, cdns);

            PatchFile patch = patchLoader.DownloadPatchConfig(buildConfig);
            patchLoader.DownloadPatchArchives(cdnConfig, patch);
            patchLoader.DownloadPatchFiles(cdnConfig);
            patchLoader.DownloadFullPatchArchives(cdnConfig);

            cdn.DownloadQueuedRequests();

            Console.WriteLine();
            Console.WriteLine($"{Colors.Cyan(product.DisplayName)} pre-loaded in {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");

            var comparisonUtil = new ComparisonUtil(console);
            var result = comparisonUtil.CompareAgainstRealRequests(cdn.allRequestsMade.ToList(), product);

            //File.WriteAllText($@"C:\Users\Tim\Dropbox\Programming\dotnet-public\missing.json", JsonConvert.SerializeObject(result.Misses.OrderBy(e => e.Uri).ThenBy(e => e.LowerByteRange)));
           // File.WriteAllText($@"C:\Users\Tim\Dropbox\Programming\dotnet-public\excess.json", JsonConvert.SerializeObject(result.UnnecessaryRequests));
            return result;
        }

        private static void GetBuildConfigAndEncryption(TactProduct product, CDNConfigFile cdnConfig, VersionsEntry targetVersion, CDN cdn, CdnsFile cdns, Logic logic)
        {
            // Not required by these products
            if (product == TactProducts.Starcraft1)
            {
                return;
            }

            if (cdnConfig.builds != null)
            {
                BuildConfigFile[] cdnBuildConfigs = new BuildConfigFile[cdnConfig.builds.Count()];
            }

            if (!string.IsNullOrEmpty(targetVersion.keyRing))
            {
                // Starcraft 2 calls this
                cdn.Get($"{cdns.entries[0].path}/config/", targetVersion.keyRing);
            }

            //Let us ignore this whole encryption thing if archives are set, surely this will never break anything and it'll back it up perfectly fine.
            var decryptionKeyName = logic.GetDecryptionKeyName(cdns, product, targetVersion);
            //if (!string.IsNullOrEmpty(decryptionKeyName) && cdnConfig.archives == null)
            //{
            //    if (!File.Exists(decryptionKeyName + ".ak"))
            //    {
            //        Console.WriteLine("Decryption key is set and not available on disk, skipping.");
            //        cdn.isEncrypted = false;
            //        return true;
            //    }
            //    else
            //    {
            //        cdn.isEncrypted = true;
            //    }
            //}
            //else
            //{
            //    cdn.isEncrypted = false;
            //}
        }
    }
}
