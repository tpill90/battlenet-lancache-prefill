namespace BattleNetPrefill.Structs
{
    public sealed class CDNConfigFile
    {
        /// <summary>
        /// A list of CDN identifiers for all archives
        /// </summary>
        public Archive[] archives;

        public string archiveGroup;

        public MD5Hash[] patchArchives;
        public int[] patchArchivesIndexSize;
        public string patchArchiveGroup;

        public string[] builds;

        /// <summary>
        /// CDN Key for the "file" index.  Can be requested and parsed.
        ///
        /// The "file" index lists what known "unarchived" files exist on the CDN.  Using the index you can take a content MD5,  and can be used to lookup the CDNKey
        /// used to download the file.
        /// </summary>
        public MD5Hash fileIndex;
        public string fileIndexSize;

        public MD5Hash? patchFileIndex;
        public int patchFileIndexSize;
    }
}