using System.Collections.Generic;
using BuildBackup.Structs;

namespace BuildBackup.Utils
{
    //TODO document what this does, and why it is needed
    public class CachedStringLookup
    {
        private readonly CdnsFile _cdnsFile;
        private Dictionary<MD5Hash, string> _cacheLookupDictionary = new Dictionary<MD5Hash, string>();

        public CachedStringLookup(CdnsFile cdnsFile)
        {
            _cdnsFile = cdnsFile;
        }

        public string TryGetPrecomputedValue(MD5Hash hash, RootFolder rootPath)
        {
            if (_cacheLookupDictionary.ContainsKey(hash))
            {
                return _cacheLookupDictionary[hash];
            }
            else
            {
                //TODO write a ToLower variant
                var hashString = hash.ToString().ToLower();
                var uri = $"{_cdnsFile.entries[0].path}/{rootPath.Name}/{hashString[0]}{hashString[1]}/{hashString[2]}{hashString[3]}/{hashString}";
                _cacheLookupDictionary.Add(hash, uri);

                return uri;
            }
        }
    }
}