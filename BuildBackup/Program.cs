using System;
using System.Diagnostics;
using System.Linq;
using BuildBackup.DataAccess;
using BuildBackup.DebugUtil;
using BuildBackup.Structs;
using Konsole;
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
        private static readonly Uri baseUrl = new Uri("http://us.patch.battle.net:1119/");
       
        private static TactProduct[] ProductsToProcess = new[]{ TactProducts.Starcraft1 };

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
            Logic logic = new Logic(cdn, baseUrl);

            // Loading CDNs
            var timer2 = Stopwatch.StartNew();
            CdnsFile cdns = logic.GetCDNs(product);
            Console.WriteLine($"GetCDNs loaded in {Colors.Yellow(timer2.Elapsed.ToString(@"mm\:ss\.FFFF"))}");

            // Initializing other classes, now that we have our CDN info loaded
            var patchLoader = new PatchLoader(cdn, cdns, console);
            var downloader = new Downloader(cdn, cdns, console);
            var ribbit = new Ribbit(cdn, cdns, console);

            // Finding the latest version of the game
            VersionsEntry targetVersion = logic.GetVersionEntry(product);
            BuildConfigFile buildConfig = Requests.GetBuildConfig(cdns.entries[0].path, targetVersion, cdn);
            CDNConfigFile cdnConfig = logic.GetCDNconfig(cdns.entries[0].path, targetVersion);

            EncodingTable encodingTable = logic.BuildEncodingTable(buildConfig, cdns);

            var downloadFile = DownloadFileHandler.ParseDownloadFile(cdn, cdns.entries[0].path, encodingTable.downloadKey);
            var installFile = logic.GetInstall(cdns.entries[0].path, encodingTable.installKey);

            var archiveIndexDictionary = IndexParser.BuildArchiveIndexes(cdns.entries[0].path, cdnConfig, cdn);
            ribbit.HandleInstallFile(cdnConfig, encodingTable, installFile, cdn, cdns, archiveIndexDictionary);
            ribbit.HandleDownloadFile(cdn, cdns, downloadFile, archiveIndexDictionary);

            downloader.DownloadUnarchivedFiles(cdnConfig, encodingTable);

            PatchFile patch = patchLoader.DownloadPatchConfig(buildConfig);
            patchLoader.DownloadPatchArchives(cdnConfig, patch, product);
            patchLoader.DownloadPatchFiles(cdnConfig, product);
            patchLoader.DownloadFullPatchArchives(cdnConfig);

            cdn.DownloadQueuedRequests();

            Console.WriteLine();
            Console.WriteLine($"{Colors.Cyan(product.DisplayName)} pre-loaded in {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");

            var comparisonUtil = new ComparisonUtil(console);
            var result = comparisonUtil.CompareAgainstRealRequests(cdn.allRequestsMade.ToList(), product);
            return result;
        }

        private static bool GetBuildConfigAndEncryption(TactProduct product, CDNConfigFile cdnConfig, VersionsEntry targetVersion, CDN cdn, CdnsFile cdns, Logic logic)
        {
            if (cdnConfig.builds != null)
            {
                BuildConfigFile[] cdnBuildConfigs = new BuildConfigFile[cdnConfig.builds.Count()];
            }

            if (!string.IsNullOrEmpty(targetVersion.keyRing))
            {
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

            return false;
        }
    }
}
