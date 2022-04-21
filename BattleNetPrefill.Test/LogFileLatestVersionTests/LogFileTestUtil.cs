using System;
using BattleNetPrefill.DebugUtil;
using BattleNetPrefill.Handlers;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Web;
using Spectre.Console.Testing;

namespace BattleNetPrefill.Test.LogFileLatestVersionTests
{
    public static class LogFileTestUtil
    {
        private static readonly Uri baseUrl = new Uri("http://us.patch.battle.net:1119/");
        private static readonly string LogFileBasePath = @"C:\Users\Tim\Dropbox\Programming\dotnet-public\BattleNetBackup\RequestReplayer\Logs";

        public static string GetLatestLogFileVersion(TactProduct product)
        {
            var latestLogVersionForProduct = NginxLogParser.GetLatestLogVersionForProduct(LogFileBasePath, product);
            return latestLogVersionForProduct;
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
