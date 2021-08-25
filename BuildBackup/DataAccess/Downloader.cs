using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ByteSizeLib;
using Konsole;
using MoreLinq.Extensions;
using Colors = Shared.Colors;

namespace BuildBackup.DataAccess
{
    public class Downloader
    {
        private CDN _cdn;
        private CdnsFile _cdns;
        private readonly IConsole _console;

        public Downloader(CDN cdn, CdnsFile cdns, IConsole console)
        {
            _cdn = cdn;

            Debug.Assert(cdns.entries != null, "Cdns must be initialized before using");
            _cdns = cdns;
            _console = console;
        }

        //TODO comment
        public void DownloadFiles(CDNConfigFile cdnConfig, Dictionary<string, IndexEntry> fileIndexList)
        {
            if (!string.IsNullOrEmpty(cdnConfig.fileIndex))
            {
                Console.WriteLine($"Downloading {Colors.Cyan(fileIndexList.Count)} unarchived files from file index..");

                // Grouping download operations into batches, to help with memory usage
                var fileKeyBatches = fileIndexList.Keys.Batch(75).ToList();

                int current = 1;
                foreach (var keys in fileKeyBatches)
                {
                    var timer = Stopwatch.StartNew();

                    Parallel.ForEach(keys, (fileId) =>
                    {
                        _cdn.Get($"{_cdns.entries[0].path}/data/", fileId);
                    });

                    current++;
                    timer.Stop();
                    Console.WriteLine($"Processed batch {Colors.Cyan(current)} of {fileKeyBatches.Count + 1} in {Colors.Yellow(timer.Elapsed)}");
                }

                Console.WriteLine("..done\n");
            }
        }

        /// <summary>
        /// Downloads all of the currently listed "archive" files from Battle.Net's CDN.  Each archive file is 256mb.
        ///
        /// More details on archive files : https://wowdev.wiki/TACT#Archives
        /// </summary>
        public void DownloadFullArchives(CDNConfigFile cdnConfig)
        {
            Console.WriteLine("Downloading full archive files....");

            //MeasureIndexSize(cdnConfig);

            var progressBar = new ProgressBar(_console, PbStyle.SingleLine, cdnConfig.archives.Length);
            int count = 0;
            foreach (var entry in cdnConfig.archives)
            {
                _cdn.Get(_cdns.entries[0].path + "/data/", entry, writeToDevNull: true);
                progressBar.Refresh(count, $"     {_cdns.entries[0].path}/data/{entry}");
                count++;
            }
            progressBar.Refresh(count, "     Done!");
        }

        private void MeasureIndexSize(CDNConfigFile cdnConfig)
        {
            // Downloading Archive indexes + parsing them to get the estimated download size
            Console.WriteLine("     Retrieving indexes...");
            var timer = Stopwatch.StartNew();
            var indexDictionary = IndexParser.BuildArchiveIndexes(_cdns.entries[0].path, cdnConfig, _cdn);
            timer.Stop();
            Console.WriteLine("     Indexes loaded in : " + Colors.Cyan(timer.Elapsed.ToString()));

            double sizeCalculatedFromIndex = ByteSize.FromBytes((double) indexDictionary.Sum(e => e.Value.size)).GigaBytes;
            double maxArchiveSize = (double) cdnConfig.archives.Length / 4;
            // Battle.Net is inaccurate with its reported size, taking the smaller of the two as its the most likely number.
            var bestGuessSize = Math.Min(sizeCalculatedFromIndex, maxArchiveSize);
            Console.WriteLine($"     Downloading {Colors.Cyan(cdnConfig.archives.Count())} full archives.  Totaling {Colors.Magenta(bestGuessSize.ToString("##.##"))}gb");
        }

        public void DownloadFilesFromIndex(CDNConfigFile cdnConfig)
        {
            if (string.IsNullOrEmpty(cdnConfig.fileIndex))
            {
                return;
            }
            Console.WriteLine("Parsing file index..");
            var fileIndexList = IndexParser.ParseIndex(_cdns.entries[0].path, cdnConfig.fileIndex, _cdn);

            var size = ByteSize.FromBytes((double)fileIndexList.Sum(e => (decimal)e.Value.size));
            Console.WriteLine($"     Total file size : {Colors.Yellow(size.GigaBytes.ToString("##.##"))}gb");
            DownloadFiles(cdnConfig, fileIndexList);
        }

        //TODO comment
        public void DownloadUnarchivedFiles(CDNConfigFile cdnConfig, Dictionary<string, string> hashes)
        {
            // Actually downloading the files
            Console.WriteLine("Processing unarchived files");
            var archiveIndexDictionary = IndexParser.BuildArchiveIndexes(_cdns.entries[0].path, cdnConfig, _cdn);

            foreach (var indexEntry in archiveIndexDictionary)
            {
                hashes.Remove(indexEntry.Key.ToUpper());
            }

            Console.WriteLine($"     Downloading {Colors.Cyan(hashes.Count())} unarchived files..");
            var progressBar = new ProgressBar(_console, PbStyle.SingleLine, cdnConfig.archives.Length, 50);
            int count = 0;
            foreach (var entry in hashes)
            {
                _cdn.Get($"{_cdns.entries[0].path}/data/", entry.Key, writeToDevNull: true);
                //TODO reenable this, slows down performance
                //progressBar.Refresh(count, $"     {_cdns.entries[0].path}/data/{entry}");
                count++;
            }
            progressBar.Refresh(count, "     Done!");
        }
    }
}
