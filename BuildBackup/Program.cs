using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BuildBackup.Structs;
using BuildBackup.Utils;
using Spectre.Console;
using Colors = Shared.Colors;

namespace BuildBackup
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
            //TactProducts.CodBlackOpsColdWar,
            //TactProducts.CodWarzone,
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

        public static bool UseCdnDebugMode = true;
        public static bool ShowDebugStats = true;

        public static bool WriteOutputFiles = false;

        public static void Main()
        {
            foreach (var product in ProductsToProcess)
            {
                AnsiConsoleSettings consoleSettings = new AnsiConsoleSettings();
                ProductHandler.ProcessProduct(product, AnsiConsole.Create(consoleSettings), UseCdnDebugMode, WriteOutputFiles, ShowDebugStats, SkipDiskCache);

                //BenchmarkUtil.Benchmark(product);
            }

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press any key to continue . . .");
                Console.ReadLine();
            }
            AnsiConsole.WriteLine("Done!");
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
                //inputHashList.Add();
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
    }
}
