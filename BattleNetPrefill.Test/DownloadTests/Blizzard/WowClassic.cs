using System.Threading.Tasks;
using BattleNetPrefill.Utils.Debug.Models;
using ByteSizeLib;
using NUnit.Framework;
using Spectre.Console.Testing;

namespace BattleNetPrefill.Test.DownloadTests.Blizzard
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class WowClassic
    {
        private ComparisonResult _results;

        [OneTimeSetUp]
        public async Task Setup()
        {
            // Run the download process only once
            var debugConfig = new DebugConfig { UseCdnDebugMode = true, CompareAgainstRealRequests = true };
            _results = await TactProductHandler.ProcessProductAsync(TactProduct.WowClassic, new TestConsole(), debugConfig: debugConfig);
        }

        [Test]
        public void Misses()
        {
            //TODO improve
            var expected = 8;
            Assert.LessOrEqual(_results.MissCount, expected);
        }

        [Test]
        public void MissedBandwidth()
        {
            //TODO improve this
            var expected = ByteSize.FromMegaBytes(7);

            Assert.Less(_results.MissedBandwidth.Bytes, expected.Bytes);
        }

        [Test]
        public void WastedBandwidth()
        {
            //TODO improve this
            var expected = ByteSize.FromMegaBytes(60);

            Assert.Less(_results.WastedBandwidth.Bytes, expected.Bytes);
        }
    }
}
