using System.Linq;
using BuildBackup.DebugUtil.Models;
using ByteSizeLib;
using NUnit.Framework;
using Spectre.Console.Testing;

namespace BuildBackup.Test.DownloadTests.Blizzard
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class Overwatch
    {
        private ComparisonResult _results;

        [OneTimeSetUp]
        public void Setup()
        {
            // Run the download process only once
            _results = ProductHandler.ProcessProduct(TactProducts.Overwatch, new TestConsole(), useDebugMode: true, showDebugStats: true);
        }

        [Test]
        public void Misses()
        {
            //TODO improve
            Assert.AreEqual(3, _results.MissCount);
        }

        [Test]
        public void MissedBandwidth()
        {
            //TODO improve
            var expected = ByteSize.FromMegaBytes(30).Bytes;

            var missedBandwidth = ByteSize.FromBytes(_results.Misses.Sum(e => e.TotalBytes));
            Assert.LessOrEqual(missedBandwidth.Bytes, expected);
        }

        [Test]
        public void WastedBandwidth()
        {
            //TODO improve this
            var expected = ByteSize.FromMegaBytes(50);

            var wastedBandwidth = ByteSize.FromBytes(_results.UnnecessaryRequests.Sum(e => e.TotalBytes));
            Assert.Less(wastedBandwidth.Bytes, expected.Bytes);
        }
    }
}
