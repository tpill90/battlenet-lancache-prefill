namespace BattleNetPrefill.Structs
{
    public struct Archive
    {
        public string hashId;
        public MD5Hash hashIdMd5;

        // Not sure what this means right now
        public int archiveIndexSize;

        public override string ToString()
        {
            return $"{hashId}";
        }
    }
}