using System.Linq;
using BuildBackup.DebugUtil.Models;
using ByteSizeLib;
using Konsole;
using NUnit.Framework;

namespace BuildBackup.Test.DownloadTests.Blizzard
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class WowClassic
    {
        private ComparisonResult _results;

        [OneTimeSetUp]
        public void Setup()
        {
            // Run the download process only once
            _results = Program.ProcessProduct(TactProducts.WowClassic, new MockConsole(120, 50), true, writeOutputFiles: false);
        }

        [Test]
        public void Misses()
        {
            //TODO improve this
            Assert.LessOrEqual(_results.MissCount, 3);
        }

        [Test]
        public void MissedBandwidth()
        {
            //TODO improve this
            var missedBandwidth = ByteSize.FromBytes(_results.Misses.Sum(e => e.TotalBytes));
            Assert.Less(missedBandwidth.Bytes, ByteSize.FromMegaBytes(2).Bytes);
        }

        [Test]
        public void WastedBandwidth()
        {
            //TODO improve this
            var expected = ByteSize.FromMegaBytes(1600);

            var wastedBandwidth = ByteSize.FromBytes(_results.UnnecessaryRequests.Sum(e => e.TotalBytes));
            Assert.Less(wastedBandwidth.Bytes, expected.Bytes);
        }
    }
}
