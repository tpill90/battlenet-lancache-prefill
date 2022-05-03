using BattleNetPrefill.Structs;
using NUnit.Framework;

namespace BattleNetPrefill.Test
{
    [TestFixture]
    public class MD5HashTests
    {
        [Test]
        [TestCase(6051113216891152126UL,    49107880105117937UL, "FEE6771C69DFF953F18856555B77AE00")]
        [TestCase(8298574650443404551UL,  5955211075102420685UL, "0795bd45ad752a73cdaef3b5eb28a552")]
        [TestCase(7043859728839941717UL, 17617057390324886482UL, "551e2f791ad1c061d22bfc795b5c7cf4")]
        [TestCase(8794752618514365567UL,  2740871278436135544UL, "7f3cfcf1e83c0d7a78b241eee7870926")]
        public void ToString_ProducesCorrectResults(ulong lowPart, ulong highPart, string expected)
        {
            var hash = new MD5Hash(lowPart, highPart).ToString();
            Assert.AreEqual(expected.ToUpper(), hash);
        }
    }
}
