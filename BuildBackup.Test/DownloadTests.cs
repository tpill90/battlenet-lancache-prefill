using Konsole;
using Shared;
using Xunit;

namespace BuildBackup.Test
{
    public class DownloadTests
    {
        [Fact]
        public void CallOfDutyWarzone_HasNoMisses()
        {
            var results = Program.ProcessProduct(TactProducts.CodWarzone, new MockConsole(120, 50), true);
            Assert.Equal(0, results.MissCount);
            // Should have some hits
            Assert.NotEqual(0, results.HitCount);
        }

        [Fact]
        public void Diablo3_HasNoMisses()
        {
            var results = Program.ProcessProduct(TactProducts.Diablo3, new MockConsole(120, 50), true);
            Assert.Equal(0, results.MissCount);
            // Should have some hits
            Assert.NotEqual(0, results.HitCount);
        }

        [Fact]
        public void Starcraft1_HasNoMisses()
        {
            var results = Program.ProcessProduct(TactProducts.Starcraft1, new MockConsole(120, 50), true);
            Assert.Equal(0, results.MissCount);
            // Should have some hits
            Assert.NotEqual(0, results.HitCount);
        }

        [Fact]
        public void Starcraft2_HasNoMisses()
        {
            var results = Program.ProcessProduct(TactProducts.Starcraft2, new MockConsole(120, 50), true);
            Assert.Equal(0, results.MissCount);
            // Should have some hits
            Assert.NotEqual(0, results.HitCount);
        }

        [Fact]
        public void Hearthstone_HasNoMisses()
        {
            var results = Program.ProcessProduct(TactProducts.Hearthstone, new MockConsole(120, 50), true);
            Assert.Equal(0, results.MissCount);
            // Should have some hits
            Assert.NotEqual(0, results.HitCount);
        }

        [Fact]
        public void HerosOfTheStorm_HasNoMisses()
        {
            var results = Program.ProcessProduct(TactProducts.HerosOfTheStorm, new MockConsole(120, 50), true);
            Assert.Equal(0, results.MissCount);
            // Should have some hits
            Assert.NotEqual(0, results.HitCount);
        }

        //TODO reenable, its so slow
        //[Fact]
        //public void Overwatch_HasNoMisses()
        //{
        //    var results = Program.ProcessProduct(TactProducts.Overwatch, new MockConsole(120, 50), true);
        //    Assert.Equal(0, results.MissCount);
        //    // Should have some hits
        //    Assert.NotEqual(0, results.HitCount);
        //}
        //TODO reenable, its so slow
        //[Fact]
        //public void WowClassic_HasNoMisses()
        //{
        //    var results = Program.ProcessProduct(TactProducts.WowClassic, new MockConsole(120, 50), true);
        //    Assert.Equal(0, results.MissCount);
        //    // Should have some hits
        //    Assert.NotEqual(0, results.HitCount);
        //}
    }
}
