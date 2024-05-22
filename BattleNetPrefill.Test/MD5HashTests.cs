using Benchmarks.Implementations;

namespace BattleNetPrefill.Test
{
    [TestFixture]
    public class MD5HashTests
    {
        [Test]
        [TestCase(6051113216891152126UL, 49107880105117937UL, "FEE6771C69DFF953F18856555B77AE00")]
        [TestCase(8298574650443404551UL, 5955211075102420685UL, "0795BD45AD752A73CDAEF3B5EB28A552")]
        [TestCase(7043859728839941717UL, 17617057390324886482UL, "551E2F791AD1C061D22BFC795B5C7CF4")]
        [TestCase(8794752618514365567UL, 2740871278436135544UL, "7F3CFCF1E83C0D7A78B241EEE7870926")]
        public void ToString_ProducesCorrectResults(ulong lowPart, ulong highPart, string expected)
        {
            var hash = new MD5Hash(lowPart, highPart).ToString();
            Assert.AreEqual(expected, hash);
        }
    }
}
