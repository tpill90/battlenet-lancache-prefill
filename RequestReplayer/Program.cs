using System;
using System.Linq;
using BattleNetPrefill;
using BattleNetPrefill.DebugUtil;
using Spectre.Console;
using Colors = BattleNetPrefill.Utils.Colors;

namespace RequestReplayer
{
    public static class Program
    {
        private static readonly string BlizzardCdnBaseUri = "http://level3.blizzard.com";
        private static readonly string LogFileBasePath = @"C:\Users\Tim\Dropbox\Programming\dotnet-public\BattleNetBackup\RequestReplayer\Logs";

        private static readonly TactProduct[] TargetProducts = 
        {
            // Activision
            TactProduct.CodBOCW,
            TactProduct.CodWarzone,
            TactProduct.CodVanguard,
            // Blizzard
            TactProduct.Diablo3,
            TactProduct.Hearthstone,
            TactProduct.HeroesOfTheStorm,
            TactProduct.Overwatch,
            TactProduct.Starcraft1,
            TactProduct.Starcraft2,
            TactProduct.WorldOfWarcraft,
            TactProduct.WowClassic
        };
        
        public static void Main()
        {
            foreach (var targetProduct in TargetProducts)
            {
                string replayLogVersion = NginxLogParser.GetLatestLogVersionForProduct(LogFileBasePath, targetProduct);
                AnsiConsole.WriteLine($"Replaying requests for {Colors.Cyan(targetProduct.DisplayName)} {Colors.Yellow(replayLogVersion)}!");
                
                var requestsToReplay = NginxLogParser.GetSavedRequestLogs(LogFileBasePath, targetProduct).ToList();

                var downloader = new Downloader(BlizzardCdnBaseUri);
                downloader.DownloadRequestsParallel(requestsToReplay);
                downloader.PrintStatistics();

                AnsiConsole.WriteLine();
            }

            AnsiConsole.WriteLine("Done!");
            Console.ReadLine();
        }
    }
}