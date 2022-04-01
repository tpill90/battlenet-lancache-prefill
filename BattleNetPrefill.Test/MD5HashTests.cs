using BattleNetPrefill.Structs;
using NUnit.Framework;

namespace BattleNetPrefill.Test
{
    //TODO comment
    [TestFixture]
    public class MD5HashTests
    {
        //TODO add a few more test cases
        [Test]
        public void ToString_ProducesCorrectResults()
        {
            var hash = new MD5Hash(6051113216891152126L, 49107880105117937L).ToString();
            Assert.AreEqual("FEE6771C69DFF953F18856555B77AE00", hash);
        }
    }
}
