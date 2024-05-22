namespace BattleNetPrefill.Integration.Test.DownloadTests
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    [ExcludeFromCodeCoverage, Category("SkipCI")]
    public class WorldOfWarcraft
    {
        private ComparisonResult _results;

        [OneTimeSetUp]
        public async Task Setup()
        {
            // Run the download process only once
            AppConfig.CompareAgainstRealRequests = true;
            var tactProductHandler = new TactProductHandler(new TestConsole(), forcePrefill: true);
            _results = await tactProductHandler.ProcessProductAsync(TactProduct.WorldOfWarcraft);
        }

        [Test]
        public void MissedBandwidth()
        {
            var expected = ByteSize.FromMegaBytes(200);
            //TODO figure out why this is so high
            Assert.Less(_results.MissedBandwidth.Bytes, expected.Bytes);
        }

        [Test]
        public void WastedBandwidth()
        {
            //TODO figure out why this is so high
            var expected = ByteSize.FromMegaBytes(2500);

            Assert.Less(_results.WastedBandwidth.Bytes, expected.Bytes);
        }
    }
}
