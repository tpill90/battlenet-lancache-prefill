using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Testing;

namespace BattleNetPrefill.Utils.Debug
{
    public static class BenchmarkUtil
    {
        public static async Task BenchmarkAsync(TactProduct targetProduct, int warmupRuns = 4, int totalRuns = 10)
        {
            AnsiConsole.WriteLine(Colors.Yellow("Starting benchmark..."));

            await WarmupAsync(targetProduct, warmupRuns);

            List<Stopwatch> runResults = new List<Stopwatch>();
            for (int i = 0; i < totalRuns; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();

                AnsiConsole.WriteLine(Colors.Yellow($"Run {i+1}"));

                Stopwatch timer = Stopwatch.StartNew();
                await TactProductHandler.ProcessProductAsync(targetProduct, new TestConsole(), useDebugMode: true, writeOutputFiles: false, showDebugStats: false);
                timer.Stop();
                runResults.Add(timer);

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            PrintStatistics(runResults);
        }

        private static async Task WarmupAsync(TactProduct targetProduct, int runs)
        {
            AnsiConsole.WriteLine(Colors.Yellow("Starting warmup..."));
            for (int i = 0; i < runs; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();

                await TactProductHandler.ProcessProductAsync(targetProduct, new TestConsole(), useDebugMode: true, writeOutputFiles: false, showDebugStats: false);

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

            AnsiConsole.WriteLine();
        }
    }
}
