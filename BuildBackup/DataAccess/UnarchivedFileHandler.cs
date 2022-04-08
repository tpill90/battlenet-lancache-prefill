using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BuildBackup.Structs;
using Konsole;
using Newtonsoft.Json;
using Shared;
using Colors = Shared.Colors;

namespace BuildBackup.DataAccess
{
    public class UnarchivedFileHandler
    {
        private CDN _cdn;
        private CdnsFile _cdns;
        private readonly IConsole _console;

        public UnarchivedFileHandler(CDN cdn, CdnsFile cdns, IConsole console)
        {
            _cdn = cdn;

            Debug.Assert(cdns.entries != null, "Cdns must be initialized before using");
            _cdns = cdns;
            _console = console;
        }

        //TODO comment
        public void DownloadUnarchivedFiles(CDNConfigFile cdnConfig, EncodingTable encodingTable, TactProduct product)
        {
            Console.Write("Processing individual, unarchived files ... ".PadRight(Config.PadRight));

            var timer = Stopwatch.StartNew();
            Dictionary<string, IndexEntry> archiveIndexDictionary = IndexParser.BuildArchiveIndexes(_cdns.entries[0].path, cdnConfig, _cdn, product, new Uri("http://level3.blizzard.com"));
            Dictionary<string, IndexEntry> fileIndexList = IndexParser.ParseIndex(_cdns.entries[0].path, cdnConfig.fileIndex, _cdn, "data");

            foreach (var indexEntry in archiveIndexDictionary)
            {
                encodingTable.EncodingDictionary.Remove(indexEntry.Key.FromHexString().ToMD5());
            }
            
            foreach (var entry in encodingTable.EncodingDictionary)
            {
                if (fileIndexList.ContainsKey(entry.Key.ToString()))
                {
                    var file = fileIndexList[entry.Key.ToString()];
                    var startBytes = file.offset;
                    var endBytes = file.offset + file.size - 1;
                    _cdn.QueueRequest($"{_cdns.entries[0].path}/data/", entry.Key.ToString(), startBytes, endBytes, writeToDevNull: true);
                }
            }

            timer.Stop();
            Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}".PadLeft(Config.Padding));
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
