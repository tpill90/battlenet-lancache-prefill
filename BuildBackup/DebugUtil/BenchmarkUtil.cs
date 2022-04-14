using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Konsole;
using Spectre.Console;
using Colors = Shared.Colors;

namespace BuildBackup.DebugUtil
{
    public static class BenchmarkUtil
    {
        public static void Benchmark(TactProduct targetProduct, int warmupRuns = 4, int totalRuns = 10)
        {
            Console.WriteLine(Colors.Yellow("Starting benchmark..."));

            Warmup(targetProduct, warmupRuns);

            List<Stopwatch> runResults = new List<Stopwatch>();
            for (int i = 0; i < totalRuns; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();

                Console.WriteLine(Colors.Yellow($"Run {i+1}"));

                Stopwatch timer = Stopwatch.StartNew();
                ProductHandler.ProcessProduct(targetProduct, new Writer(), useDebugMode: true, writeOutputFiles: false, showDebugStats: false);
                timer.Stop();
                runResults.Add(timer);

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            PrintStatistics(runResults);
        }

        private static void Warmup(TactProduct targetProduct, int runs)
        {
            Console.WriteLine(Colors.Yellow("Starting warmup..."));
            for (int i = 0; i < runs; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();

                ProductHandler.ProcessProduct(targetProduct, new Writer(), useDebugMode: true, writeOutputFiles: false, showDebugStats: false);

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private static void PrintStatistics(List<Stopwatch> runResults)
        {
            // Formatting output to table
            var table = new Table();
            table.AddColumn(new TableColumn("Statistics").LeftAligned());
            table.AddColumn(new TableColumn("").Centered());

            var averageTicks = runResults.Average(e => e.Elapsed.Ticks);
            table.AddRow("Average", new TimeSpan(Convert.ToInt64(averageTicks)).ToString(@"mm\:ss\.FFFF"));
            table.AddRow("Minimum", runResults.Min(e => e.Elapsed).ToString(@"mm\:ss\.FFFF"));
            table.AddRow("Maximum", runResults.Max(e => e.Elapsed).ToString(@"mm\:ss\.FFFF"));
            AnsiConsole.Write(table);
            
            Console.WriteLine();
        }
    }
}
