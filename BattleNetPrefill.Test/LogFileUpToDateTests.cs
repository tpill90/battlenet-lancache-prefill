using System.Diagnostics.CodeAnalysis;
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
        [TestCase("d3")]
        [TestCase("hero")]
        [TestCase("hsb")]
        [TestCase("osi")]
        [TestCase("pro")]
        [TestCase("rtro")]
        [TestCase("s1")]
        [TestCase("s2")]
        [TestCase("w3")]
        [TestCase("wow")]
        [TestCase("wow_classic")]
        [Parallelizable(ParallelScope.All)]
        [ExcludeFromCodeCoverage, Category("NoCoverage")]
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
