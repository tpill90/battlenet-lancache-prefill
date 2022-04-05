using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BuildBackup.Structs;
using ByteSizeLib;
using Shared;
using Shared.Models;
using Colors = Shared.Colors;

namespace BuildBackup.DataAccess
{
    public class Ribbit
    {
        private CDN _cdn;
        private CdnsFile _cdns;

        public Ribbit(CDN cdn, CdnsFile cdns)
        {
            _cdn = cdn;

            Debug.Assert(cdns.entries != null, "Cdns must be initialized before using");
            _cdns = cdns;
        }

        //TODO comment
        public void HandleInstallFile(CDNConfigFile cdnConfig, EncodingTable encodingTable, InstallFile installFile, 
            CDN cdn, CdnsFile cdns, Dictionary<string, IndexEntry> archiveIndexDictionary)
        {
            Console.Write("Parsing install file list...");
            var timer = Stopwatch.StartNew();

            Dictionary<string, IndexEntry> fileIndexList = IndexParser.ParseIndex(_cdns.entries[0].path, cdnConfig.fileIndex, _cdn, "data");

            //TODO lookup why these are missing
            List<InstallFileEntry> hashLookupMisses = new List<InstallFileEntry>();

            // Doing a reverse lookup on the manifest to find the index key for each file's content hash.  
            var archiveIndexDownloads = new List<InstallFileMatch>();
            var fileIndexDownloads = new List<InstallFileMatch>();

            var reverseLookupDictionary = encodingTable.EncodingDictionary.ToDictionary(e => e.Value, e => e.Key);

            var filtered = installFile.entries
                .Where(e => e.tags.Any(e2 => e2.Contains("enUS")))
                .ToList();
            foreach (var file in filtered)
            {
                //The manifest contains pairs of IndexId-ContentHash, reverse lookup for matches based on the ContentHash
                if (reverseLookupDictionary.ContainsKey(file.contentHashString.ToUpper()))
                {
                    // If we found a match for the archive content, look into the archive index to see where the file can be downloaded from
                    var upperHash = reverseLookupDictionary[file.contentHashString].ToUpper();

                    if (archiveIndexDictionary.ContainsKey(upperHash))
                    {
                        IndexEntry archiveIndex = archiveIndexDictionary[upperHash];
                        archiveIndexDownloads.Add(new InstallFileMatch { IndexEntry = archiveIndex, InstallFileEntry = file });
                    }
                    else if (fileIndexList.ContainsKey(upperHash))
                    {
                        IndexEntry indexMatch = fileIndexList[upperHash];
                        fileIndexDownloads.Add(new InstallFileMatch() { IndexEntry = indexMatch, InstallFileEntry = file });
                        //TODO Not sure what needs to be done here
                        //Debugger.Break();
                    }
                    else
                    {
                        hashLookupMisses.Add(file);
                    }
                }
                else
                {
                    hashLookupMisses.Add(file);
                }
            }


            var requests = archiveIndexDownloads.Select(e => new Request
            {
                Uri = e.IndexEntry.IndexId,
                LowerByteRange = (int)e.IndexEntry.offset,
                // Need to subtract 1, since the byte range is "inclusive"
                UpperByteRange = ((int)e.IndexEntry.offset + (int)e.IndexEntry.size - 1)
            }).ToList();
            requests = NginxLogParser.CoalesceRequests(requests);

            foreach(var indexDownload in requests)
            {
                cdn.QueueRequest($"{cdns.entries[0].path}/data/", indexDownload.Uri, indexDownload.LowerByteRange, indexDownload.UpperByteRange, true);
            }
            Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");
        }

        public void HandleDownloadFile(CDN cdn, CdnsFile cdns, DownloadFile download, Dictionary<string, IndexEntry> archiveIndexDictionary)
        {
            Console.Write("Parsing download file list...");
            var timer = Stopwatch.StartNew();

            var indexDownloads = 0;
            var totalBytes = 0L;

            //TODO make this more flexible.  Perhaps pass in the region by name?
            var tagToUse = download.tags.Single(e => e.Name.Contains("enUS"));
            var tagToUse2 = download.tags.Single(e => e.Name.Contains("Windows"));

            var asd = download.tags.Select(e => e.Name).ToList();

            for (var i = 0; i < download.entries.Length; i++)
            {
                var current = download.entries[i];

                // Filtering out files that shouldn't be downloaded by tag.  Ex. only want English audio files for a US install
                if (tagToUse.Bits[i] == false || tagToUse2.Bits[i] == false)
                {
                    continue;
                }
                if (!archiveIndexDictionary.ContainsKey(current.hash))
                {
                    continue;
                }
                
                IndexEntry e = archiveIndexDictionary[current.hash];

                // Need to subtract 1, since the byte range is "inclusive"
                int upperByteRange = ((int) e.offset + (int) e.size - 1);
                cdn.QueueRequest($"{cdns.entries[0].path}/data/", e.IndexId, e.offset, upperByteRange, writeToDevNull: true);

                indexDownloads++;
                totalBytes += (upperByteRange - e.offset);
            }
            
            Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");
        }
    }
}
