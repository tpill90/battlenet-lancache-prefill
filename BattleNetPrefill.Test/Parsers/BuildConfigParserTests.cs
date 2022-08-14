using System.Threading.Tasks;
using BattleNetPrefill.Handlers;
using BattleNetPrefill.Parsers;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Web;
using NUnit.Framework;
using Spectre.Console.Testing;

namespace BattleNetPrefill.Test.Parsers
{
    [TestFixture]
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
            CdnRequestManager cdnRequestManager = new CdnRequestManager(AppConfig.BattleNetPatchUri, new TestConsole(), useDebugMode: true);
            await cdnRequestManager.InitializeAsync(tactProduct);
            var configFileHandler = new ConfigFileHandler(cdnRequestManager);
            VersionsEntry targetVersion = await configFileHandler.GetLatestVersionEntryAsync(tactProduct);

            // Parsing the build config
            var buildConfig = await BuildConfigParser.GetBuildConfigAsync(targetVersion, cdnRequestManager, tactProduct);

            // Expecting that there are no unknown keypairs left after parsing.
            Assert.AreEqual(0, buildConfig.UnknownKeyPairs.Count);
        }
    }
}
