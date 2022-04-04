using Konsole;
using NUnit.Framework;
using Shared;

namespace BuildBackup.Test
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class ActivisionDownloadTests
    {
        [Test]
        public void CallOfDutyBlackOpsColdWar_HasNoMisses()
        {
            var results = Program.ProcessProduct(TactProducts.CodBlackOpsColdWar, new MockConsole(120, 50), true);
            Assert.AreEqual(0, results.MissCount);
        }

        [Test]
        public void CallOfDutyWarzone_HasNoMisses()
        {
            var results = Program.ProcessProduct(TactProducts.CodWarzone, new MockConsole(120, 50), true);
            Assert.AreEqual(0, results.MissCount);
        }

        [Test]
        public void CallOfDutyVanguard_HasNoMisses()
        {
            var results = Program.ProcessProduct(TactProducts.CodVanguard, new MockConsole(120, 50), true);
            Assert.AreEqual(0, results.MissCount);
        }
    }
}
