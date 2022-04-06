namespace BuildBackup.Structs
{
    public struct BuildConfigFile
    {
        public MD5Hash root;
        
        public MD5Hash[] download;
        public string[] downloadSize;

        public MD5Hash[] install;
        public string[] installSize;

        public string[] encoding;
        public string[] encodingSize;
        public string[] size;
        public string[] sizeSize;
        public string buildName;
        public string buildPlaybuildInstaller;
        public string buildProduct;
        public string buildUid;
        public string buildBranch;
        public string buildNumber;
        public string buildAttributes;
        public string buildComments;
        public string buildCreator;
        public string buildFixedHash;
        public string buildReplayHash;
        public string buildManifestVersion;
        public string patch;
        public string patchSize;
        public string patchConfig;
        public string partialPriority;
        public string partialPrioritySize;
    }
}