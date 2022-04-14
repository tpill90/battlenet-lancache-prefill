using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
using BuildBackup.DebugUtil;
using BuildBackup.Structs;
using ByteSizeLib;
using Konsole;

namespace BuildBackup
{
    //TODO figure out why Roslyn analyzers are complaining with a bunch of warnings
    //TODO Readme.md needs to be heavily updated.  Needs documentation on what this program does, how to use it, how to compile it, acknowledgements, external docs, etc.
    //TODO Repo - Github repo needs to be renamed, something like BattleNet-Preloader.
    //TODO Repo - Squash old commits + generally cleanup repo history
    //TODO Uncached performance - Improve uncached performance of all applications
    //TODO Performance - Improve overall performance of Overwatch, Wow
    //TODO Performance - Improve overall performance of parsing archives
	//TODO Reduce the number of overall allocations
	//TODO consider creating a flag/option, to not write any data to the cache, and to not use the cache.  Would be useful for saving disk space, or testing performance of an uncached run.
    public class Program
    {
        private static TactProduct[] ProductsToProcess = new TactProduct[]
        {
            //TactProducts.CodBlackOpsColdWar,
            //TactProducts.CodWarzone,
            //TactProducts.CodVanguard,
            //TactProducts.Diablo3,
            //TactProducts.Hearthstone,
            //TactProducts.HeroesOfTheStorm,
            //TactProducts.Overwatch,
            //TactProducts.Starcraft1,
            //TactProducts.Starcraft2,
            TactProducts.WorldOfWarcraft,
            //TactProducts.WowClassic
        };

        //TODO extract to config file
        public static bool UseCdnDebugMode = true;
        public static bool ShowDebugStats = false;

        public static bool WriteOutputFiles = false;

        public static void Main()
        {
            //BenchmarkMD5Hashes();

            foreach (var product in ProductsToProcess)
            {
                ProductHandler.ProcessProduct(product, new Writer(), UseCdnDebugMode, WriteOutputFiles, ShowDebugStats);

                BenchmarkUtil.Benchmark(product);
            }
            Console.WriteLine("Pre-load Complete!\n");

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press any key to continue . . .");
                Console.ReadLine();
            }
        }

        private static void BenchmarkMD5Hashes()
        {
            int count = 10000000;

            var random = new Random();
            var hashList = new List<MD5Hash>();
            Stopwatch timer;

            // Prepopulating
            timer = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                hashList.Add(new MD5Hash((ulong) random.NextInt64(), (ulong) random.NextInt64()));
            }

            timer.Stop();
            Console.WriteLine($"Prepopulated in {timer.Elapsed}");

            // Warmup
            timer = Stopwatch.StartNew();
            Console.WriteLine("Warmup");
            foreach (var hash in hashList)
            {
                hash.ToStringOld();
                hash.ToStringNew();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Console.WriteLine($"Warmup done in {timer.Elapsed}");

            timer = Stopwatch.StartNew();
            foreach (var hash in hashList)
            {
                hash.ToStringOld();
            }

            Console.WriteLine($"Old done in {timer.Elapsed}");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            timer = Stopwatch.StartNew();
            foreach (var hash in hashList)
            {
                hash.ToStringNew();
            }

            Console.WriteLine($"New done in {timer.Elapsed}");
        }
    }
}
