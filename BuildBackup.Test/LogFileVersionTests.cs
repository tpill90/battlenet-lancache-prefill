using System;
using System.IO;
using System.Linq;
using Shared;
using Xunit;

namespace BuildBackup.Test
{
    public class LogFileVersionTests
    {
        private static readonly Uri baseUrl = new Uri("http://us.patch.battle.net:1119/");
        private static readonly string LogFileBasePath = @"C:\Users\Tim\Dropbox\Programming\dotnet-public\BattleNetBackup\RequestReplayer\Logs";

        //TODO comment
        [Fact]
        public void CallOfDutyWarzone_UpToDate()
        {
            var product = TactProducts.CodWarzone;

            VersionsEntry cdnVersion = GetLatestCdnVersion(product);
            var latestLogFile = GetLatestLogFileVersion(product);

            Assert.Equal(cdnVersion.versionsName, latestLogFile);
        }

        //TODO comment
        [Fact]
        public void Diablo3_UpToDate()
        {
            var product = TactProducts.Diablo3;

            VersionsEntry cdnVersion = GetLatestCdnVersion(product);
            var latestLogFile = GetLatestLogFileVersion(product);

            Assert.Equal(cdnVersion.versionsName, latestLogFile);
        }

        [Fact]
        public void Hearthstone_UpToDate()
        {
            var product = TactProducts.Hearthstone;

            VersionsEntry cdnVersion = GetLatestCdnVersion(product);
            var latestLogFile = GetLatestLogFileVersion(product);

            Assert.Equal(cdnVersion.versionsName, latestLogFile);
        }

        [Fact]
        public void HeroesOfTheStorm_UpToDate()
        {
            var product = TactProducts.HerosOfTheStorm;

            VersionsEntry cdnVersion = GetLatestCdnVersion(product);
            var latestLogFile = GetLatestLogFileVersion(product);

            Assert.Equal(cdnVersion.versionsName, latestLogFile);
        }

        //TODO comment
        [Fact]
        public void Starcraft1_UpToDate()
        {
            var product = TactProducts.Starcraft1;

            VersionsEntry cdnVersion = GetLatestCdnVersion(product);
            var latestLogFile = GetLatestLogFileVersion(product);

            Assert.Equal(cdnVersion.versionsName, latestLogFile);
        }

        //TODO comment
        [Fact]
        public void Starcraft2_UpToDate()
        {
            var product = TactProducts.Starcraft2;

            VersionsEntry cdnVersion = GetLatestCdnVersion(product);
            var latestLogFile = GetLatestLogFileVersion(product);

            Assert.Equal(cdnVersion.versionsName, latestLogFile);
        }

        //TODO comment
        [Fact]
        public void Overwatch_UpToDate()
        {
            var product = TactProducts.Overwatch;

            VersionsEntry cdnVersion = GetLatestCdnVersion(product);
            var latestLogFile = GetLatestLogFileVersion(product);

            Assert.Equal(cdnVersion.versionsName, latestLogFile);
        }

        //TODO comment
        [Fact]
        public void WowClassic_UpToDate()
        {
            var product = TactProducts.WowClassic;

            VersionsEntry cdnVersion = GetLatestCdnVersion(product);
            var latestLogFile = GetLatestLogFileVersion(product);

            Assert.Equal(cdnVersion.versionsName, latestLogFile);
        }

        private static string GetLatestLogFileVersion(TactProduct product)
        {
            // Finding the most recent log file version
            var latestLogFile = new DirectoryInfo($@"{LogFileBasePath}\{product.DisplayName}")
                .GetFiles()
                .Where(e => !e.Name.Contains("coalesced"))
                .OrderByDescending(e => e.LastWriteTime)
                .FirstOrDefault();
            return latestLogFile.Name.Replace(".log", "");
        }

        private static VersionsEntry GetLatestCdnVersion(TactProduct product)
        {
            // Finding the latest version of the game
            Logic logic = new Logic(new CDN(), baseUrl);
            VersionsEntry cdnVersion = logic.GetVersionEntry(product);
            return cdnVersion;
        }
    }
}
