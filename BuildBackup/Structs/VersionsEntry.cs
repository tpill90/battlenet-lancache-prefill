namespace BuildBackup.Structs
{
    public struct VersionsEntry
    {
        public string region;
        public MD5Hash buildConfig;
        public MD5Hash cdnConfig;
        public string buildId;
        public string versionsName;
        public string productConfig;
        public MD5Hash? keyRing;
    }
}