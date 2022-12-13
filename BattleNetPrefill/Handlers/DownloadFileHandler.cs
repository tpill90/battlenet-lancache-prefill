namespace BattleNetPrefill.Handlers
{
    /// <summary>
    /// Handles the "DownloadFile", which is in essence a "download manifest" listing all files that will need to be downloaded, for a complete installation.
    /// 
    /// Handles parsing the raw "DownloadFile".  Additionally, handles determining which files that need to be downloaded based on the download manifest.
    ///
    /// See also:
    /// https://wowdev.wiki/TACT#Download_manifest
    /// https://github.com/d07RiV/blizzget/wiki/Download-file
    /// </summary>
    public class DownloadFileHandler
    {
        private readonly CdnRequestManager _cdnRequestManager;
        private DownloadFile _downloadFile;

        public DownloadFileHandler(CdnRequestManager cdnRequestManager)
        {
            _cdnRequestManager = cdnRequestManager;
        }

        /// <summary>
        /// Downloads the "DownloadFile", and parses the raw data into a useable format.
        /// Must be called prior to using <see cref="HandleDownloadFileAsync"/>
        /// </summary>
        public async Task ParseDownloadFileAsync(BuildConfigFile buildConfig)
        {
            _downloadFile = new DownloadFile();

            var content = await _cdnRequestManager.GetRequestAsBytesAsync(RootFolder.data, buildConfig.download[1]);

            using var memoryStream = BLTE.Parse(content);
            using BinaryReader bin = new BinaryReader(memoryStream);


            var unusedBytesToSkip = ReadHeader(bin);

            // Reading the DownloadFile entries
            _downloadFile.entries = new DownloadEntry[_downloadFile.numEntries];
            byte[] md5Buffer = new byte[Unsafe.SizeOf<MD5Hash>()];
            for (int i = 0; i < _downloadFile.numEntries; i++)
            {
                _downloadFile.entries[i].hash = bin.ReadMd5Hash(md5Buffer);
                // Skips data that we are not interested in reading, to get to the next entry
                bin.BaseStream.Position += unusedBytesToSkip;
            }

            // Reading the tags. There will be 1 bit per entry, to determine if a file should be downloaded.  Packing these individual bit into a byte
            int numMaskBytes = (int)((_downloadFile.numEntries + 7) / 8);

            _downloadFile.tags = new DownloadTag[_downloadFile.numTags];
            for (int i = 0; i < _downloadFile.numTags; i++)
            {
                DownloadTag tag = new DownloadTag();
                tag.Name = bin.ReadCString();
                tag.Type = bin.ReadInt16BigEndian();

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

        private int ReadHeader(BinaryReader bin)
        {
            if (Encoding.UTF8.GetString(bin.ReadBytes(2)) != "DL")
            {
                throw new Exception("Error while parsing download file. Did BLTE header size change?");
            }

            byte version = bin.ReadByte();
            byte hash_size_ekey = bin.ReadByte();
            byte hasChecksumInEntry = bin.ReadByte();
            _downloadFile.numEntries = bin.ReadUInt32BigEndian();
            _downloadFile.numTags = bin.ReadUInt16BigEndian();

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

            return entryExtra;
        }

        /// <summary>
        /// Determines which files need to be downloaded, and queues them for download.
        /// </summary>
        public async Task HandleDownloadFileAsync(ArchiveIndexHandler archiveIndexHandler, CDNConfigFile cdnConfigFile, TactProduct targetProduct)
        {
            Dictionary<MD5Hash, IndexEntry> unarchivedFileIndex = await IndexParser.ParseIndexAsync(_cdnRequestManager, RootFolder.data, cdnConfigFile.fileIndex);

            var tagsToUse = DetermineTagsToUse(targetProduct);

            var computedMask = BuildDownloadMask(tagsToUse);
            for (var i = 0; i < _downloadFile.entries.Length; i++)
            {
                DownloadEntry current = _downloadFile.entries[i];

                //Filtering out files that shouldn't be downloaded by tag.  Ex. only want English audio files for a US install
                if (!computedMask.FileShouldBeDownloaded(i))
                {
                    continue;
                }

                ArchiveIndexEntry? archiveIndex = archiveIndexHandler.ArchivesContainKey(current.hash);
                // If a file is not found in the archive index, then there is a possibility that it is an "unarchived" file.
                if (archiveIndex == null)
                {
                    // Handles downloading individual unarchived files if they are found in the unarchived file index
                    if (unarchivedFileIndex.ContainsKey(current.hash))
                    {
                        IndexEntry file = unarchivedFileIndex[current.hash];

                        uint endBytes = file.offset + file.size - 1;
                        if (targetProduct == TactProduct.Warcraft3Reforged)
                        {
                            //TODO i might be able to remove the weird 4096 byte coalescing logic now that this is here
                            // Only Warcraft 3 does this weird logic, not entirely sure why no other products do it.
                            endBytes = Math.Max(endBytes, file.offset + 4095);
                        }
                        _cdnRequestManager.QueueRequest(RootFolder.data, current.hash, file.offset, endBytes);
                    }
                    continue;
                }

                ArchiveIndexEntry e = archiveIndex.Value;

                MD5Hash archiveIndexKey = cdnConfigFile.archives[e.index].hashIdMd5;
                var startBytes = e.offset;
                uint upperByteRange = (e.offset + e.size - 1);
                _cdnRequestManager.QueueRequest(RootFolder.data, archiveIndexKey, startBytes, upperByteRange);
            }
        }

        /// <summary>
        /// Based upon the specified product, determines which download tags should be used.
        /// The download tags determine which features should be downloaded, ex. Single Player, Multi Player, etc,
        /// as well as languages(enUS), operating system(Windows,linux), cpu architecture(x86/x64)
        /// These tags can vary greatly between products, and as such should be considered unique per product.
        /// </summary>
        private List<DownloadTag> DetermineTagsToUse(TactProduct targetProduct)
        {
            // Default tags that work with most products
            if (targetProduct.DefaultTags == null)
            {
                var tags = new List<string> { "enUS", "Windows", "noigr", "x86_64" };
                return _downloadFile.tags.Where(e => tags.Contains(e.Name)).ToList();
            }

            // Override tags that are specific to this product
            List<DownloadTag> tagsToUse = new List<DownloadTag>();
            foreach (var tag in targetProduct.DefaultTags)
            {
                var foundTags = _downloadFile.tags.Where(e => e.Name.Contains(tag));
                tagsToUse.AddRange(foundTags);
            }
            return tagsToUse;
        }

        /// <summary>
        /// Calculates the final download mask, that determines which files should be downloaded for the current product.
        /// </summary>
        internal DownloadTag BuildDownloadMask(List<DownloadTag> tagsToUse)
        {
            // Need to first pre-process tags that belong to the same "type".
            // These tags must be combined using logical OR to determine all files that might be installed.
            // Games like Call of Duty use these tags to determine which features to install (Campaign, Multiplayer, Zombies, etc.)
            var tagsByCategory = tagsToUse.GroupBy(e => e.Type)
                                          .Select(e => e.ToList())
                                          .Where(e => e.Count > 1) // Nothing to combine if there is only a single tag
                                          .ToList();
            foreach (var downloadTags in tagsByCategory)
            {
                var combinedMask = downloadTags.First();
                foreach (var currentTag in downloadTags)
                {
                    // Iterate through the two masks, combining them with a logical OR
                    for (int i = 0; i < combinedMask.Mask.Length; i++)
                    {
                        combinedMask.Mask[i] |= currentTag.Mask[i];
                    }
                    tagsToUse.Remove(currentTag);
                }
                tagsToUse.Add(combinedMask);
            }

            // Compute the final mask, which will be used to determine which files to download.
            // Files should only be downloaded if ALL tags say to download the file, eg. perform a logical AND across all tags
            var computedMask = tagsToUse.First();
            tagsToUse.RemoveAt(0);

            foreach (var currentTag in tagsToUse)
            {
                for (int i = 0; i < computedMask.Mask.Length; i++)
                {
                    computedMask.Mask[i] &= currentTag.Mask[i];
                }
            }

            return computedMask;
        }
    }
}