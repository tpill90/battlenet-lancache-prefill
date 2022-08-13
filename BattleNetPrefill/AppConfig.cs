using System;
using System.IO;
using BattleNetPrefill.Utils;

namespace BattleNetPrefill
{
    public static class AppConfig
    {
        static AppConfig()
        {
            // Create required folders
            Directory.CreateDirectory(CacheDir);
        }

        /// <summary>
        /// https://wowdev.wiki/TACT#HTTP_URLs
        /// </summary>
        public static readonly Uri BattleNetPatchUri = new Uri("http://us.patch.battle.net:1119");

        public static readonly string CacheDir = Path.Combine(AppContext.BaseDirectory, "Cache");

        public static readonly DebugConfig DebugConfig = new DebugConfig
        {
            UseCdnDebugMode = false,
            CompareAgainstRealRequests = false
        };

        public static readonly string LogFileBasePath = @$"{DirectorySearch.TryGetSolutionDirectory()}/Logs";
    }

    public class DebugConfig
    {
        /// <summary>
        /// If set to true, will skip making any non-required requests, and instead record them to later be compared against for accuracy.
        /// Dramatically speeds up debugging since bandwidth use is a small fraction of the full download size (ex. 100mb vs a possible 30gb download).
        /// </summary>
        public bool UseCdnDebugMode { get; init; }

        /// <summary>
        /// When enabled, will compare the requests that this application made against the previously recorded requests that the real Battle.Net launcher makes.
        /// A comparison will be output to screen, giving feedback on how accurate our application is vs Battle.Net.
        ///
        /// While not required, enabling <see cref="UseCdnDebugMode"/> will allow for significantly faster debugging.
        /// </summary>
        public bool CompareAgainstRealRequests { get; init; }
    }
}
