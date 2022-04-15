using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BuildBackup.DebugUtil;
using BuildBackup.Structs;
using BuildBackup.Utils;
using Konsole;
using Colors = Shared.Colors;

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
        public static bool ShowDebugStats = true;

        public static bool WriteOutputFiles = false;

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

        //TODO measure this with benchmark.net
        private static void Benchmark_MD5GetHashCode()
        {
            int numEntries = 4 * 1000 * 1000;

            var random = new Random();

            for (int i = 0; i < 1000; i++)
            {
                MD5GetHashCodeRun(i * 100000, random);
            }

            MD5GetHashCodeRun(numEntries, random);
        }

        private static void MD5GetHashCodeRun(int numEntries, Random random)
        {
            var inputHashList = new List<MD5Hash>();
            Stopwatch timer;

            // Prepopulating
            timer = Stopwatch.StartNew();
            for (long i = 0; i < numEntries; i++)
            {
                inputHashList.Add(new MD5Hash((ulong) random.NextInt64(), (ulong) random.NextInt64()));
            }

            timer.Stop();
            // Filling dictionary
            //var hashDict = new Dictionary<MD5Hash, long>(equalityComparer);
            //timer = Stopwatch.StartNew();
            //for (int i = 0; i < numEntries; i++)
            //{
            //    hashDict.Add(hashList[i], i);
            //}
            //Console.WriteLine($"Filled dict in {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");

            // Calculating collisions
            List<int> calculatedHashCodes = new List<int>();
            foreach (var md5 in inputHashList)
            {
                calculatedHashCodes.Add(MD5HashEqualityComparer.Instance.GetHashCode(md5));
            }

            var grouped = calculatedHashCodes.GroupBy(e => e).Where(e => e.Count() > 1).ToList();
            Console.WriteLine($"{Colors.Magenta(inputHashList.Count.ToString("N0"))} entries {Colors.Cyan(grouped.Count)} collisions");


            //// Warmup
            //timer = Stopwatch.StartNew();
            //foreach (var hash in hashList)
            //{
            //    int asd = equalityComparer.GetHashCode(hash);
            //}
            //GC.Collect();
            //GC.WaitForPendingFinalizers();
            //Console.WriteLine($"Warmup done in {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");

            //timer = Stopwatch.StartNew();
            //foreach (var hash in hashList)
            //{
            //    int asd = equalityComparer.GetHashCode(hash);
            //}

            //Console.WriteLine($"Done in {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");
        }

        //TODO measure this with benchmark.net
        private static void BenchmarkMD5Hash_ToString()
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
