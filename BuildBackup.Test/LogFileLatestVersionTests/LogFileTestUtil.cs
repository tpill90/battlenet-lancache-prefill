using System;
using BuildBackup.DebugUtil;
using BuildBackup.Handlers;
using BuildBackup.Structs;
using BuildBackup.Web;
using Spectre.Console.Testing;

namespace BuildBackup.Test.LogFileLatestVersionTests
{
    public static class LogFileTestUtil
    {
        private static readonly Uri baseUrl = new Uri("http://us.patch.battle.net:1119/");
        private static readonly string LogFileBasePath = @"C:\Users\Tim\Dropbox\Programming\dotnet-public\BattleNetBackup\RequestReplayer\Logs";

        public static string GetLatestLogFileVersion(TactProduct product)
        {
            return NginxLogParser.GetLatestLogVersionForProduct(LogFileBasePath, product);
        }

        public static VersionsEntry GetLatestCdnVersion(TactProduct product)
        {
            // Finding the latest version of the game
            ConfigFileHandler configFileHandler = new ConfigFileHandler(new CDN(new TestConsole(), baseUrl));
            VersionsEntry cdnVersion = configFileHandler.GetLatestVersionEntry(product);
            return cdnVersion;
        }
    }
}
