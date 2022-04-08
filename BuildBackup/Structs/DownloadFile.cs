using System.Collections;

namespace BuildBackup.Structs
{
    public struct DownloadFile
    {
        public byte[] unk;
        public uint numEntries;
        public uint numTags;
        public DownloadEntry[] entries;
        public DownloadTag[] tags;
    }

    public struct DownloadEntry
    {
        public MD5Hash hash;
        public byte[] unk;

        public override string ToString()
        {
            return hash.ToString();
        }
    }

    public sealed class DownloadTag
    {
        public string Name;
        public short Type;
        public BitArray Bits;
    }
}