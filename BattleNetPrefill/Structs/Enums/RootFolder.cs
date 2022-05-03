namespace BattleNetPrefill.Structs
{
    /// <summary>
    /// Files are grouped on Blizzard's CDNs in one of three folders.
    /// The url to access these files will be build with the following format :
    ///     http://{host}/{productPath}/{rootFolder}
    /// 
    /// See more: https://wowdev.wiki/TACT#File_types
    /// </summary>
    public class RootFolder : EnumBase<RootFolder>
    {
        /// <summary>
        /// Contains archives, indexes, and unarchived standalone files (typically binaries, mp3s, and movies)
        /// </summary>
        public static readonly RootFolder data = new RootFolder("data");

        /// <summary>
        /// Contains the three types of config files: Build configs, CDN configs, and Patch configs
        /// </summary>
        public static readonly RootFolder config = new RootFolder("config");

        /// <summary>
        /// Contains patch manifests, files, archives, indexes
        /// </summary>
        public static readonly RootFolder patch = new RootFolder("patch");

        public RootFolder(string name) : base(name)
        {
        }
    }
}
