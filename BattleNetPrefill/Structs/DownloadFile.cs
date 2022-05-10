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
    
    public sealed class DownloadTag
    {
        /// <summary>
        /// The name of the tag, describing what it is for.
        /// Ex. "enUS", "Multiplayer"
        /// </summary>
        public string Name;

        /// <summary>
        /// Tags are grouped in categories by different "types".  Some of these tags for example may include
        /// "language", "operating system", "architecture", "game feature".
        ///
        /// These tags are 
        /// </summary>
        public short Type;

        public byte[] Mask;
        
        public bool FileShouldBeDownloaded(int index)
        {
            return (Mask[index / 8] & (1 << (index % 8))) != 0;
        }

        public override string ToString()
        {
            return $"{Name} {Type}";
        }
    }
}