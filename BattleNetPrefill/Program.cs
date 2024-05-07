namespace BattleNetPrefill
{
    // TODO - Add publish build pipeline
    // TODO - Setup mkdocs and copy from SteamPrefill.  Update docs in general
    // TODO warcraft 3 hangs for some reason on retreiving uncached archive indexes
    //TODO add total download size to summary
    public static class Program
    {
        private const string Description = "Automatically fills a Lancache with games from Battle.Net, so that subsequent downloads will be \n" +
                                           "  served from the Lancache, improving speeds and reducing load on your internet connection. \n" +
                                           "\n" +
                                           "  Start by selecting apps for prefill with the 'select-apps' command, then start the prefill using 'prefill'";

        public static async Task<int> Main()
        {
            try
            {
                // Checking to see if the user double clicked the exe in Windows, and display a message on how to use the app
                OperatingSystemUtils.DetectDoubleClickOnWindows("BattleNetPrefill");

                var cliArgs = ParseHiddenFlags();
                return await new CliApplicationBuilder()
                             .AddCommandsFromThisAssembly()
                             .SetTitle("BattleNetPrefill")
                             .SetExecutableNamePlatformAware("BattleNetPrefill")
                             .SetDescription(Description)
                             .SetVersion($"v{ThisAssembly.Info.InformationalVersion}")
                             .Build()
                             .RunAsync(cliArgs);
            }
            catch (Exception e)
            {
                AnsiConsole.Console.LogException(e);
            }

            // Return failed status code, since you can only get to this line if an exception was handled
            return 1;
        }

        /// <summary>
        /// Adds hidden flags that may be useful for debugging/development, but shouldn't be displayed to users in the help text
        /// </summary>
        private static List<string> ParseHiddenFlags()
        {
            // Have to skip the first argument, since its the path to the executable
            var args = Environment.GetCommandLineArgs().Skip(1).ToList();

            // TODO comment
            if (args.Any(e => e.Contains("--compare-requests")))
            {
                AnsiConsole.Console.LogMarkupLine($"Using {LightYellow("--compare-requests")} flag.  Running comparison logic...");
                // Need to enable SkipDownloads as well in order to get this to work well
                AppConfig.CompareAgainstRealRequests = true;
                args.Remove("--compare-requests");
            }

            // Will skip over downloading logic.  Will only download manifests and compute files to be downloaded
            if (args.Any(e => e.Contains("--no-download")))
            {
                AnsiConsole.Console.LogMarkupLine($"Using {LightYellow("--no-download")} flag.  Will skip downloading chunks...");
                AppConfig.SkipDownloads = true;
                args.Remove("--no-download");
            }

            // Skips using locally cached indexes. Saves disk space, at the expense of slower subsequent runs.
            // Useful for debugging since the indexes will always be re-downloaded.
            if (args.Any(e => e.Contains("--nocache")) || args.Any(e => e.Contains("--no-cache")))
            {
                AnsiConsole.Console.LogMarkupLine($"Using {LightYellow("--nocache")} flag.  Will always re-download indexes...");
                AppConfig.NoLocalCache = true;
                args.Remove("--nocache");
                args.Remove("--no-cache");
            }

            // Adding some formatting to logging to make it more readable + clear that these flags are enabled
            if (AppConfig.CompareAgainstRealRequests || AppConfig.SkipDownloads || AppConfig.NoLocalCache)
            {
                AnsiConsole.Console.WriteLine();
                AnsiConsole.Console.Write(new Rule());
            }

            return args;
        }
    }
}