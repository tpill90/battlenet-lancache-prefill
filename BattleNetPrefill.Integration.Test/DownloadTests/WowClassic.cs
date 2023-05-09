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
            var debugConfig = new DebugConfig { UseCdnDebugMode = true, CompareAgainstRealRequests = true };
            var tactProductHandler = new TactProductHandler(TactProduct.WowClassic, new TestConsole(), debugConfig: debugConfig);
            _results = await tactProductHandler.ProcessProductAsync(forcePrefill: true);
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
