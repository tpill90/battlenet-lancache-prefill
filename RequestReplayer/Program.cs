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
            TactProducts.Hearthstone,
            TactProducts.HeroesOfTheStorm,
            TactProducts.Overwatch,
            TactProducts.Starcraft1,
            TactProducts.Starcraft2,
            TactProducts.WorldOfWarcraft,
            TactProducts.WowClassic
        };
        
        public static void Main()
        {
            foreach (var targetProduct in TargetProducts)
            {
                string replayLogVersion = NginxLogParser.GetLatestLogVersionForProduct(LogFileBasePath, targetProduct);
                Console.WriteLine($"Replaying requests for {Colors.Cyan(targetProduct.DisplayName)} {Colors.Yellow(replayLogVersion)}!");
                
                var requestsToReplay = NginxLogParser.GetSavedRequestLogs(LogFileBasePath, targetProduct).ToList();

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