namespace BattleNetPrefill.Integration.Test.Parsers
{
    [TestFixture]
    [Category("SkipCI")]
    public class BuildConfigParserTests
    {
        /// <summary>
        /// This test is to ensure that all possible BuildConfig fields are being properly handled.
        /// Also it should hopefully catch any new fields introduced in the future.
        /// </summary>
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
        public async Task BuildConfig_ShouldHaveNoUnknownKeyPairs(string productCode)
        {
            var tactProduct = TactProduct.Parse(productCode);

            // Setting up required classes
            AppConfig.SkipDownloads = true;
            CdnRequestManager cdnRequestManager = new CdnRequestManager(AppConfig.BattleNetPatchUri, new TestConsole());
            await cdnRequestManager.InitializeAsync(tactProduct);
            var configFileHandler = new ConfigFileHandler(cdnRequestManager);
            VersionsEntry targetVersion = await configFileHandler.GetLatestVersionEntryAsync(tactProduct);

            // Parsing the build config
            var buildConfig = await BuildConfigParser.GetBuildConfigAsync(targetVersion, cdnRequestManager);

            // Expecting that there are no unknown keypairs left after parsing.
            Assert.AreEqual(0, buildConfig.UnknownKeyPairs.Count);
        }
    }
}
