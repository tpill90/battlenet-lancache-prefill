using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BuildBackup.Structs;
using ByteSizeLib;
using Konsole;
using Newtonsoft.Json;
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

        //TODO make this private, and make DownloadIndexedFilesFromArchive() call it instead
        public (DownloadFile, InstallFile) ProcessRibbit(string rootKey, Logic logic, string downloadKey, string installKey)
        {
            DownloadFile download = new DownloadFile();
            InstallFile install = new InstallFile();

            Console.Write("Processing Ribbit... ");

            var timer = Stopwatch.StartNew();
            if (rootKey == "")
            {
                Console.WriteLine("Unable to find root key in encoding!");
            }
            else
            {
                //TODO why is this commented out?
                //root = logic.GetRoot(cdns.entries[0].path + "/", rootKey);
            }

            if (downloadKey == "")
            {
                Console.WriteLine("Unable to find download key in encoding!");
            }
            else
            {
                download = logic.GetDownload(_cdns.entries[0].path, downloadKey, parseIt: true);
            }
            if (installKey == "")
            {
                Console.WriteLine("Unable to find install key in encoding!");
            }
            else
            {
                install = logic.GetInstall(_cdns.entries[0].path, installKey);
            }

            Console.WriteLine($"Done! {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");
            return (download, install);
        }

        //TODO comment
        //TODO hashes needs a better name
        public void DownloadIndexedFilesFromArchive(CDNConfigFile cdnConfig, EncodingTable encodingTable, InstallFile installFile, 
            CDN cdn, CdnsFile cdns, DownloadFile download)
        {
            var rootFolder = @"C:\Users\Tim\Desktop\temp";
            Console.WriteLine("Parsing download file list.  Doing reverse lookup...");
            var timer = Stopwatch.StartNew();

            Dictionary<string, IndexEntry> fileIndexList = IndexParser.ParseIndex(_cdns.entries[0].path, cdnConfig.fileIndex, _cdn, "data");
            Dictionary<string, IndexEntry> archiveIndexDictionary = IndexParser.BuildArchiveIndexes(_cdns.entries[0].path, cdnConfig, _cdn);

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
            foreach (var file in download.entries)
            {
                if (encodingTable.EncodingDictionary.ContainsKey(file.hash.ToUpper()))
                {
                    var upperHash = encodingTable.EncodingDictionary[file.hash.ToUpper()].ToUpper();
                    if (archiveIndexDictionary.ContainsKey(upperHash))
                    {
                        Debugger.Break();
                        IndexEntry archiveIndex = archiveIndexDictionary[upperHash];
                        //archiveIndexDownloads.Add(new InstallFileMatch { IndexEntry = archiveIndex, InstallFileEntry = file });
                    }
                    
                }
                if (archiveIndexDictionary.ContainsKey(file.hash.ToUpper()))
                {
                    //Debugger.Break();
                    IndexEntry archiveIndex = archiveIndexDictionary[file.hash.ToUpper()];
                    archiveIndexDownloads.Add(new InstallFileMatch { IndexEntry = archiveIndex });
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


            Console.WriteLine($"     Done! {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");
            Console.WriteLine($"     Starting {Colors.Cyan(requests.Count)} file downloads by byte range. " +
                              $"Totaling {Colors.Magenta(ByteSize.FromBytes(requests.Sum(e => e.TotalBytes)))}");

            int count = 0;
            var progressBar = new ProgressBar(_console, PbStyle.SingleLine, requests.Count, 50);
            Parallel.ForEach(requests, new ParallelOptions { MaxDegreeOfParallelism = 5 }, indexDownload =>
            {
                cdn.GetByteRange($"{cdns.entries[0].path}/data/", indexDownload.Uri, indexDownload.LowerByteRange, indexDownload.UpperByteRange, true);
                //progressBar.Refresh(count, $"Downloading {rangeRequests.Count} file downloads by byte range");
                count++;
            });
            Console.WriteLine($"    Complete! {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");
        }
    }
}
