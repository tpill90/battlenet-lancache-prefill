using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BuildBackup.Structs;
using ByteSizeLib;
using Konsole;
using Shared;
using Shared.Models;
using Colors = Shared.Colors;

namespace BuildBackup.DataAccess
{
    public class Ribbit
    {
        private CDN _cdn;
        private CdnsFile _cdns;
        private readonly IConsole _console;

        public Ribbit(CDN cdn, CdnsFile cdns, IConsole console)
        {
            _cdn = cdn;

            Debug.Assert(cdns.entries != null, "Cdns must be initialized before using");
            _cdns = cdns;
            _console = console;
        }

        //TODO comment
        public void DownloadIndexedFilesFromArchive(CDNConfigFile cdnConfig, EncodingTable encodingTable, InstallFile installFile, 
            CDN cdn, CdnsFile cdns, Dictionary<string, IndexEntry> archiveIndexDictionary)
        {
            Console.WriteLine("Parsing install file list.");
            var timer = Stopwatch.StartNew();

            Dictionary<string, IndexEntry> fileIndexList = IndexParser.ParseIndex(_cdns.entries[0].path, cdnConfig.fileIndex, _cdn, "data");

            //TODO lookup why these are missing
            List<InstallFileEntry> hashLookupMisses = new List<InstallFileEntry>();

            // Doing a reverse lookup on the manifest to find the index key for each file's content hash.  
            var archiveIndexDownloads = new List<InstallFileMatch>();
            var fileIndexDownloads = new List<InstallFileMatch>();

            var reverseLookupDictionary = encodingTable.EncodingDictionary.ToDictionary(e => e.Value, e => e.Key);

            foreach (var file in installFile.entries)
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

            Console.WriteLine($"     Starting {Colors.Cyan(requests.Count)} file downloads by byte range. " +
                              $"Totaling {Colors.Magenta(ByteSize.FromBytes(requests.Sum(e => e.TotalBytes)))}");
            
            //var progressBar = new ProgressBar(_console, PbStyle.SingleLine, requests.Count, 50);
            Parallel.ForEach(requests, new ParallelOptions { MaxDegreeOfParallelism = 5 }, indexDownload =>
            {
                cdn.GetByteRange($"{cdns.entries[0].path}/data/", indexDownload.Uri, indexDownload.LowerByteRange, indexDownload.UpperByteRange, true);
                //progressBar.Refresh(count, $"Downloading {rangeRequests.Count} file downloads by byte range");
            });
            Console.WriteLine($"     Complete! {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");
        }

        public void HandleDownloadFile(CDNConfigFile cdnConfig, CDN cdn, CdnsFile cdns, DownloadFile download, Dictionary<string, IndexEntry> archiveIndexDictionary)
        {
            Console.WriteLine("Parsing download file list...");
            var timer = Stopwatch.StartNew();

            var indexDownloads = 0;
            var totalBytes = 0L;
            foreach (var file in download.entries)
            {
                if (archiveIndexDictionary.ContainsKey(file.hash))
                {
                    IndexEntry e = archiveIndexDictionary[file.hash];
                    
                    // Need to subtract 1, since the byte range is "inclusive"
                    int upperByteRange = ((int)e.offset + (int)e.size - 1);
                    cdn.QueueRequest($"{cdns.entries[0].path}/data/", e.IndexId, e.offset, upperByteRange, writeToDevNull: true);

                    indexDownloads++;
                    totalBytes += (upperByteRange - e.offset);
                }
            }

            Console.WriteLine($"     Starting {Colors.Cyan(indexDownloads)} file downloads by byte range. " +
                              $"Totaling {Colors.Magenta(ByteSize.FromBytes(totalBytes))}");

            Console.WriteLine($"     Complete! {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");
        }
    }
}
