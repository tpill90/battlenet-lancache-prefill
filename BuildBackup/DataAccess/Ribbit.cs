using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ByteSizeLib;
using Konsole;
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
        public void DownloadIndexedFilesFromArchive(CDNConfigFile cdnConfig, Dictionary<string, string> hashes, InstallFile installFile, CDN cdn, CdnsFile cdns)
        {
            Console.WriteLine("Parsing download file list.  Doing reverse lookup...");
            var timer = Stopwatch.StartNew();


            Dictionary<string, IndexEntry> fileIndexList = IndexParser.ParseIndex(_cdns.entries[0].path, cdnConfig.fileIndex, _cdn);
            Dictionary<string, IndexEntry> archiveIndexDictionary = IndexParser.BuildArchiveIndexes(_cdns.entries[0].path, cdnConfig, _cdn);

            List<InstallFileEntry> files = installFile.entries
                .Where(e => e.tags.Contains("1=enUS"))
                .ToList();

            var reverseLookupDictionary = hashes.ToDictionary(e => e.Value, e => e.Key);

            //TODO lookup why these are missing
            List<InstallFileEntry> hashLookupMisses = new List<InstallFileEntry>();

            // Doing a reverse lookup on the manifest to find the index key for each file's content hash.  
            var indexDownloads = new List<InstallFileMatch>();
            var indexDownloads2 = new List<InstallFileMatch>();
            foreach (var file in files)
            {
                //The manifest contains pairs of IndexId-ContentHash, reverse lookup for matches based on the ContentHash
                if (reverseLookupDictionary.ContainsKey(file.contentHashString.ToUpper()))
                {
                    var encodingTableHash = reverseLookupDictionary[file.contentHashString];

                    // If we found a match for the archive content, look into the archive index to see where the file can be downloaded from
                    if (archiveIndexDictionary.ContainsKey(encodingTableHash.ToUpper()))
                    {
                        IndexEntry archiveIndex = archiveIndexDictionary[encodingTableHash.ToUpper()];
                        indexDownloads.Add(new InstallFileMatch() { IndexEntry = archiveIndex, InstallFileEntry = file });
                    }
                    if (fileIndexList.ContainsKey(encodingTableHash.ToUpper()))
                    {
                        IndexEntry indexMatch = fileIndexList[encodingTableHash.ToUpper()];
                        indexDownloads2.Add(new InstallFileMatch() { IndexEntry = indexMatch, InstallFileEntry = file });
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

            // Creating requests
            var rangeRequests = indexDownloads.Select(e => new RangeRequest()
            {
                archiveId = e.IndexEntry.IndexId,
                start = (int) e.IndexEntry.offset,
                // Need to subtract 1, since the byte range is "inclusive"
                end = ((int) e.IndexEntry.offset + (int) e.IndexEntry.size - 1)
            }).ToList();

            Console.WriteLine($"     Done! {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");

            var size = ByteSize.FromBytes((double)rangeRequests.Sum(e => e.end - e.start)).MegaBytes;
            Console.WriteLine($"     Starting {Colors.Cyan(rangeRequests.Count)} file downloads by byte range. Totaling {Colors.Magenta(size.ToString("##.##"))}mb");

            //TODO reenable and fix
            //var progressBar = new ProgressBar(coalesced.Count, $"Downloading {coalesced.Count} file downloads by byte range");
            foreach (var indexDownload in rangeRequests)
            {
                //TODO this wasn't necessarily working correctly before.  Was accidentially having it download the entire file.
                //TODO renable + have it write to dev-null
                cdn.GetByteRange($"{cdns.entries[0].path}/data/", indexDownload.archiveId, indexDownload.start, indexDownload.end, true);
                //progressBar.Tick();
            }
            //progressBar.Message = "Done!";
            //progressBar.Dispose();
        }
    }
}
