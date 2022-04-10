using System;
using System.Collections.Generic;
using System.Diagnostics;
using BuildBackup.Structs;
using Konsole;
using Colors = Shared.Colors;

namespace BuildBackup.DataAccess
{
    public class UnarchivedFileHandler
    {
        private CDN _cdn;
        private readonly IConsole _console;

        public UnarchivedFileHandler(CDN cdn, IConsole console)
        {
            _cdn = cdn;
            _console = console;
        }

        //TODO comment
        public void DownloadUnarchivedFiles(CDNConfigFile cdnConfig, EncodingTable encodingTable, Dictionary<MD5Hash, IndexEntry> archiveIndexDictionary)
        {
            Console.Write("Processing individual, unarchived files ... ".PadRight(Config.PadRight));

            var timer = Stopwatch.StartNew();
            Dictionary<string, IndexEntry> fileIndexList = IndexParser.ParseIndex(cdnConfig.fileIndex, _cdn, RootFolder.data);

            foreach (var indexEntry in archiveIndexDictionary)
            {
                encodingTable.EncodingDictionary.Remove(indexEntry.Key);
            }
            
            foreach (var entry in encodingTable.EncodingDictionary)
            {
                if (fileIndexList.ContainsKey(entry.Key.ToString()))
                {
                    var file = fileIndexList[entry.Key.ToString()];
                    var startBytes = file.offset;
                    var endBytes = file.offset + file.size - 1;
                    _cdn.QueueRequest(RootFolder.data, entry.Key.ToString(), startBytes, endBytes, writeToDevNull: true);
                }
            }

            timer.Stop();
            Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}".PadLeft(Config.Padding));
        }

        public void DownloadUnarchivedIndexFiles(CDNConfigFile cdnConfig, DownloadFile downloadFile, EncodingTable encodingTable)
        {
            Console.Write($"Processing unarchived files from file index..");
            var timer = Stopwatch.StartNew();

            Dictionary<string, IndexEntry> fileIndexList = IndexParser.ParseIndex(cdnConfig.fileIndex, _cdn, RootFolder.data);

            foreach (var download in downloadFile.entries)
            {
                if (fileIndexList.ContainsKey(download.hash.ToString()))
                {
                    Debugger.Break();
                }
            }

            foreach(var entry in fileIndexList)
            {
                var file = fileIndexList[entry.Key];
                var startBytes = file.offset;
                var endBytes = file.offset + file.size - 1;
                _cdn.QueueRequest(RootFolder.data, entry.Key, startBytes, endBytes, writeToDevNull: true);
            }

            timer.Stop();
            Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}".PadLeft(Config.Padding));
        }
    }
}
