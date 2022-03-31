using Konsole;
using NUnit.Framework;
using Shared;

namespace BuildBackup.Test
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class BlizzardDownloadTests
    {
        [Test]
        public void Diablo3_HasNoMisses()
        {
            var results = Program.ProcessProduct(TactProducts.Diablo3, new MockConsole(120, 50), true);
            Assert.AreEqual(0, results.MissCount);
            // Should have some hits
            Assert.AreNotEqual(0, results.HitCount);
        }

        [Test]
        public void Hearthstone_HasNoMisses()
        {
            var results = Program.ProcessProduct(TactProducts.Hearthstone, new MockConsole(120, 50), true);
            Assert.AreEqual(0, results.MissCount);
            // Should have some hits
            Assert.AreNotEqual(0, results.HitCount);
        }

        [Test]
        public void HerosOfTheStorm_HasNoMisses()
        {
            var results = Program.ProcessProduct(TactProducts.HeroesOfTheStorm, new MockConsole(120, 50), true);
            Assert.AreEqual(0, results.MissCount);
            // Should have some hits
            Assert.AreNotEqual(0, results.HitCount);
        }

        [Test]
        public void Starcraft1_HasNoMisses()
        {
            var results = Program.ProcessProduct(TactProducts.Starcraft1, new MockConsole(120, 50), true);
            Assert.AreEqual(0, results.MissCount);
            // Should have some hits
            Assert.AreNotEqual(0, results.HitCount);
        }

        [Test]
        public void Starcraft2_HasNoMisses()
        {
            var results = Program.ProcessProduct(TactProducts.Starcraft2, new MockConsole(120, 50), true);
            Assert.AreEqual(0, results.MissCount);
            // Should have some hits
            Assert.AreNotEqual(0, results.HitCount);
        }

        [Test]
        public void Overwatch_HasNoMisses()
        {
            var results = Program.ProcessProduct(TactProducts.Overwatch, new MockConsole(120, 50), true);
            Assert.AreEqual(0, results.MissCount);
            // Should have some hits
            Assert.AreNotEqual(0, results.HitCount);
        }

        [Test]
        public void WowClassic_HasNoMisses()
        {
            var results = Program.ProcessProduct(TactProducts.WowClassic, new MockConsole(120, 50), true);
            Assert.AreEqual(0, results.MissCount);
            // Should have some hits
            Assert.AreNotEqual(0, results.HitCount);
        }
    }
}
