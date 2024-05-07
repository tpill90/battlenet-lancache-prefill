namespace BattleNetPrefill.Structs
{
    /// <summary>
    /// https://wowdev.wiki/TACT#Encoding_table
    /// </summary>
    public struct EncodingFile
    {
        public uint numEntriesA;
        public ulong stringBlockSize;

        public Dictionary<MD5Hash, MD5Hash> aEntriesReversed;

        /// <summary>
        /// Lookup from EncodingKey -> CdnKey.  Lookup can be used to take the Md5 for a file, and lookup where it can be downloaded from on the CDNs.
        /// </summary>
        public Dictionary<MD5Hash, MD5Hash> ReversedEncodingDictionary => aEntriesReversed;
    }
}