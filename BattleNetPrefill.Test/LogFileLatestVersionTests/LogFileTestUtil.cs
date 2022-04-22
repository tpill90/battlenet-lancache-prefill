using System;
using BattleNetPrefill.DebugUtil;
using BattleNetPrefill.Handlers;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Web;

namespace BattleNetPrefill.Test.LogFileLatestVersionTests
{
    public static class LogFileTestUtil
    {
        public static string GetLatestLogFileVersion(TactProduct product)
        {
            var latestLogVersionForProduct = NginxLogParser.GetLatestLogVersionForProduct(Config.LogFileBasePath, product);
            return latestLogVersionForProduct;
        }

        public static VersionsEntry GetLatestCdnVersion(TactProduct product)
        {
            // Finding the latest version of the game
            ConfigFileHandler configFileHandler = new ConfigFileHandler(new CDN(Config.BattleNetPatchUri));
            VersionsEntry cdnVersion = configFileHandler.GetLatestVersionEntry(product);
            return cdnVersion;
        }
    }
}
