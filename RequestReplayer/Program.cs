using System;
using System.Diagnostics;
using System.Linq;
using BuildBackup;
using BuildBackup.DebugUtil;
using Colors = Shared.Colors;

namespace RequestReplayer
{
    public static class Program
    {
        private static readonly string BlizzardCdnBaseUri = "http://level3.blizzard.com";
        private static readonly string LogFileBasePath = @"C:\Users\Tim\Dropbox\Programming\dotnet-public\BattleNetBackup\RequestReplayer\Logs";

        private static readonly TactProduct[] TargetProducts = 
        {
            // Activision
            TactProducts.CodBlackOpsColdWar,
            TactProducts.CodWarzone,
            TactProducts.CodVanguard,
            // Blizzard
            TactProducts.Diablo3,
            //TODO estimated download time left is displayed wrong
            TactProducts.Hearthstone,
            TactProducts.HeroesOfTheStorm,
            TactProducts.Overwatch,
            TactProducts.Starcraft1,
            TactProducts.Starcraft2,
            TactProducts.WorldOfWarcraft,
            TactProducts.WowClassic
        };

        //TODO do a version check to see if the logs are out of date
        //TODO figure out why some games have errors when replaying
        public static void Main()
        {
            foreach (var targetProduct in TargetProducts)
            {
                string replayLogVersion = NginxLogParser.GetLatestLogVersionForProduct(LogFileBasePath, targetProduct);
                Console.WriteLine($"Replaying requests for {Colors.Cyan(targetProduct.DisplayName)} {Colors.Yellow(replayLogVersion)}!");

                var timer = Stopwatch.StartNew();
                var requestsToReplay = NginxLogParser.ParseRequestLogs(LogFileBasePath, targetProduct).ToList();
                timer.Stop();
                Console.WriteLine($"Parsed request logs in {Colors.Yellow(timer.Elapsed.ToString(@"ss\.FFFF"))}");

                var downloader = new Downloader(BlizzardCdnBaseUri);
                downloader.DownloadRequestsParallel(requestsToReplay);
                downloader.PrintStatistics();

                Console.WriteLine();
            }

            Console.WriteLine("Done!");
            Console.ReadLine();
        }
    }
}