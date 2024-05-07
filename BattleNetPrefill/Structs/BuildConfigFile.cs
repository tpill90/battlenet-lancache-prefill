namespace BattleNetPrefill.Structs
{
    public sealed class BuildConfigFile
    {
        public MD5Hash[] download;

        public MD5Hash[] install;
        public int[] installSize;

        public MD5Hash[] encoding;
        public int[] encodingSize;

        public MD5Hash[] size;
        public int[] sizeSize;

        public MD5Hash? patch;

        public MD5Hash? patchConfig;

        public MD5Hash[] patchIndex;
        public int[] patchIndexSize;

        public MD5Hash[] vfsRoot;
        public int[] vfsRootSize;

        public string buildName;

        public Dictionary<string, string> UnknownKeyPairs = new Dictionary<string, string>();
    }
}