namespace BuildBackup.Structs
{
    public struct BuildConfigFile
    {
        public MD5Hash root;
        
        public MD5Hash[] download;
        public string[] downloadSize;

        public MD5Hash[] install;
        public string[] installSize;

        public MD5Hash[] encoding;
        public string[] encodingSize;
        public MD5Hash[] size;
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

        public MD5Hash? patch;
        public string patchSize;

        public MD5Hash? patchConfig;

        public MD5Hash[] patchIndex;

        public string partialPriority;
        public string partialPrioritySize;

        public MD5Hash[] vfsRoot;
        public int[] vfsRootSize;

        public string buildBaseContentReleaseManifestFileId;
        public string buildBaseContentReleaseManifestHash;

        public string buildContentReleaseManifestFileId;
        public string buildContentReleaseManifestHash;

        public string buildReleaseName;
    }
}