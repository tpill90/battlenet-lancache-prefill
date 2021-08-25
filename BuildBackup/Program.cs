using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AutoMapper;
using BuildBackup.DataAccess;
using BuildBackup.Structs;
using ByteSizeLib;
using Konsole;
using Shared;
using Shared.Models;
using Colors = Shared.Colors;

namespace BuildBackup
{
    //TODO get an actual logger configured + get some color + better logging info
    /// <summary>
    /// Documentation :
    ///   https://wowdev.wiki/TACT
    ///   https://blizztrack.com/
    /// </summary>
    public class Program
    {
        private static readonly Uri baseUrl = new Uri("http://us.patch.battle.net:1119/");

        // From : https://blizztrack.com/
        private static TactProduct[] checkPrograms = new TactProduct[]{ TactProducts.Starcraft2 };

        public static bool UseCdnDebugMode = true;
        
        private static readonly string LogFileBasePath = @"C:\Users\Tim\Dropbox\Programming\dotnet\BattleNetBackup\RequestReplayer\Logs";

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
            PatchLoader patchLoader = new PatchLoader(cdn, cdns);
            Downloader downloader = new Downloader(cdn, cdns, console);
            DataAccess.Ribbit ribbit = new DataAccess.Ribbit(cdn, cdns);

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

            Console.WriteLine("Loading encoding table...");
            EncodingFile encoding = logic.GetEncoding(buildConfig, cdns);
            EncodingTable encodingTable = logic.BuildEncodingTable(buildConfig, encoding);

            //downloader.DownloadFullArchives(cdnConfig);

            var ribbitResult = ribbit.ProcessRibbit(product, encodingTable.rootKey, logic, encodingTable.downloadKey, encodingTable.installKey);
            ribbit.DownloadIndexedFilesFromArchive(cdnConfig, encodingTable.EncodingDictionary, ribbitResult.Item2, cdn, cdns);

            downloader.DownloadUnarchivedFiles(cdnConfig, encodingTable.EncodingDictionary);

            //TODO reenable.  Unrelated to cache priming
            //Downloader.DownloadFilesFromIndex(cdnConfig, cdns, cdn);
            //DownloadFilesFromIndex2(cdnConfig, fileIndexMatches.ToDictionary(e => e.Key, e=> e.Value));

            PatchFile patch = patchLoader.DownloadPatchConfig(buildConfig);
            patchLoader.DownloadPatchFiles(cdnConfig);
            patchLoader.DownloadPatchArchives(cdnConfig, patch);

            Console.WriteLine();
            Console.WriteLine($"{Colors.Cyan(product.DisplayName)} pre-loaded in {Colors.Yellow(timer.Elapsed.ToString())}");

            return CompareToRealRequests(cdn.allRequestsMade.ToList(), product);
        }

        public class ComparisonResult
        {
            public List<ComparedRequest> Hits { get; set; }
            public List<ComparedRequest> Misses { get; set; }

            public int HitCount => Hits.Count;
            public int MissCount => Misses.Count;
        }

        //TODO need to calculate total bandwidth waste as well.
        private static ComparisonResult CompareToRealRequests(List<Request> allRequestsMade, TactProduct product)
        {
            //TODO re-implement coalescing + dedupe.  However this messes with the FullDownloadProperty
            //allRequestsMade = NginxLogParser.CoalesceRequests(allRequestsMade);
            
            var mapperConfig = new MapperConfiguration(cfg => cfg.CreateMap<Request, ComparedRequest>());
            var mapper = new Mapper(mapperConfig);

            var realRequests = NginxLogParser.CoalesceRequests(NginxLogParser.ParseRequestLogs(LogFileBasePath, product));

            

            List<ComparedRequest> realRequestMatches = realRequests.Select(e => mapper.Map<ComparedRequest>(e)).ToList();

            allRequestsMade = allRequestsMade.Where(e => e != null).ToList();
            if (allRequestsMade.Any(e => e == null))
            {
                //TODO debug this, probably a threading issue.
                Debugger.Break();
            }

            foreach (var realRequest in realRequestMatches)
            {
                // Finding any requests that match on URI
                var uriMatches = allRequestsMade.Where(e => e.Uri == realRequest.Uri).ToList();

                // Handle each one of the matches
                foreach (var match in uriMatches)
                {
                    if (match.DownloadWholeFile)
                    {
                        realRequest.Matched = true;
                        realRequest.MatchedRequest = match;
                    }
                }
            }

            var comparisonResult = new ComparisonResult
            {
                Hits = realRequestMatches.Where(e => e.Matched == true).ToList(),
                Misses = realRequestMatches.Where(e => e.Matched == false).ToList()
            };

            Console.WriteLine("Total requests made : " + Colors.Cyan(allRequestsMade.Count));
            Console.WriteLine("Real requests made : " + Colors.Cyan(realRequests.Count));
            Console.WriteLine();

            var totalRequestSize = ByteSize.FromBytes((double) allRequestsMade.Sum(e => e.TotalBytes)).GigaBytes;
            Console.WriteLine("Total bandwidth required: " + Colors.Cyan(totalRequestSize) + "gb");
            Console.WriteLine("Missing request byte size : " + allRequestsMade.Count(e => e.TotalBytes == 0));

            var realRequestSize = ByteSize.FromBytes((double)realRequests.Sum(e => e.TotalBytes)).GigaBytes;
            Console.WriteLine("Real bandwidth required : " + Colors.Cyan(realRequestSize) + "gb");
            Console.WriteLine("Missing request byte size : " + realRequests.Count(e => e.TotalBytes == 0));

            Console.WriteLine($"Total Hits : {Colors.Green(comparisonResult.HitCount)}");
            Console.WriteLine($"Total Misses : {Colors.Red(comparisonResult.MissCount)}");
            Console.WriteLine();


            return comparisonResult;
        }

        public class ComparedRequest : Request
        {
            public bool Matched { get; set; }

            public Request MatchedRequest { get; set; }
        }
    }
}
