using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Spectre.Console;
using Utf8Json;
using static BattleNetPrefill.Utils.SpectreColors;

namespace BattleNetPrefill.Utils
{
    public static class UpdateChecker
    {
        private static readonly Uri _githubReleasesUri = new Uri("https://api.github.com/repos/tpill90/Battlenet-lancache-prefill/releases");
        private static readonly string _lastUpdateCheckFile = $"{Config.CacheDir}/lastUpdateCheck.txt";

        //TODO document
        //TODO wrap in a try catch
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
                httpClient.DefaultRequestHeaders.Add("User-Agent", "BattleNetPrefill");

                // Query Github for a list of all available releases
                var response = await httpClient.GetStringAsync(_githubReleasesUri);
                GithubRelease latestRelease = JsonSerializer.Deserialize<List<GithubRelease>>(response)
                                                      .OrderByDescending(e => e.published_at)
                                                      .First();

                // Compare the available releases against our known releases
                var latestVersion = latestRelease.tag_name.Replace("v", "");
                var assemblyVersion = typeof(Program).Assembly.GetName().Version.ToString(3);
                if (latestVersion != assemblyVersion)
                {
                    WriteUpdateMessage(assemblyVersion, latestVersion);
                }

                await File.WriteAllTextAsync(_lastUpdateCheckFile, DateTime.Now.ToLongTimeString());
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
            return fileInfo.Exists && fileInfo.CreationTime.AddDays(7) > DateTime.UtcNow;
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
            table.AddRow(LightBlue("https://github.com/tpill90/Battlenet-lancache-prefill/releases"));
            table.AddRow("");

            // Render the table to the console
            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }
    }
    
    public class GithubRelease
    {
        public string tag_name { get; set; }
        public string name { get; set; }
        public bool draft { get; set; }
        public bool prerelease { get; set; }
        public DateTime created_at { get; set; }
        public DateTime published_at { get; set; }
    }
}
