namespace BattleNetPrefill.Parsers
{
    public static class IndexParser
    {
        public static async Task<Dictionary<MD5Hash, IndexEntry>> ParseIndexAsync(CdnRequestManager cdnRequestManager, RootFolder folder, MD5Hash hashId)
        {
            byte[] indexContent = await cdnRequestManager.GetRequestAsBytesAsync(folder, hashId, isIndex: true);

            var indexDict = new Dictionary<MD5Hash, IndexEntry>();

            using BinaryReader bin = new BinaryReader(new MemoryStream(indexContent));
            bin.BaseStream.Position = bin.BaseStream.Length - 28;

            var footer = bin.Read<IndexFooter>();
            // Skipping footer checksum
            bin.ReadBytes(footer.checksumSize);

            if ((footer.numElements & 0xff000000) != 0)
            {
                bin.BaseStream.Position -= footer.checksumSize + 4;
                footer.numElements = bin.ReadUInt32BigEndian();
            }

            bin.BaseStream.Position = 0;

            var indexBlockSize = 1024 * footer.blockSizeKB;

            int indexEntries = indexContent.Length / indexBlockSize;
            var recordSize = footer.keySizeInBytes + footer.sizeBytes + footer.offsetBytes;
            var recordsPerBlock = indexBlockSize / recordSize;
            var blockPadding = indexBlockSize - (recordsPerBlock * recordSize);

            byte[] md5HashBuffer = BinaryReaderExtensions.AllocateBuffer<MD5Hash>();

            for (var b = 0; b < indexEntries; b++)
            {
                for (var bi = 0; bi < recordsPerBlock; bi++)
                {
                    if (footer.keySizeInBytes != 16)
                    {
                        throw new Exception("Index Header must be 16 bytes!!!");
                    }

                    MD5Hash headerHash = bin.ReadMd5Hash(md5HashBuffer);
                    var indexEntry = new IndexEntry();

                    if (footer.sizeBytes == 4)
                    {
                        indexEntry.size = bin.ReadUInt32BigEndian();
                    }

                    if (footer.offsetBytes == 4)
                    {
                        // Archive index
                        indexEntry.offset = bin.ReadUInt32BigEndian();
                    }
                    else if (footer.offsetBytes == 6)
                    {
                        // Group index
                        throw new NotImplementedException("Group index reading is not implemented!");
                    }

                    if (indexEntry.size == 0)
                    {
                        continue;
                    }

                    indexDict.TryAdd(headerHash, indexEntry);
                }

                bin.ReadBytes(blockPadding);
            }
            return indexDict;
        }
    }
}