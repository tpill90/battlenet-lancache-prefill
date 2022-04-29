using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleNetPrefill.EncryptDecrypt;
using BattleNetPrefill.Parsers;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Utils;
using BattleNetPrefill.Web;

namespace BattleNetPrefill.Handlers
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
        public async Task ParseDownloadFileAsync(BuildConfigFile buildConfig)
        {
            _downloadFile = new DownloadFile();

            var content = await _cdn.GetRequestAsBytesAsync(RootFolder.data, buildConfig.download[1]);

            using var memoryStream = new MemoryStream(BLTE.Parse(content));
            using BinaryReader bin = new BinaryReader(memoryStream);
            
            // Reading header
            if (Encoding.UTF8.GetString(bin.ReadBytes(2)) != "DL")
            {
                throw new Exception("Error while parsing download file. Did BLTE header size change?");
            }
            byte version = bin.ReadByte();
            byte hash_size_ekey = bin.ReadByte();
            byte hasChecksumInEntry = bin.ReadByte();
            _downloadFile.numEntries = bin.ReadUInt32(true);
            _downloadFile.entries = new DownloadEntry[_downloadFile.numEntries];

            _downloadFile.numTags = bin.ReadUInt16(true);
            _downloadFile.tags = new DownloadTag[_downloadFile.numTags];

            int entryExtra = 6;
            if (hasChecksumInEntry > 0)
            {
                entryExtra += 4;
            }
            if (version >= 2)
            {
                entryExtra += bin.ReadByte();
                if (version >= 3)
                {
                    bin.BaseStream.Seek(16, SeekOrigin.Begin);
                }
            }

            // Reading the DownloadFile entries
            for (int i = 0; i < _downloadFile.numEntries; i++)
            {
                _downloadFile.entries[i].hash = bin.Read<MD5Hash>();
                // Skips data that we are not interested in reading, to get to the next entry
                bin.BaseStream.Position += entryExtra;
            }

            // Reading the tags
            int numMaskBytes = (int)((_downloadFile.numEntries + 7) / 8);
            for (int i = 0; i < _downloadFile.numTags; i++)
            {
                DownloadTag tag = new DownloadTag();
                tag.Name = bin.ReadCString();
                tag.Type = bin.ReadInt16BE();

                tag.Mask = bin.ReadBytes(numMaskBytes);

                for (int j = 0; j < numMaskBytes; j++)
                {
                    var original = tag.Mask[j];
                    var result = (original * 0x0202020202 & 0x010884422010) % 1023;
                    tag.Mask[j] = (byte)result;
                }

                _downloadFile.tags[i] = tag;
            }
        }
        
        //TODO document method
        public async Task HandleDownloadFileAsync(ArchiveIndexHandler archiveIndexHandler, CDNConfigFile cdnConfigFile, TactProduct targetProduct)
        {
            Dictionary<MD5Hash, IndexEntry> unarchivedFileIndex = await IndexParser.ParseIndexAsync(_cdn, RootFolder.data, cdnConfigFile.fileIndex);

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
                // If a file is not found in the archive index, then there is a possibility that it is an "unarchived" file.
                if (archiveIndex == null)
                {
                    // Handles downloading individual unarchived files if they are found in the unarchived file index
                    if (unarchivedFileIndex.ContainsKey(current.hash))
                    {
                        IndexEntry file = unarchivedFileIndex[current.hash];
                        //TODO i might be able to remove the weird 4096 byte coalescing logic now that this is here
                        var endBytes = Math.Max(file.offset + file.size - 1, file.offset + 4095);

                        _cdn.QueueRequest(RootFolder.data, current.hash, file.offset, endBytes);
                    }
                    continue;
                }

                IndexEntry e = archiveIndex.Value;

                MD5Hash archiveIndexKey = cdnConfigFile.archives[e.index].hashIdMd5;
                var startBytes = e.offset;
                uint upperByteRange = (e.offset + e.size - 1);
                _cdn.QueueRequest(RootFolder.data, archiveIndexKey, startBytes, upperByteRange);
            }
        }

        //TODO document how this works + test
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