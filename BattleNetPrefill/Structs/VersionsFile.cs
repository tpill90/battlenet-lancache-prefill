namespace BattleNetPrefill.Structs
{
    public struct VersionsEntry
    {
        public MD5Hash buildConfig;
        public MD5Hash cdnConfig;
        public string versionsName;
        public MD5Hash? keyRing;
    }
}