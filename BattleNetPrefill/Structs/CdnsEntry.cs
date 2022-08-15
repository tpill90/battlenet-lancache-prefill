namespace BattleNetPrefill.Structs
{
    public class CdnsFile
    {
        public CdnsEntry[] entries;

        public List<string> UnknownKeyPairs = new List<string>();
    }

    public struct CdnsEntry
    {
        public string name;
        public string path;
        public string[] hosts;
        public string configPath;
        public string servers;
    }
}