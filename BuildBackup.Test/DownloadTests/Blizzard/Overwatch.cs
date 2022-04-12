using System.Linq;
using BuildBackup.DebugUtil.Models;
using ByteSizeLib;
using Konsole;
using NUnit.Framework;

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
            _results = Program.ProcessProduct(TactProducts.Overwatch, new MockConsole(120, 50), true, writeOutputFiles: false);
        }

        [Test]
        public void Misses()
        {
            //TODO improve
            Assert.AreEqual(1, _results.MissCount);
        }

        [Test]
        public void MissedBandwidth()
        {
            var missedBandwidth = ByteSize.FromBytes(_results.Misses.Sum(e => e.TotalBytes));
            Assert.LessOrEqual(missedBandwidth.Bytes, ByteSize.FromKiloBytes(10).Bytes);
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
