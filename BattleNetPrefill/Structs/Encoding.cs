namespace BattleNetPrefill.Structs
{
    /// <summary>
    /// https://wowdev.wiki/TACT#Encoding_table
    /// </summary>
    public struct EncodingFile
    {
        public byte unk1;
        public byte checksumSizeA;
        public byte checksumSizeB;
        public ushort sizeA;
        public ushort sizeB;
        public uint numEntriesA;
        public uint numEntriesB;
        public byte unk2;
        public ulong stringBlockSize;
        public string[] stringBlockEntries;
        public EncodingHeaderEntry[] aHeaders;
        //public Dictionary<MD5Hash, MD5Hash> aEntries;

        public Dictionary<MD5Hash, MD5Hash> aEntriesReversed;

        /// <summary>
        /// Lookup from EncodingKey -> CdnKey.  Lookup can be used to take the Md5 for a file, and lookup where it can be downloaded from on the CDNs.
        /// </summary>
        public Dictionary<MD5Hash, MD5Hash> ReversedEncodingDictionary => aEntriesReversed;

        public EncodingHeaderEntry[] bHeaders;
        public Dictionary<string, EncodingFileDescEntry> bEntries;
        public string encodingESpec;
    }

    public struct EncodingHeaderEntry
    {
        public MD5Hash firstHash;
        public MD5Hash checksum;
    }

    public sealed class EncodingFileEntry
    {
        public ushort keyCount;
        public uint size;
        public MD5Hash hash;

        public MD5Hash key;
    }

    public struct EncodingFileDescEntry
    {
        public string key;
        public uint stringIndex;
        public ulong compressedSize;
    }
}