namespace BattleNetPrefill.Integration.Test
{
    [TestFixture]
    public class LogFileUpToDateTests
    {
        [Test]
        [TestCase("pro")]
        [TestCase("s1")]
        [TestCase("s2")]
        [TestCase("wow")]
        [TestCase("wow_classic")]
        [Parallelizable(ParallelScope.All)]
        [ExcludeFromCodeCoverage, Category("SkipCI")]
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
            ConfigFileHandler configFileHandler = new ConfigFileHandler(new CdnRequestManager(new TestConsole()));
            VersionsEntry cdnVersion = configFileHandler.GetLatestVersionEntryAsync(product).Result;
            return cdnVersion;
        }
    }
}
