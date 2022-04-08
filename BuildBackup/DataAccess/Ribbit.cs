using BuildBackup.Structs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Colors = Shared.Colors;

namespace BuildBackup.DataAccess
{
    public class Ribbit
    {
        private CDN _cdn;

        public Ribbit(CDN cdn)
        {
            _cdn = cdn;
        }

        //TODO comment
        //TODO move to a different file, something like InstallFileHandler
        public void HandleInstallFile(EncodingTable encodingTable, Dictionary<MD5Hash, IndexEntry> archiveIndexDictionary)
        {
            Console.Write("Parsing install file list...".PadRight(Config.PadRight));
            var timer = Stopwatch.StartNew();

            // Doing a reverse lookup on the manifest to find the index key for each file's content hash.  
            var reverseLookupDictionary = encodingTable.EncodingDictionary.ToDictionary(e => e.Value, e => e.Key);

            InstallFile installFile = ParseInstallFile(encodingTable.installKey);

            //TODO make this more flexible/multi region
            var filtered = installFile.entries.Where(e => e.tags.Contains("1=enUS") && e.tags.Contains("2=Windows")).ToList();

            foreach (var file in filtered)
            {
                //The manifest contains pairs of IndexId-ContentHash, reverse lookup for matches based on the ContentHash
                if (!reverseLookupDictionary.ContainsKey(file.contentHash))
                {
                    continue;
                }
                
                // If we found a match for the archive content, look into the archive index to see where the file can be downloaded from
                MD5Hash upperHash = reverseLookupDictionary[file.contentHash];

                if (!archiveIndexDictionary.ContainsKey(upperHash))
                {
                    continue;
                }

                IndexEntry archiveIndex = archiveIndexDictionary[upperHash];
                    
                var lowerByteRange = (int)archiveIndex.offset;
                // Need to subtract 1, since the byte range is "inclusive"
                var upperByteRange = ((int)archiveIndex.offset + (int)archiveIndex.size - 1);
                _cdn.QueueRequest(RootFolder.data, archiveIndex.IndexId, lowerByteRange, upperByteRange, true);
            }
            Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}".PadLeft(Config.Padding));
        }

        public InstallFile ParseInstallFile(string hash)
        {
            var install = new InstallFile();

            byte[] content = _cdn.Get(RootFolder.data, hash);

            using (BinaryReader bin = new BinaryReader(new MemoryStream(BLTE.Parse(content))))
            {
                if (Encoding.UTF8.GetString(bin.ReadBytes(2)) != "IN")
                {
                    throw new Exception("Error while parsing install file. Did BLTE header size change?");
                }

                bin.ReadByte();

                install.hashSize = bin.ReadByte();
                if (install.hashSize != 16) throw new Exception("Unsupported install hash size!");

                install.numTags = bin.ReadUInt16(true);
                install.numEntries = bin.ReadUInt32(true);

                int bytesPerTag = ((int)install.numEntries + 7) / 8;

                install.tags = new InstallTagEntry[install.numTags];

                for (var i = 0; i < install.numTags; i++)
                {
                    install.tags[i].name = bin.ReadCString();
                    install.tags[i].type = bin.ReadUInt16(true);

                    var filebits = bin.ReadBytes(bytesPerTag);

                    for (int j = 0; j < bytesPerTag; j++)
                        filebits[j] = (byte)((filebits[j] * 0x0202020202 & 0x010884422010) % 1023);

                    install.tags[i].files = new BitArray(filebits);
                }

                install.entries = new InstallFileEntry[install.numEntries];

                for (var i = 0; i < install.numEntries; i++)
                {
                    install.entries[i].name = bin.ReadCString();
                    install.entries[i].contentHash = bin.Read<MD5Hash>();
                    install.entries[i].size = bin.ReadUInt32(true);
                    install.entries[i].tags = new List<string>();
                    for (var j = 0; j < install.numTags; j++)
                    {
                        if (install.tags[j].files[i] == true)
                        {
                            install.entries[i].tags.Add(install.tags[j].type + "=" + install.tags[j].name);
                        }
                    }
                }
            }

            return install;
        }

        //TODO move to a different file
        public void HandleDownloadFile(DownloadFile download, Dictionary<MD5Hash, IndexEntry> archiveIndexDictionary, CDNConfigFile cdnConfigFile)
        {
            Console.Write("Parsing download file list...".PadRight(Config.PadRight));
            var timer = Stopwatch.StartNew();

            Dictionary<string, IndexEntry> fileIndexList = IndexParser.ParseIndex(cdnConfigFile.fileIndex, _cdn, RootFolder.data);

            //TODO make this more flexible.  Perhaps pass in the region by name?
            var enUsTag = download.tags.Single(e => e.Name.Contains("enUS"));
            var tagToUse2 = download.tags.FirstOrDefault(e => e.Name.Contains("Windows"));
            var x86Tag = download.tags.FirstOrDefault(e => e.Name.Contains("x86"));
            var noIgrTag = download.tags.FirstOrDefault(e => e.Name.Contains("noigr"));

            for (var i = 0; i < download.entries.Length; i++)
            {
                DownloadEntry current = download.entries[i];

                // Filtering out files that shouldn't be downloaded by tag.  Ex. only want English audio files for a US install
                if (enUsTag.Bits[i] == false || tagToUse2?.Bits[i] == false || x86Tag?.Bits[i] == false || noIgrTag?.Bits[i] == false)
                {
                    continue;
                }
                if (!archiveIndexDictionary.ContainsKey(current.hash))
                {
                    // Handles downloading unarchived files unarchived files
                    var file = fileIndexList[current.hash.ToString()];
                    var startBytes2 = file.offset;
                    var endBytes2 = file.offset + file.size - 1;
                    
                    _cdn.QueueRequest(RootFolder.data, current.hash.ToString(), startBytes2, endBytes2, writeToDevNull: true);

                    continue;
                }
                
                IndexEntry e = archiveIndexDictionary[current.hash];
                uint blockSize = 1048576;

                uint offset = e.offset;
                uint size = e.size;
                uint blockStart = offset / blockSize;
                uint blockEnd = (offset + size + blockSize - 1) / blockSize;

                uint getStart = blockEnd;
                uint getEnd = blockStart;

                var mask = cdnConfigFile.archives[e.index].mask;

                //TODO fix this.  sometimes happens when running all unit tests.  Might not even be needed
                if (mask != null)
                {
                    for (int j = (int)blockStart; j < blockEnd; ++j)
                    {
                        var condition2 = (mask[j / 8] & (1 << (j & 7))) != 0;
                        if (j / 8 >= mask.Length || condition2)
                        {
                            getStart = (uint)Math.Max(getStart, i);
                            getEnd = (uint)Math.Max(getEnd, i + 1);
                        }
                    }
                }
                
                uint chunkSize = 4096;
                var startBytes = e.offset;

                if (startBytes == 71093275)
                {
                    //Debugger.Break();
                }

                // Need to subtract 1, since the byte range is "inclusive"
                uint numChunks = (e.offset + e.size - 1) / chunkSize;
                uint upperByteRange2 = ((numChunks + 1) * chunkSize) ;
                uint upperByteRange = (e.offset + e.size - 1) + 4096;
                _cdn.QueueRequest(RootFolder.data, e.IndexId, startBytes, upperByteRange, writeToDevNull: true);
            }

            Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}".PadLeft(Config.Padding));
        }
    }
}
