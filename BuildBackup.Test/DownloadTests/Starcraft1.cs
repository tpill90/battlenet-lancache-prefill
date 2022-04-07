using System.Linq;
using ByteSizeLib;
using Konsole;
using NUnit.Framework;
using Shared;
using Shared.Models;

namespace BuildBackup.Test.DownloadTests
{
    public class Starcraft1
    {
        private ComparisonResult _results;

        [OneTimeSetUp]
        public void Setup()
        {
            // Run the download process only once
            _results = Program.ProcessProduct(TactProducts.Starcraft1, new MockConsole(120, 50), true);
        }

        [Test]
        public void MissedBandwidth()
        {
            var expected = ByteSize.FromMegaBytes(1);

            var missedBandwidth = ByteSize.FromBytes(_results.Misses.Sum(e => e.TotalBytes));
            Assert.Less(missedBandwidth.Bytes, expected.Bytes);
        }

        [Test]
        public void WastedBandwidth()
        {
            //TODO improve this
            var expected = ByteSize.FromMegaBytes(700);

            var wastedBandwidth = ByteSize.FromBytes(_results.UnnecessaryRequests.Sum(e => e.TotalBytes));
            Assert.Less(wastedBandwidth.Bytes, expected.Bytes);
        }

        [Test]
        public void Misses()
        {
            Assert.AreEqual(0, _results.MissCount);
        }
    }
}
