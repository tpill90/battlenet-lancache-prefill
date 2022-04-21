using System.Linq;
using BattleNetPrefill.DebugUtil.Models;
using ByteSizeLib;
using NUnit.Framework;
using Spectre.Console.Testing;

namespace BattleNetPrefill.Test.DownloadTests.Activision
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class CodWarzone
    {
        private ComparisonResult _results;

        [OneTimeSetUp]
        public void Setup()
        {
            // Run the download process only once
            _results = TactProductHandler.ProcessProduct(TactProduct.CodWarzone, new TestConsole(), useDebugMode: true, showDebugStats: true);
        }

        [Test]
        public void Misses()
        {
            //TODO improve this
            Assert.LessOrEqual(_results.MissCount, 7);
        }

        [Test]
        public void MissedBandwidth()
        {
            //TODO improve this
            var missedBandwidth = ByteSize.FromBytes(_results.Misses.Sum(e => e.TotalBytes));
            Assert.Less(missedBandwidth.Bytes, ByteSize.FromMegaBytes(5).Bytes);
        }

        [Test]
        public void WastedBandwidth()
        {
            var wastedBandwidth = ByteSize.FromBytes(_results.UnnecessaryRequests.Sum(e => e.TotalBytes));
            Assert.AreEqual(0, wastedBandwidth.Bytes);
        }
    }
}
