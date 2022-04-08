using Konsole;
using NUnit.Framework;

namespace BuildBackup.Test.ActualDownloadTests
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class BlizzardDownloadTests
    {
        [Test]
        public void Hearthstone_HasNoMisses()
        {
            var results = Program.ProcessProduct(TactProducts.Hearthstone, new MockConsole(120, 50), true);
            Assert.AreEqual(0, results.MissCount);
        }
        
        [Test]
        public void Overwatch_HasNoMisses()
        {
            var results = Program.ProcessProduct(TactProducts.Overwatch, new MockConsole(120, 50), true);
            Assert.AreEqual(0, results.MissCount);
        }

        [Test]
        public void WowClassic_HasNoMisses()
        {
            var results = Program.ProcessProduct(TactProducts.WowClassic, new MockConsole(120, 50), true);
            Assert.AreEqual(0, results.MissCount);
        }
    }
}
