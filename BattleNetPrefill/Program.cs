using System;
using System.Diagnostics;
using Spectre.Console;

namespace BattleNetPrefill
{
    //TODO Add more analyzers
    //TODO Readme.md needs to be heavily updated.  Needs documentation on what this program does, how to use it, how to compile it, acknowledgements, external docs, etc.
    //TODO Repo - Github repo needs to be renamed, something like BattleNet-Preloader.
    //TODO Repo - Squash old commits + generally cleanup repo history
    //TODO consider creating a flag/option, to not write any data to the cache, and to not use the cache.  Would be useful for saving disk space, or testing performance of an uncached run.
    //TODO add documentation on how to add unicode support to windows https://spectreconsole.net/best-practices
    public class Program
    {
        private static TactProduct[] ProductsToProcess = new TactProduct[]
        {
            TactProducts.CodBOCW,
            TactProducts.CodWarzone,
            TactProducts.CodVanguard,
            TactProducts.Diablo3,
            TactProducts.Hearthstone,
            TactProducts.HeroesOfTheStorm,
            TactProducts.Overwatch,
            TactProducts.Starcraft1,
            TactProducts.Starcraft2,
            TactProducts.WorldOfWarcraft,
            TactProducts.WowClassic
        };

        //TODO extract to config file
        public static bool SkipDiskCache = false;

        public static bool UseCdnDebugMode = false;
        public static bool ShowDebugStats = false;

        public static bool WriteOutputFiles = false;

        public static void Main()
        {
            foreach (var product in ProductsToProcess)
            {
                AnsiConsoleSettings consoleSettings = new AnsiConsoleSettings();
                TactProductHandler.ProcessProduct(product, AnsiConsole.Create(consoleSettings), UseCdnDebugMode, WriteOutputFiles, ShowDebugStats, SkipDiskCache);

                //BenchmarkUtil.Benchmark(product);
            }

            if (Debugger.IsAttached)
            {
                AnsiConsole.WriteLine("Press any key to continue . . .");
                Console.ReadLine();
            }
            AnsiConsole.WriteLine("Done!");
        }
    }
}
