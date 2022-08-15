namespace BattleNetPrefill.Utils
{
    public static class UpdateChecker
    {
        private const string _repoName = "tpill90/Battlenet-lancache-prefill";
        private static readonly string _lastUpdateCheckFile = $"{AppConfig.CacheDir}/lastUpdateCheck.txt";

        /// <summary>
        /// Compares the current application version against the newest version available on Github Releases.  If there is a newer version, displays a message
        /// to the user.
        /// </summary>
        public static async Task CheckForUpdatesAsync()
        {
            try
            {
                if (UpdatesHaveBeenRecentlyChecked())
                {
                    return;
                }

                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
                httpClient.DefaultRequestHeaders.Add("User-Agent", _repoName);

                // Query Github for a list of all available releases
                var response = await httpClient.GetStringAsync(new Uri($"https://api.github.com/repos/{_repoName}/releases"));
                GithubRelease latestRelease = JsonSerializer.Deserialize(response, Structs.Enums.SerializationContext.Default.ListGithubRelease)
                                                            .OrderByDescending(e => e.PublishedAt)
                                                            .First();

                // Compare the available releases against our known releases
                var latestVersion = latestRelease.TagName.Replace("v", "");
                var assemblyVersion = typeof(Program).Assembly.GetName().Version.ToString(3);
                if (latestVersion != assemblyVersion)
                {
                    WriteUpdateMessage(assemblyVersion, latestVersion);
                }

                await File.WriteAllTextAsync(_lastUpdateCheckFile, DateTime.Now.ToString());
            }
            catch
            {
                // Doesn't matter if this fails.  Its non-critical to the application's function
            }
        }

        /// <summary>
        /// Will only check for updates once every 7 days.  If updates have been checked within the last 7 days, will return true.
        /// </summary>
        private static bool UpdatesHaveBeenRecentlyChecked()
        {
            var fileInfo = new FileInfo(_lastUpdateCheckFile);
            return fileInfo.Exists && fileInfo.LastWriteTimeUtc.AddDays(3) > DateTime.UtcNow;
        }

        private static void WriteUpdateMessage(string currentVersion, string updateVersion)
        {
            var table = new Table
            {
                ShowHeaders = false,
                Border = TableBorder.Rounded,
                BorderStyle = new Style(Color.Yellow4)
            };
            table.AddColumn("");

            // Add some rows
            table.AddRow("");
            table.AddRow($"A newer version is available {currentVersion} → {Olive(updateVersion)}");
            table.AddRow("");
            table.AddRow($"Download at :  ");
            table.AddRow(LightBlue($"https://api.github.com/repos/{_repoName}/releases"));
            table.AddRow("");

            // Render the table to the console
            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }
    }
    
    public class GithubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }

        [JsonPropertyName("published_at")]
        public DateTime PublishedAt { get; set; }

        public override string ToString()
        {
            return $"{TagName} - {PublishedAt}";
        }
    }
}
