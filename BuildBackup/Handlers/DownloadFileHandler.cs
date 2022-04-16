using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BuildBackup.DataAccess;
using BuildBackup.Structs;
using CASCLib;
using Shared;

namespace BuildBackup.Handlers
{
    //TODO document
    public class DownloadFileHandler
    {
        private readonly CDN _cdn;

        private DownloadFile _downloadFile;

        public DownloadFileHandler(CDN cdn)
        {
            _cdn = cdn;
        }

        //TODO document
        public void ParseDownloadFile(BuildConfigFile buildConfig)
        {
            var timer = Stopwatch.StartNew();

            _downloadFile = new DownloadFile();

            var hash = buildConfig.download[1].ToString().ToLower();

            byte[] content = _cdn.Get(RootFolder.data, hash);

            using (var memoryStream = new MemoryStream(content))
            using (var blteStream = new BLTEStream(memoryStream, buildConfig.download[1]))
            using (BinaryReader bin = new BinaryReader(blteStream))
            {
                if (Encoding.UTF8.GetString(bin.ReadBytes(2)) != "DL")
                {
                    throw new Exception("Error while parsing download file. Did BLTE header size change?");
                }
                byte version = bin.ReadByte();
                byte hash_size_ekey = bin.ReadByte();
                byte has_checksum_in_entry = bin.ReadByte();
                _downloadFile.numEntries = bin.ReadUInt32(true);
                _downloadFile.numTags = bin.ReadUInt16(true);

                int entryExtra = 6;
                if (has_checksum_in_entry > 0)
                {
                    entryExtra += 4;
                }
                if (version >= 2)
                {
                    byte number_of_flag_bytes = bin.ReadByte();
                    entryExtra += number_of_flag_bytes;
                    if (version >= 3)
                    {
                        bin.BaseStream.Seek(16, SeekOrigin.Begin);
                    }
                }

                // Reading the DownloadFile entries
                _downloadFile.entries = new DownloadEntry[_downloadFile.numEntries];
                for (int i = 0; i < _downloadFile.numEntries; i++)
                {
                    _downloadFile.entries[i].hash = bin.Read<MD5Hash>();
                    bin.BaseStream.Position += entryExtra;
                }

                // Reading the tags
                int numMaskBytes = (int)((_downloadFile.numEntries + 7) / 8);
                _downloadFile.tags = new DownloadTag[_downloadFile.numTags];

                for (int i = 0; i < _downloadFile.numTags; i++)
                {
                    DownloadTag tag = new DownloadTag();
                    tag.Name = bin.ReadCString();
                    tag.Type = bin.ReadInt16BE();

                    tag.Mask = bin.ReadBytes(numMaskBytes);

                    for (int j = 0; j < numMaskBytes; j++)
                    {
                        var bit = tag.Mask[j];
                        var result = (bit * 0x0202020202 & 0x010884422010) % 1023;
                        tag.Mask[j] = (byte)result;
                    }

                    _downloadFile.tags[i] = tag;
                }
            }

            Console.Write("Parsed download file...".PadRight(Config.PadRight));
            Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}".PadLeft(Config.Padding));
        }
        
        //TODO document method
        public void HandleDownloadFile(ArchiveIndexHandler archiveIndexHandler, CDNConfigFile cdnConfigFile, TactProduct targetProduct)
        {
            Console.Write("Handling download file list...".PadRight(Config.PadRight));
            var timer = Stopwatch.StartNew();

            Dictionary<string, IndexEntry> fileIndexList = IndexParser.ParseIndex(cdnConfigFile.fileIndex, _cdn, RootFolder.data);

            //TODO make this more flexible/multi region.  Should probably be passed in/ validated per product.
            //TODO do a check to make sure that the tags being used are actually valid for the product
            List<DownloadTag> tagsToUse = new List<DownloadTag>();
            if (targetProduct.DefaultTags != null)
            {
                foreach (var tag in targetProduct.DefaultTags)
                {
                    tagsToUse.Add(_downloadFile.tags.FirstOrDefault(e => e.Name.Contains(tag)));
                }
            }
            else
            {
                tagsToUse = _downloadFile.tags.Where(e => e.Name.Contains("enUS") ||
                                                          e.Name.Contains("Windows") ||
                                                          e.Name.Contains("x86") ||
                                                          e.Name.Contains("noigr")).ToList();
            }

            var computedMask = BuildDownloadMask(tagsToUse);
            for (var i = 0; i < _downloadFile.entries.Length; i++)
            {
                DownloadEntry current = _downloadFile.entries[i];
                
                //Filtering out files that shouldn't be downloaded by tag.  Ex. only want English audio files for a US install
                //TODO document how this works
                if ((computedMask.Mask[i/8]  & (1 << (i % 8))) == 0)
                {
                    continue;
                }

                IndexEntry? archiveIndex = archiveIndexHandler.TryGet(current.hash);
                if (archiveIndex == null)
                {
                    if (fileIndexList.ContainsKey(current.hash.ToString()))
                    {
                        // Handles downloading unarchived files unarchived files
                        //TODO get rid of .ToString
                        var file = fileIndexList[current.hash.ToString()];

                        var endBytes2 = file.offset + file.size - 1;
                        _cdn.QueueRequest(RootFolder.data, current.hash, file.offset, endBytes2);
                    }
                    continue;
                }

                IndexEntry e = archiveIndex.Value;
                var startBytes = e.offset;
                // Need to subtract 1, since the byte range is "inclusive"
                uint upperByteRange = (e.offset + e.size - 1);
                string archiveIndexKey = cdnConfigFile.archives[e.index].hashId;
                _cdn.QueueRequest(RootFolder.data, archiveIndexKey, startBytes, upperByteRange);
            }

            Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}".PadLeft(Config.Padding));
        }

        //TODO document how this works
        private static DownloadTag BuildDownloadMask(List<DownloadTag> tagsToUse)
        {
            // Need to first pre-process groups of similar tags.  Must be combined using logical OR to determine all files that might be installed.
            // Games like Call of Duty use these tags to determine which features to install (Campaign, Multiplayer, Zombies, etc.)
            var groupedTags = tagsToUse.GroupBy(e => e.Type).Where(e => e.Count() > 1).ToList();
            foreach (var group in groupedTags)
            {
                var groupedList = group.ToList();
                var combinedMask = groupedList.First();

                for (int tagIndex = 1; tagIndex < groupedList.Count; tagIndex++)
                {
                    var current = groupedList[tagIndex];
                    for (int i = 0; i < combinedMask.Mask.Length; i++)
                    {
                        combinedMask.Mask[i] |= current.Mask[i];
                    }

                    tagsToUse.Remove(current);
                }
            }

            // Compute the final mask, which will be used to determine which files to download
            var computedMask = tagsToUse.First();
            tagsToUse.RemoveAt(0);
            
            foreach (var tag in tagsToUse)
            {
                for (int i = 0; i < computedMask.Mask.Length; i++)
                {
                    computedMask.Mask[i] &= tag.Mask[i];
                }
            }

            return computedMask;
        }
    }
}