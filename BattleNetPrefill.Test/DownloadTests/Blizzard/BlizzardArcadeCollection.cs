using System.Threading.Tasks;
using BattleNetPrefill.Utils.Debug.Models;
using ByteSizeLib;
using NUnit.Framework;
using Spectre.Console.Testing;

namespace BattleNetPrefill.Test.DownloadTests.Blizzard
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class BlizzardArcadeCollection
    {
        private ComparisonResult _results;

        [OneTimeSetUp]
        public async Task Setup()
        {
            // Run the download process only once
            var debugConfig = new DebugConfig { UseCdnDebugMode = true, CompareAgainstRealRequests = true };
            _results = await TactProductHandler.ProcessProductAsync(TactProduct.BlizzardArcadeCollection, new TestConsole(), debugConfig: debugConfig);
        }

        [Test]
        public void Misses()
        {
            //TODO Improve
            Assert.AreEqual(1, _results.MissCount);
        }

        [Test]
        public void MissedBandwidth()
        {
            //TODO Improve
            var expected = ByteSize.FromMegaBytes(2);
            
            Assert.Less(_results.MissedBandwidth.Bytes, expected.Bytes);
        }

        [Test]
        public void WastedBandwidth()
        {
            //TODO Improve
            var expected = ByteSize.FromMegaBytes(2);

            Assert.Less(_results.WastedBandwidth.Bytes, expected.Bytes);
        }
    }
}
