using System;
using System.IO;
using System.Linq;
using BuildBackup.Structs;
using Konsole;
using Shared;

namespace BuildBackup.Test.LogFileLatestVersionTests
{
    public static class LogFileTestUtil
    {
        private static readonly Uri baseUrl = new Uri("http://us.patch.battle.net:1119/");
        private static readonly string LogFileBasePath = @"C:\Users\Tim\Dropbox\Programming\dotnet-public\BattleNetBackup\RequestReplayer\Logs";

        public static string GetLatestLogFileVersion(TactProduct product)
        {
            // Finding the most recent log file version
            var latestLogFile = new DirectoryInfo($@"{LogFileBasePath}\{product.DisplayName}")
                .GetFiles()
                .Where(e => !e.Name.Contains("coalesced"))
                .OrderByDescending(e => e.LastWriteTime)
                .FirstOrDefault();
            return latestLogFile.Name.Replace(".log", "");
        }

        public static VersionsEntry GetLatestCdnVersion(TactProduct product)
        {
            // Finding the latest version of the game
            Logic logic = new Logic(new CDN(new MockConsole(120, 50)), baseUrl);
            VersionsEntry cdnVersion = logic.GetVersionEntry(product);
            return cdnVersion;
        }
    }
}
