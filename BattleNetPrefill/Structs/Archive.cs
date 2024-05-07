namespace BattleNetPrefill.Structs
{
    public struct Archive
    {
        public string hashId;
        public MD5Hash hashIdMd5;

        public override string ToString()
        {
            return $"{hashId}";
        }
    }

    public readonly struct ArchiveIndexEntry
    {
        /// <summary>
        /// Use this to lookup the CdnHash of the archive index that holds this file.
        /// </summary>
        public readonly short index;

        public readonly uint offset;
        public readonly uint size;

        public ArchiveIndexEntry(short index, uint size, uint offset)
        {
            this.index = index;
            this.offset = offset;
            this.size = size;
        }
    }

    // These fields are all "used" because we're reading the entire struct directly from the stream
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    public struct ArchiveIndexFooter
    {
        public byte version;
        public byte unk1;
        public byte unk2;
        public byte blockSizeKb;
        public byte offsetBytes;
        public byte sizeBytes;
        public byte keySizeBytes;
        public byte checksumSize;
        public int numElements;
    }
}