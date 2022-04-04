using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BuildBackup.Structs;
using Konsole;
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

        /// <summary>
        /// Downloads all of the currently listed "archive" files from Battle.Net's CDN.  Each archive file is 256mb.
        ///
        /// More details on archive files : https://wowdev.wiki/TACT#Archives
        /// </summary>
        public void DownloadFullArchives(CDNConfigFile cdnConfig)
        {
            Console.WriteLine("Downloading full archive files....");

            var progressBar = new ProgressBar(_console, PbStyle.SingleLine, cdnConfig.archives.Length);
            int count = 0;
            var timer = Stopwatch.StartNew();
           
            Parallel.ForEach(cdnConfig.archives, new ParallelOptions { MaxDegreeOfParallelism = 10 }, (entry) =>
            {
                _cdn.Get($"{_cdns.entries[0].path}/data/", entry, writeToDevNull: true);
                progressBar.Refresh(count, $"     {_cdns.entries[0].path}/data/{entry}");
                count++;
            });
            timer.Stop();
            progressBar.Refresh(count, $"     Done! {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");
        }

        //TODO comment
        public void DownloadUnarchivedFiles(CDNConfigFile cdnConfig, EncodingTable encodingTable)
        {
            // Actually downloading the files
            Console.WriteLine("Processing unarchived files");
            var archiveIndexDictionary = IndexParser.BuildArchiveIndexes(_cdns.entries[0].path, cdnConfig, _cdn);

            foreach (var indexEntry in archiveIndexDictionary)
            {
                encodingTable.EncodingDictionary.Remove(indexEntry.Key.ToUpper());
            }

            Console.WriteLine($"     Downloading {Colors.Cyan(encodingTable.EncodingDictionary.Count())} unarchived files..");

            int count = 0;
            var timer = Stopwatch.StartNew();
            var progressBar = new ProgressBar(_console, PbStyle.SingleLine, encodingTable.EncodingDictionary.Count, 50);
            Parallel.ForEach(encodingTable.EncodingDictionary, new ParallelOptions { MaxDegreeOfParallelism = 20 }, entry =>
            {
                _cdn.Get($"{_cdns.entries[0].path}/data/", entry.Key, writeToDevNull: true);
                //progressBar.Refresh(count, $"     {_cdns.entries[0].path}/data/{entry}");
                count++;
            });

            timer.Stop();
            progressBar.Refresh(count, $"     Done! {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");
        }

        public void DownloadUnarchivedIndexFiles(CDNConfigFile cdnConfig)
        {
            Dictionary<string, IndexEntry> fileIndexList = IndexParser.ParseIndex(_cdns.entries[0].path, cdnConfig.fileIndex, _cdn, "data");

            Console.WriteLine($"     Downloading {Colors.Cyan(fileIndexList.Count())} unarchived files from file index..");

            int count = 0;
            var timer = Stopwatch.StartNew();
            var progressBar = new ProgressBar(_console, PbStyle.SingleLine, fileIndexList.Count, 50);
            Parallel.ForEach(fileIndexList, new ParallelOptions { MaxDegreeOfParallelism = 20 }, entry =>
            {
                _cdn.Get($"{_cdns.entries[0].path}/data/", entry.Key, writeToDevNull: true);
                //progressBar.Refresh(count, $"     {_cdns.entries[0].path}/data/{entry}");
                count++;
            });

            timer.Stop();
            progressBar.Refresh(count, $"     Done! {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");
        }
    }
}
