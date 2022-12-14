using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using BattleNetPrefill.Utils.Debug.Models;
using ByteSizeLib;
using NUnit.Framework;
using Spectre.Console.Testing;

namespace BattleNetPrefill.Test.DownloadTests.Blizzard
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    [ExcludeFromCodeCoverage, Category("NoCoverage")]
    public class BlizzardArcadeCollection
    {
        private ComparisonResult _results;

        [OneTimeSetUp]
        public async Task Setup()
        {
            // Run the download process only once
            var debugConfig = new DebugConfig { UseCdnDebugMode = true, CompareAgainstRealRequests = true };
            var tactProductHandler = new TactProductHandler(TactProduct.BlizzardArcadeCollection, new TestConsole(), debugConfig: debugConfig);
            _results = await tactProductHandler.ProcessProductAsync(forcePrefill: true);
        }

        [Test]
        public void Misses()
        {
            Assert.AreEqual(1, _results.MissCount);
        }

        [Test]
        public void MissedBandwidth()
        {
            var expected = ByteSize.FromMegaBytes(2);

            Assert.Less(_results.MissedBandwidth.Bytes, expected.Bytes);
        }

        [Test]
        public void WastedBandwidth()
        {
            var expected = ByteSize.FromMegaBytes(2);

            Assert.Less(_results.WastedBandwidth.Bytes, expected.Bytes);
        }
    }
}
