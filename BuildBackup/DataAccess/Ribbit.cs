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
                //.Where(e => e.tags.Contains("1=enUS"))
                .ToList();

            var reverseLookupDictionary = hashes.ToDictionary(e => e.Value, e => e.Key);

            //TODO lookup why these are missing
            List<InstallFileEntry> hashLookupMisses = new List<InstallFileEntry>();

            // Doing a reverse lookup on the manifest to find the index key for each file's content hash.  
            var indexDownloads = new List<IndexEntry>();
            foreach (var file in files)
            {
                //The manifest contains pairs of IndexId-ContentHash, reverse lookup for matches based on the ContentHash
                if (reverseLookupDictionary.ContainsKey(file.contentHashString.ToUpper()))
                {
                    var encodingTableHash = reverseLookupDictionary[file.contentHashString];

                    // If we found a match for the archive content, look into the archive index to see where the file can be downloaded from
                    if (archiveIndexDictionary.ContainsKey(encodingTableHash.ToUpper()))
                    {
                        var archiveIndex = archiveIndexDictionary[encodingTableHash.ToUpper()];
                        indexDownloads.Add(archiveIndex);
                    }
                    else if (fileIndexList.ContainsKey(encodingTableHash.ToUpper()))
                    {
                        var indexMatch = fileIndexList[encodingTableHash.ToUpper()];
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

            // Coalescing requests
            var initial = indexDownloads.Select(e => new RangeRequest()
            {
                archiveId = e.IndexId,
                start = (int)e.offset,
                // Need to subtract 1, since the byte range is "inclusive"
                end = ((int)e.offset + (int)e.size - 1)
            })
                // Deduplication
                // TODO make this look nicer
                .GroupBy(e => new
                {
                    e.archiveId,
                    e.start,
                    e.end
                })
                .Select(e => new RangeRequest()
                {
                    archiveId = e.Key.archiveId,
                    start = e.Key.start,
                    end = e.Key.end
                })
                .OrderBy(e => e.archiveId)
                .ThenBy(e => e.start).ToList();

            var coalesced = new List<RangeRequest>();
            var current = initial[0];
            initial.RemoveAt(0);

            while (initial.Any())
            {
                var matched = initial.FirstOrDefault(e => e.archiveId == current.archiveId && e.start == current.end);
                if (matched != null)
                {
                    //TODO this might be a bug?
                    current.end = matched.start;
                    initial.Remove(matched);
                }
                else
                {
                    coalesced.Add(current);
                    current = initial[0];
                    initial.RemoveAt(0);
                }
            }

            coalesced = coalesced.OrderBy(e => e.archiveId).ThenBy(e => e.start).ToList();

            Console.WriteLine($"     Done! {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");

            var size = ByteSize.FromBytes((double)coalesced.Sum(e => e.end - e.start)).MegaBytes;
            Console.WriteLine($"     Starting {Colors.Cyan(coalesced.Count)} file downloads by byte range. Totaling {Colors.Magenta(size.ToString("##.##"))}mb");

            //TODO reenable and fix
            //var progressBar = new ProgressBar(coalesced.Count, $"Downloading {coalesced.Count} file downloads by byte range");
            foreach (var indexDownload in coalesced)
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
