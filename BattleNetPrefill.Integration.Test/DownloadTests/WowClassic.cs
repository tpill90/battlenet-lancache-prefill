namespace BattleNetPrefill.Integration.Test.DownloadTests
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    [ExcludeFromCodeCoverage, Category("SkipCI")]
    public class WowClassic
    {
        private ComparisonResult _results;

        [OneTimeSetUp]
        public async Task Setup()
        {
            // Run the download process only once
            AppConfig.CompareAgainstRealRequests = true;
            var tactProductHandler = new TactProductHandler(new TestConsole(), forcePrefill: true);
            _results = await tactProductHandler.ProcessProductAsync(TactProduct.WowClassic);
        }

        [Test]
        public void MissedBandwidth()
        {
            var expected = ByteSize.FromMegaBytes(30);

            Assert.Less(_results.MissedBandwidth.Bytes, expected.Bytes);
        }

        //TODO figure out why this is so high
        [Test]
        public void WastedBandwidth()
        {
            var expected = ByteSize.FromMegaBytes(115);

            Assert.Less(_results.WastedBandwidth.Bytes, expected.Bytes);
        }
    }
}
