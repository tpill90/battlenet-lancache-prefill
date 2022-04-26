using System.Linq;
using System.Threading.Tasks;
using BattleNetPrefill.Utils.Debug.Models;
using ByteSizeLib;
using NUnit.Framework;
using Spectre.Console.Testing;

namespace BattleNetPrefill.Test.DownloadTests.Blizzard
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class HeroesOfTheStorm
    {
        private ComparisonResult _results;

        [OneTimeSetUp]
        public async Task Setup()
        {
            // Run the download process only once
            _results = await TactProductHandler.ProcessProductAsync(TactProduct.HeroesOfTheStorm, new TestConsole(), useDebugMode: true, showDebugStats: true);
        }

        [Test]
        public void Misses()
        {
            Assert.AreEqual(0, _results.MissCount);
        }

        [Test]
        public void MissedBandwidth()
        {
            var missedBandwidth = ByteSize.FromBytes(_results.Misses.Sum(e => e.TotalBytes));
            Assert.AreEqual(0, missedBandwidth.Bytes);
        }

        [Test]
        public void WastedBandwidth()
        {
            var wastedBandwidth = ByteSize.FromBytes(_results.UnnecessaryRequests.Sum(e => e.TotalBytes));
            Assert.AreEqual(0, wastedBandwidth.Bytes);
        }
    }
}
