using System;
using System.Diagnostics;
using BuildBackup.DebugUtil;
using Konsole;

namespace BuildBackup
{
    //TODO figure out why Roslyn analyzers are complaining with a bunch of warnings
    //TODO Readme.md needs to be heavily updated.  Needs documentation on what this program does, how to use it, how to compile it, acknowledgements, external docs, etc.
    //TODO Repo - Github repo needs to be renamed, something like BattleNet-Preloader.
    //TODO Repo - Squash old commits + generally cleanup repo history
    //TODO Uncached performance - Improve uncached performance of all applications
    //TODO Performance - Improve overall performance of Overwatch, Wow
    public class Program
    {
        private static TactProduct[] ProductsToProcess = new[]
        {
            //TODO improve performance of these products
            //TactProducts.Diablo3,
            //TactProducts.HeroesOfTheStorm,
            //TactProducts.Overwatch,
            //TactProducts.Starcraft2,
            TactProducts.WorldOfWarcraft,
            //TactProducts.WowClassic
        };

        //TODO extract to config file
        public static bool UseCdnDebugMode = true;
        public static bool WriteOutputFiles = false;
        public static bool ShowDebugStats = false;

        public static void Main()
        {
            foreach (var product in ProductsToProcess)
            {
                //ProductHandler.ProcessProduct(product, new Writer(), UseCdnDebugMode, WriteOutputFiles, ShowDebugStats);
                BenchmarkUtil.Benchmark(product);
            }
            Console.WriteLine("Pre-load Complete!\n");

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press any key to continue . . .");
                Console.ReadLine();
            }
        }
    }
}
