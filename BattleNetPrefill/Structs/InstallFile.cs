using System.Collections;
using System.Collections.Generic;

namespace BattleNetPrefill.Structs
{
    public struct InstallFile
    {
        public byte hashSize;
        public ushort numTags;
        public uint numEntries;
        public InstallTagEntry[] tags;
        public InstallFileEntry[] entries;
    }

    public struct InstallTagEntry
    {
        public string name;
        public ushort type;
        public BitArray files;
    }

    public struct InstallFileEntry
    {
        public string name;
        public MD5Hash contentHash;
        public uint size;
        public List<string> tags;

        public override string ToString()
        {
            return $"{name} size: {size}";
        }
    }
}