namespace BattleNetPrefill.Integration.Test.DownloadTests
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    [ExcludeFromCodeCoverage, Category("SkipCI")]
    public class Starcraft1
    {
        private ComparisonResult _results;

        [OneTimeSetUp]
        public async Task Setup()
        {
            // Run the download process only once
            AppConfig.CompareAgainstRealRequests = true;
            var tactProductHandler = new TactProductHandler(new TestConsole(), forcePrefill: true);
            _results = await tactProductHandler.ProcessProductAsync(TactProduct.Starcraft1);
        }

        [Test]
        public void Misses()
        {
            Assert.LessOrEqual(_results.MissCount, 0);
        }

        [Test]
        public void MissedBandwidth()
        {
            Assert.AreEqual(0, _results.MissedBandwidth.Bytes);
        }

        [Test]
        public void WastedBandwidth()
        {
            // Anything less than 1MiB is fine
            var expected = ByteSize.FromMegaBytes(1);
            Assert.Less(_results.MissedBandwidth.Bytes, expected.Bytes);
        }
    }
}
