namespace BattleNetPrefill.Integration.Test.DownloadTests
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    [ExcludeFromCodeCoverage, Category("SkipCI")]
    public class Overwatch
    {
        private ComparisonResult _results;

        [OneTimeSetUp]
        public async Task Setup()
        {
            // Run the download process only once
            AppConfig.CompareAgainstRealRequests = true;
            var tactProductHandler = new TactProductHandler(new TestConsole(), forcePrefill: true);
            _results = await tactProductHandler.ProcessProductAsync(TactProduct.Overwatch2);
        }

        [Test]
        public void MissedBandwidth()
        {
            //TODO figure out why this is so high
            var expected = ByteSize.FromMegaBytes(1);

            Assert.Less(_results.MissedBandwidth.Bytes, expected.Bytes);
        }

        [Test]
        public void WastedBandwidth()
        {
            var expected = ByteSize.FromMegaBytes(30);

            Assert.Less(_results.WastedBandwidth.Bytes, expected.Bytes);
        }
    }
}