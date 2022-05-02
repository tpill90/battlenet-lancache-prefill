namespace BattleNetPrefill.Structs
{
    public sealed class DownloadFile
    {
        public uint numEntries;
        public uint numTags;

        public DownloadEntry[] entries;
        public DownloadTag[] tags;
    }
    
    public struct DownloadEntry
    {
        public MD5Hash hash;

        public override string ToString()
        {
            return hash.ToString();
        }
    }
    
    public struct DownloadTag
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