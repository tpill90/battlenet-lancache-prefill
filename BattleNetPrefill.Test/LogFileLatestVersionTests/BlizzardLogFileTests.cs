using BattleNetPrefill.Structs;
using NUnit.Framework;

namespace BattleNetPrefill.Test.LogFileLatestVersionTests
{
    //TODO comment
    /// <summary>
    /// Tests to validate that the 
    /// </summary>
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class BlizzardLogFileTests
    {
        [Test]
        public void Diablo3_UpToDate()
        {
            var product = TactProduct.Diablo3;

            VersionsEntry cdnVersion = LogFileTestUtil.GetLatestCdnVersion(product);
            var latestLogFile = LogFileTestUtil.GetLatestLogFileVersion(product);

            Assert.AreEqual(cdnVersion.versionsName, latestLogFile);
        }

        [Test]
        public void Hearthstone_UpToDate()
        {
            var product = TactProduct.Hearthstone;

            VersionsEntry cdnVersion = LogFileTestUtil.GetLatestCdnVersion(product);
            var latestLogFile = LogFileTestUtil.GetLatestLogFileVersion(product);

            Assert.AreEqual(cdnVersion.versionsName, latestLogFile);
        }

        [Test]
        public void HeroesOfTheStorm_UpToDate()
        {
            var product = TactProduct.HeroesOfTheStorm;

            VersionsEntry cdnVersion = LogFileTestUtil.GetLatestCdnVersion(product);
            var latestLogFile = LogFileTestUtil.GetLatestLogFileVersion(product);

            Assert.AreEqual(cdnVersion.versionsName, latestLogFile);
        }

        [Test]
        public void Starcraft1_UpToDate()
        {
            var product = TactProduct.Starcraft1;

            VersionsEntry cdnVersion = LogFileTestUtil.GetLatestCdnVersion(product);
            var latestLogFile = LogFileTestUtil.GetLatestLogFileVersion(product);

            Assert.AreEqual(cdnVersion.versionsName, latestLogFile);
        }

        [Test]
        public void Starcraft2_UpToDate()
        {
            var product = TactProduct.Starcraft2;

            VersionsEntry cdnVersion = LogFileTestUtil.GetLatestCdnVersion(product);
            var latestLogFile = LogFileTestUtil.GetLatestLogFileVersion(product);

            Assert.AreEqual(cdnVersion.versionsName, latestLogFile);
        }

        [Test]
        public void Overwatch_UpToDate()
        {
            var product = TactProduct.Overwatch;

            VersionsEntry cdnVersion = LogFileTestUtil.GetLatestCdnVersion(product);
            var latestLogFile = LogFileTestUtil.GetLatestLogFileVersion(product);

            Assert.AreEqual(cdnVersion.versionsName, latestLogFile);
        }

        [Test]
        public void Warcraft3_UpToDate()
        {
            var product = TactProduct.Warcraft3Reforged;

            VersionsEntry cdnVersion = LogFileTestUtil.GetLatestCdnVersion(product);
            var latestLogFile = LogFileTestUtil.GetLatestLogFileVersion(product);

            Assert.AreEqual(cdnVersion.versionsName, latestLogFile);
        }

        [Test]
        public void WorldOfWarcraft_UpToDate()
        {
            var product = TactProduct.WorldOfWarcraft;

            VersionsEntry cdnVersion = LogFileTestUtil.GetLatestCdnVersion(product);
            var latestLogFile = LogFileTestUtil.GetLatestLogFileVersion(product);

            Assert.AreEqual(cdnVersion.versionsName, latestLogFile);
        }

        [Test]
        public void WowClassic_UpToDate()
        {
            var product = TactProduct.WowClassic;

            VersionsEntry cdnVersion = LogFileTestUtil.GetLatestCdnVersion(product);
            var latestLogFile = LogFileTestUtil.GetLatestLogFileVersion(product);

            Assert.AreEqual(cdnVersion.versionsName, latestLogFile);
        }
    }
}
