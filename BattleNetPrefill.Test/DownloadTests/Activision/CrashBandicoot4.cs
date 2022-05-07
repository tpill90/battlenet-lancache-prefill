using System.Threading.Tasks;
using BattleNetPrefill.Utils.Debug.Models;
using ByteSizeLib;
using NUnit.Framework;
using Spectre.Console.Testing;

namespace BattleNetPrefill.Test.DownloadTests.Activision
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class CrashBandicoot4
    {
        private ComparisonResult _results;

        [OneTimeSetUp]
        public async Task Setup()
        {
            // Run the download process only once
            var debugConfig = new DebugConfig { UseCdnDebugMode = true, CompareAgainstRealRequests = true };
            var tactProductHandler = new TactProductHandler(TactProduct.CrashBandicoot4, new TestConsole(), debugConfig: debugConfig);
            _results = await tactProductHandler.ProcessProductAsync(forcePrefill: true);
        }

        [Test]
        public void Misses()
        {
            Assert.AreEqual(0, _results.MissCount);
        }

        [Test]
        public void MissedBandwidth()
        {
            Assert.AreEqual(0, _results.MissedBandwidth.Bytes);
        }

        [Test]
        public void WastedBandwidth()
        {
            //TODO improve this
            Assert.Less(_results.WastedBandwidth.Bytes, ByteSize.FromMegaBytes(1).Bytes);
        }
    }
}
