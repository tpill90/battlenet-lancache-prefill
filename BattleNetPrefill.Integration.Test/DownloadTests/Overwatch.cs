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
            var expected = ByteSize.FromMegaBytes(1);
            Assert.Less(_results.MissedBandwidth.Bytes, expected.Bytes);
        }

        [Test]
        public void WastedBandwidth()
        {
            //TODO - Way higher than this should ever be, however wasted bandwidth isn't nearly as a bad as outright missing requests
            var expected = ByteSize.FromMegaBytes(1000);
            Assert.Less(_results.WastedBandwidth.Bytes, expected.Bytes);
        }
    }
}