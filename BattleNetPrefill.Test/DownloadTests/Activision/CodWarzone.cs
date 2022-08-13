using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using BattleNetPrefill.Utils.Debug.Models;
using ByteSizeLib;
using NUnit.Framework;
using Spectre.Console.Testing;

namespace BattleNetPrefill.Test.DownloadTests.Activision
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    [ExcludeFromCodeCoverage, Category("NoCoverage")]
    public class CodWarzone
    {
        private ComparisonResult _results;

        [OneTimeSetUp]
        public async Task Setup()
        {
            // Run the download process only once
            var debugConfig = new DebugConfig { UseCdnDebugMode = true, CompareAgainstRealRequests = true };
            var tactProductHandler = new TactProductHandler(TactProduct.CodWarzone, new TestConsole(), debugConfig: debugConfig);
            _results = await tactProductHandler.ProcessProductAsync(forcePrefill: true);
        }

        [Test]
        public void Misses()
        {
            var expected = 6;
            Assert.LessOrEqual(_results.MissCount, expected);
        }

        [Test]
        public void MissedBandwidth()
        {
            var expected = ByteSize.FromMegaBytes(3);
            Assert.LessOrEqual(_results.MissedBandwidth.Bytes, expected.Bytes);
        }

        [Test]
        public void WastedBandwidth()
        {
            Assert.AreEqual(0, _results.WastedBandwidth.Bytes);
        }
    }
}
