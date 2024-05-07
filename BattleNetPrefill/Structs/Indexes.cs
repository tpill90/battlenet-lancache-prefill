namespace BattleNetPrefill.Structs
{
    public struct IndexEntry
    {
        public uint offset;
        public uint size;

        public override string ToString()
        {
            return $"Offset: {offset} size: {size}";
        }
    }

    // These fields are all "used" because we're reading the entire struct directly from the stream
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    public struct IndexFooter
    {
        public ulong tocHash;
        public byte version;
        public byte unk0;
        public byte unk1;
        public byte blockSizeKB;
        public byte offsetBytes;
        public byte sizeBytes;
        public byte keySizeInBytes;
        public byte checksumSize;
        public uint numElements;
    }
}
