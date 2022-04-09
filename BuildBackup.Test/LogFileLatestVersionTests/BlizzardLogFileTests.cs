using BuildBackup.Structs;
using NUnit.Framework;
namespace BuildBackup.Test.LogFileLatestVersionTests
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
            var product = TactProducts.Diablo3;

            VersionsEntry cdnVersion = LogFileTestUtil.GetLatestCdnVersion(product);
            var latestLogFile = LogFileTestUtil.GetLatestLogFileVersion(product);

            Assert.AreEqual(cdnVersion.versionsName, latestLogFile);
        }

        [Test]
        public void Hearthstone_UpToDate()
        {
            var product = TactProducts.Hearthstone;

            VersionsEntry cdnVersion = LogFileTestUtil.GetLatestCdnVersion(product);
            var latestLogFile = LogFileTestUtil.GetLatestLogFileVersion(product);

            Assert.AreEqual(cdnVersion.versionsName, latestLogFile);
        }

        [Test]
        public void HeroesOfTheStorm_UpToDate()
        {
            var product = TactProducts.HeroesOfTheStorm;

            VersionsEntry cdnVersion = LogFileTestUtil.GetLatestCdnVersion(product);
            var latestLogFile = LogFileTestUtil.GetLatestLogFileVersion(product);

            Assert.AreEqual(cdnVersion.versionsName, latestLogFile);
        }

        [Test]
        public void Starcraft1_UpToDate()
        {
            var product = TactProducts.Starcraft1;

            VersionsEntry cdnVersion = LogFileTestUtil.GetLatestCdnVersion(product);
            var latestLogFile = LogFileTestUtil.GetLatestLogFileVersion(product);

            Assert.AreEqual(cdnVersion.versionsName, latestLogFile);
        }

        [Test]
        public void Starcraft2_UpToDate()
        {
            var product = TactProducts.Starcraft2;

            VersionsEntry cdnVersion = LogFileTestUtil.GetLatestCdnVersion(product);
            var latestLogFile = LogFileTestUtil.GetLatestLogFileVersion(product);

            Assert.AreEqual(cdnVersion.versionsName, latestLogFile);
        }

        [Test]
        public void Overwatch_UpToDate()
        {
            var product = TactProducts.Overwatch;

            VersionsEntry cdnVersion = LogFileTestUtil.GetLatestCdnVersion(product);
            var latestLogFile = LogFileTestUtil.GetLatestLogFileVersion(product);

            Assert.AreEqual(cdnVersion.versionsName, latestLogFile);
        }

        [Test]
        public void WorldOfWarcraft_UpToDate()
        {
            var product = TactProducts.WorldOfWarcraft;

            VersionsEntry cdnVersion = LogFileTestUtil.GetLatestCdnVersion(product);
            var latestLogFile = LogFileTestUtil.GetLatestLogFileVersion(product);

            Assert.AreEqual(cdnVersion.versionsName, latestLogFile);
        }

        [Test]
        public void WowClassic_UpToDate()
        {
            var product = TactProducts.WowClassic;

            VersionsEntry cdnVersion = LogFileTestUtil.GetLatestCdnVersion(product);
            var latestLogFile = LogFileTestUtil.GetLatestLogFileVersion(product);

            Assert.AreEqual(cdnVersion.versionsName, latestLogFile);
        }
    }
}
