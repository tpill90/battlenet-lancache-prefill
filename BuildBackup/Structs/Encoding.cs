using System.Collections.Generic;

namespace BuildBackup.Structs
{
    //TODO comment
    public class EncodingTable
    {
        public Dictionary<string, string> EncodingDictionary = new Dictionary<string, string>();

        public string rootKey = "";
        public string downloadKey = "";
        public string installKey = "";
    }
}