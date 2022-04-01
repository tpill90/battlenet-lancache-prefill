using BattleNetPrefill.Structs;
using NUnit.Framework;

namespace BattleNetPrefill.Test.LogFileLatestVersionTests
{
    //TODO comment
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class ActivisionLogFileTests
    {
        [Test]
        public void CodBlackOpsColdWar_UpToDate()
        {
            var product = TactProduct.CodBOCW;

            VersionsEntry cdnVersion = LogFileTestUtil.GetLatestCdnVersion(product);
            var latestLogFile = LogFileTestUtil.GetLatestLogFileVersion(product);

            Assert.AreEqual(cdnVersion.versionsName, latestLogFile);
        }

        [Test]
        public void CodWarzone_UpToDate()
        {
            var product = TactProduct.CodWarzone;

            VersionsEntry cdnVersion = LogFileTestUtil.GetLatestCdnVersion(product);
            var latestLogFile = LogFileTestUtil.GetLatestLogFileVersion(product);

            Assert.AreEqual(cdnVersion.versionsName, latestLogFile);
        }

        [Test]
        public void CodVanguard_UpToDate()
        {
            var product = TactProduct.CodVanguard;

            VersionsEntry cdnVersion = LogFileTestUtil.GetLatestCdnVersion(product);
            var latestLogFile = LogFileTestUtil.GetLatestLogFileVersion(product);

            Assert.AreEqual(cdnVersion.versionsName, latestLogFile);
        }
    }
}
