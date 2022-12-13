namespace BattleNetPrefill.Structs.Enums
{
    /// <summary>
    /// Enumeration of all possible endpoints for Blizzard's patch api
    /// https://wowdev.wiki/TACT#HTTP_URLs
    /// </summary>
    public class PatchRequest : EnumBase<PatchRequest>
    {
        /// <summary>
        /// A table of CDN domains available with game data per region 
        /// </summary>
        public static readonly PatchRequest cdns = new PatchRequest("cdns");

        /// <summary>
        /// Current version, BuildConfig, CdnConfig, ProductConfig and optionally keyring per region 
        /// </summary>
        public static readonly PatchRequest versions = new PatchRequest("versions");

        /// <summary>
        /// Similar to versions, but tailored for use by the Battle.net App background downloader 
        /// </summary>
        public static readonly PatchRequest bgdl = new PatchRequest("bgdl");

        /// <summary>
        /// Contains InstallBlobMD5 and GameBlobMD5  
        /// </summary>
        public static readonly PatchRequest blobs = new PatchRequest("bgdl");

        /// <summary>
        /// A blob file that regulates game functionality for the Battle.net App 
        /// </summary>
        public static readonly PatchRequest game = new PatchRequest("blob/game");

        /// <summary>
        /// A blob file that regulates installer functionality for the game in the Battle.net App 
        /// </summary>
        public static readonly PatchRequest install = new PatchRequest("blob/game");

        public PatchRequest(string name) : base(name)
        {
        }
    }
}
