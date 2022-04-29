using BattleNetPrefill.Handlers;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Utils.Debug;
using BattleNetPrefill.Web;
using NUnit.Framework;

namespace BattleNetPrefill.Test
{
    [TestFixture]
    [Parallelizable(ParallelScope.Self)]
    public class LogFileUpToDateTests
    {
        [Test]
        [TestCase("d3")]
        [TestCase("fore")]
        [TestCase("hero")]
        [TestCase("hsb")]
        [TestCase("lazr")]
        [TestCase("odin")]
        [TestCase("osi")]
        [TestCase("pro")]
        [TestCase("rtro")]
        [TestCase("s1")]
        [TestCase("s2")]
        [TestCase("viper")]
        [TestCase("w3")]
        [TestCase("wlby")]
        [TestCase("wow")]
        [TestCase("wow_classic")]
        [TestCase("zeus")]
        [Parallelizable(ParallelScope.All)]
        public void LogFilesAreUpToDate(string productCode)
        {
            var targetProduct = TactProduct.Parse(productCode);

            VersionsEntry latestVersion = GetLatestCdnVersion(targetProduct);
            var currentLogFileVersion = NginxLogParser.GetLatestLogVersionForProduct(Config.LogFileBasePath, targetProduct);

            Assert.AreEqual(latestVersion.versionsName, currentLogFileVersion);
        }

        private static VersionsEntry GetLatestCdnVersion(TactProduct product)
        {
            // Finding the latest version of the game
            ConfigFileHandler configFileHandler = new ConfigFileHandler(new CDN(Config.BattleNetPatchUri));
            VersionsEntry cdnVersion = configFileHandler.GetLatestVersionEntryAsync(product).Result;
            return cdnVersion;
        }
    }
}
