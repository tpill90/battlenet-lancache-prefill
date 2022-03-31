using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BuildBackup.DataAccess;
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
    /// </summary>
    public class Program
    {
        private static readonly Uri baseUrl = new Uri("http://us.patch.battle.net:1119/");
       
        private static TactProduct[] checkPrograms = new TactProduct[]{ TactProducts.Starcraft1 };

        public static bool UseCdnDebugMode = true;
        

        public static void Main()
        {
            foreach (var product in checkPrograms)
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
            CDN cdn = new CDN
            {
                DebugMode = useDebugMode
            };

            Logic logic = new Logic(cdn, baseUrl);
            
            var timer = Stopwatch.StartNew();

            Console.WriteLine($"Now starting processing of : {Colors.Cyan(product.DisplayName)}");

            // Finding the latest version of the game
            VersionsEntry targetVersion = logic.GetVersionEntry(product);

            // Loading CDNs
            CdnsFile cdns = logic.GetCDNs(product);

            // Initializing other classes, now that we have our CDN info loaded
            var patchLoader = new PatchLoader(cdn, cdns, console);
            var downloader = new Downloader(cdn, cdns, console);
            var ribbit = new Ribbit(cdn, cdns);

            BuildConfigFile buildConfig = Requests.GetBuildConfig(cdns.entries[0].path, targetVersion.buildConfig, cdn);
            CDNConfigFile cdnConfig = logic.GetCDNconfig(cdns.entries[0].path, targetVersion.cdnConfig);

            if (cdnConfig.builds != null)
            {
                BuildConfigFile[] cdnBuildConfigs = new BuildConfigFile[cdnConfig.builds.Count()];
            }

            if (!string.IsNullOrEmpty(targetVersion.keyRing))
            {
                cdn.Get($"{cdns.entries[0].path}/config/", targetVersion.keyRing);
            }

            // Let us ignore this whole encryption thing if archives are set, surely this will never break anything and it'll back it up perfectly fine.
            var decryptionKeyName = logic.GetDecryptionKeyName(cdns, product, targetVersion);
            if (!string.IsNullOrEmpty(decryptionKeyName) && cdnConfig.archives == null)
            {
                if (!File.Exists(decryptionKeyName + ".ak"))
                {
                    Console.WriteLine("Decryption key is set and not available on disk, skipping.");
                    cdn.isEncrypted = false;
                    return null;
                }
                else
                {
                    cdn.isEncrypted = true;
                }
            }
            else
            {
                cdn.isEncrypted = false;
            }

            EncodingTable encodingTable = logic.BuildEncodingTable(buildConfig, cdns);

            downloader.DownloadFullArchives(cdnConfig);

            (DownloadFile, InstallFile) ribbitResult = ribbit.ProcessRibbit(encodingTable.rootKey, logic, encodingTable.downloadKey, encodingTable.installKey);
            ribbit.DownloadIndexedFilesFromArchive(cdnConfig, encodingTable.EncodingDictionary, ribbitResult.Item2, cdn, cdns);

            downloader.DownloadUnarchivedFiles(cdnConfig, encodingTable.EncodingDictionary);
            
            PatchFile patch = patchLoader.DownloadPatchConfig(buildConfig);
            patchLoader.DownloadPatchFiles(cdnConfig);
            patchLoader.DownloadPatchArchives(cdnConfig, patch);

            Console.WriteLine();
            Console.WriteLine($"{Colors.Cyan(product.DisplayName)} pre-loaded in {Colors.Yellow(timer.Elapsed.ToString())}");

            return ComparisonUtil.CompareToRealRequests(cdn.allRequestsMade.ToList(), product);
        }
    }
}
