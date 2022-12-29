namespace BattleNetPrefill
{
    public static class AppConfig
    {
        static AppConfig()
        {
            // Create required folders
            Directory.CreateDirectory(CacheDir);

            if (Directory.Exists(CacheDirOriginal))
            {
                Directory.Delete(CacheDirOriginal, true);
            }
        }

        /// <summary>
        /// https://wowdev.wiki/TACT#HTTP_URLs
        /// </summary>
        public static readonly Uri BattleNetPatchUri = new Uri("http://us.patch.battle.net:1119");

        //TODO add migration to remove this
        //TODO remove after 2023/02/01
        public static readonly string CacheDirOriginal = Path.Combine(AppContext.BaseDirectory, "Cache");

        /// <summary>
        /// Downloaded archive indexes, as well as other metadata, are saved into this directory to speedup future prefill runs.
        /// All data in here should be able to be deleted safely.
        /// </summary>
        public static readonly string CacheDir = GetCacheDirBaseDirectories();

        public static readonly DebugConfig DebugConfig = new DebugConfig
        {
            //TODO turn this into a cli flag
            UseCdnDebugMode = true,
            CompareAgainstRealRequests = false
        };

        public static readonly string LogFileBasePath = @$"{DirectorySearch.TryGetSolutionDirectory()}/Logs";

        /// <summary>
        /// Global retry policy that will wait increasingly longer periods after a failed request
        /// </summary>
        public static AsyncRetryPolicy RetryPolicy = Policy.Handle<Exception>()
                                                 .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromMilliseconds(100 * retryAttempt));

        //TODO move to lancacheprefill.common
        /// <summary>
        /// Gets the base directories for the cache folder, determined by which Operating System the app is currently running on.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static string GetCacheDirBaseDirectories()
        {
            if (System.OperatingSystem.IsWindows())
            {
                string pathAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(pathAppData, "BattlenetPrefill", "Cache");
            }
            if (System.OperatingSystem.IsLinux())
            {
                // Gets base directories for the XDG Base Directory specification (Linux)
                string pathHome = Environment.GetEnvironmentVariable("HOME")
                                  ?? throw new ArgumentNullException("HOME", "Could not determine HOME directory");

                string pathXdgCacheHome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME")
                                          ?? Path.Combine(pathHome, ".cache");

                return Path.Combine(pathXdgCacheHome, "SteamPrefill");
            }
            if (System.OperatingSystem.IsMacOS())
            {
                string pathLibraryCaches = Path.GetFullPath("~/Library/Caches");
                return Path.Combine(pathLibraryCaches, "SteamPrefill");
            }

            throw new NotSupportedException($"Unknown platform {RuntimeInformation.OSDescription}");
        }
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
