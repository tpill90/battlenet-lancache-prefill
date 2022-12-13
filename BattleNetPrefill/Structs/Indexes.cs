namespace BattleNetPrefill.Structs
{
    public struct IndexEntry
    {
        /// <summary>
        /// Use this to lookup the CdnHash of the archive index that holds this file.
        /// </summary>
        public short index;

        public uint offset;
        public uint size;

        public override string ToString()
        {
            return $"{index} Offset: {offset} size: {size}";
        }
    }

    public struct IndexFooter
    {
        public byte[] tocHash;
        public byte version;
        public byte unk0;
        public byte unk1;
        public byte blockSizeKB;
        public byte offsetBytes;
        public byte sizeBytes;
        public byte keySizeInBytes;
        public byte checksumSize;
        public uint numElements;
        public byte[] footerChecksum;
    }
}
