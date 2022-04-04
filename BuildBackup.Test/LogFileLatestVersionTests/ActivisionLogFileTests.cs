using BuildBackup.Structs;
using NUnit.Framework;
using Shared;

namespace BuildBackup.Test.LogFileLatestVersionTests
{
    //TODO comment
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class ActivisionLogFileTests
    {
        [Test]
        public void CodBlackOpsColdWar_UpToDate()
        {
            var product = TactProducts.CodBlackOpsColdWar;

            VersionsEntry cdnVersion = LogFileTestUtil.GetLatestCdnVersion(product);
            var latestLogFile = LogFileTestUtil.GetLatestLogFileVersion(product);

            Assert.AreEqual(cdnVersion.versionsName, latestLogFile);
        }

        [Test]
        public void CallOfDutyWarzone_UpToDate()
        {
            var product = TactProducts.CodWarzone;

            VersionsEntry cdnVersion = LogFileTestUtil.GetLatestCdnVersion(product);
            var latestLogFile = LogFileTestUtil.GetLatestLogFileVersion(product);

            Assert.AreEqual(cdnVersion.versionsName, latestLogFile);
        }

        [Test]
        public void CodVanguard_UpToDate()
        {
            var product = TactProducts.CodVanguard;

            VersionsEntry cdnVersion = LogFileTestUtil.GetLatestCdnVersion(product);
            var latestLogFile = LogFileTestUtil.GetLatestLogFileVersion(product);

            Assert.AreEqual(cdnVersion.versionsName, latestLogFile);
        }
    }
}
