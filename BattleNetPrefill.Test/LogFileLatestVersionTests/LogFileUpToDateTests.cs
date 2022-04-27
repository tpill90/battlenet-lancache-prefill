using BattleNetPrefill.Handlers;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Utils.Debug;
using BattleNetPrefill.Web;
using NUnit.Framework;

namespace BattleNetPrefill.Test.LogFileLatestVersionTests
{
    //TODO make this a single class, with parameterized inputs for each product
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
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
            var product = TactProduct.Parse(productCode);

            VersionsEntry cdnVersion = GetLatestCdnVersion(product);
            var latestLogFile = NginxLogParser.GetLatestLogVersionForProduct(Config.LogFileBasePath, product);

            Assert.AreEqual(cdnVersion.versionsName, latestLogFile);
        }

        public static VersionsEntry GetLatestCdnVersion(TactProduct product)
        {
            // Finding the latest version of the game
            ConfigFileHandler configFileHandler = new ConfigFileHandler(new CDN(Config.BattleNetPatchUri));
            VersionsEntry cdnVersion = configFileHandler.GetLatestVersionEntryAsync(product).Result;
            return cdnVersion;
        }
    }
}
