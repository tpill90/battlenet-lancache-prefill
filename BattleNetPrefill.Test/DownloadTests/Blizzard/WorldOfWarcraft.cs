using System.Threading.Tasks;
using BattleNetPrefill.Utils.Debug.Models;
using ByteSizeLib;
using NUnit.Framework;
using Spectre.Console.Testing;

namespace BattleNetPrefill.Test.DownloadTests.Blizzard
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class WorldOfWarcraft
    {
        private ComparisonResult _results;

        [OneTimeSetUp]
        public async Task Setup()
        {
            // Run the download process only once
            var debugConfig = new DebugConfig { UseCdnDebugMode = true, CompareAgainstRealRequests = true };
            _results = await TactProductHandler.ProcessProductAsync(TactProduct.WorldOfWarcraft, new TestConsole(), debugConfig: debugConfig);
        }

        [Test]
        public void Misses()
        {
            //TODO improve
            var expected = 13;
            Assert.LessOrEqual(_results.MissCount, expected);
        }

        [Test]
        public void MissedBandwidth()
        {
            //TODO improve this
            var expected = ByteSize.FromMegaBytes(6);
            
            Assert.Less(_results.MissedBandwidth.Bytes, expected.Bytes);
        }

        [Test]
        public void WastedBandwidth()
        {
            //TODO improve this
            var expected = ByteSize.FromMegaBytes(700);

            Assert.Less(_results.WastedBandwidth.Bytes, expected.Bytes);
        }
    }
}
