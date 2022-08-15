namespace BattleNetPrefill.Structs
{
    /// <summary>
    /// The install file lists files installed on disk. Since the install file is shared by architectures and OSs, there are also tags to select a subset of files.
    /// When using multiple tags, a binary combination of the bitfields of files to be installed can be created.
    ///
    /// https://wowdev.wiki/TACT#Install_manifest
    /// </summary>
    public struct InstallFile
    {
        /// <summary>
        /// Size of hashes used for files (usually md5 -> 16) 
        /// </summary>
        public byte hashSize;

        /// <summary>
        /// Number of tags in header of file 
        /// </summary>
        public ushort numTags;

        /// <summary>
        /// The number of entries in the body of the file 
        /// </summary>
        public uint numEntries;

        public InstallTagEntry[] tags;
        public InstallFileEntry[] entries;
    }

    public struct InstallTagEntry
    {
        public string name;
        public ushort type;

        /// <summary>
        /// A bitfield that lists which files are installed when the specified tag is installed. 
        /// </summary>
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