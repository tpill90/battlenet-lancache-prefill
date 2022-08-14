using BattleNetPrefill.Handlers;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Utils.Debug;
using BattleNetPrefill.Web;
using NUnit.Framework;
using Spectre.Console.Testing;

namespace BattleNetPrefill.Test
{
    [TestFixture]
    public class LogFileUpToDateTests
    {
        [Test]
        [TestCase("anbs")]
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
            var currentLogFileVersion = NginxLogParser.GetLatestLogVersionForProduct(AppConfig.LogFileBasePath, targetProduct);

            Assert.AreEqual(latestVersion.versionsName, currentLogFileVersion);
        }

        private static VersionsEntry GetLatestCdnVersion(TactProduct product)
        {
            // Finding the latest version of the game
            ConfigFileHandler configFileHandler = new ConfigFileHandler(new CdnRequestManager(AppConfig.BattleNetPatchUri, new TestConsole()));
            VersionsEntry cdnVersion = configFileHandler.GetLatestVersionEntryAsync(product).Result;
            return cdnVersion;
        }
    }
}
