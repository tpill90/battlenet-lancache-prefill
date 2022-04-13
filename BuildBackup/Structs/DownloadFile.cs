using System;
using System.Collections;

namespace BuildBackup.Structs
{
    //TODO document how this works
    public sealed class DownloadFile
    {
        public byte[] unk;
        public uint numEntries;
        public uint numTags;
        public DownloadEntry[] entries;
        public DownloadTag[] tags;
    }

    //TODO document how this works
    public struct DownloadEntry
    {
        public MD5Hash hash;
        public byte[] unk;
        public UInt64 fileSize;

        public override string ToString()
        {
            return hash.ToString();
        }
    }

    //TODO document how this works
    public sealed class DownloadTag
    {
        public string Name;
        public short Type;
        public byte[] Mask;

        public override string ToString()
        {
            return $"{Name} {Type}";
        }
    }
}