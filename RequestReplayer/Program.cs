using System;
using System.Linq;
using BattleNetPrefill;
using BattleNetPrefill.Utils.Debug;
using Spectre.Console;
using static BattleNetPrefill.Utils.SpectreColors;

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
                AnsiConsole.MarkupLine($"Replaying requests for {Cyan(targetProduct.DisplayName)} {Yellow(replayLogVersion)}!");
                
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