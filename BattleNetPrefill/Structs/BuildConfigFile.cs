using System.Collections.Generic;

namespace BattleNetPrefill.Structs
{
    public sealed class BuildConfigFile
    {
        public MD5Hash root;
        
        public MD5Hash[] download;
        public string[] downloadSize;

        public MD5Hash[] install;
        public int[] installSize;

        public MD5Hash[] encoding;
        public int[] encodingSize;

        public MD5Hash[] size;
        public int[] sizeSize;

        public MD5Hash? patch;
        public string patchSize;

        public MD5Hash? patchConfig;

        public MD5Hash[] patchIndex;
        public int[] patchIndexSize;

        public MD5Hash[] vfsRoot;
        public int[] vfsRootSize;

        public Dictionary<string,string> vfs = new Dictionary<string, string>();
        public Dictionary<string, string> vfsSize = new Dictionary<string, string>();

        public string buildName;
        public string buildPlaybuildInstaller;
        public string buildProduct;
        public string buildToken;
        public string buildUid;
        public string buildBranch;
        public string buildNumber;
        public string buildAttributes;
        public string buildComments;
        public string buildCreator;
        public string buildFixedHash;
        public string buildReplayHash;
        public string buildManifestVersion;

        public string partialPriority;
        public string partialPrioritySize;

        public string buildBaseContentReleaseManifestFileId;
        public string buildBaseContentReleaseManifestHash;

        public string buildContentReleaseManifestFileId;
        public string buildContentReleaseManifestHash;

        public string buildReleaseName;

        public Dictionary<string, string> UnknownKeyPairs = new Dictionary<string, string>();
    }
}