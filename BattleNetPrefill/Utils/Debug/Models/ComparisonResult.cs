using System;
using System.Collections.Generic;
using System.Linq;
using ByteSizeLib;
using Spectre.Console;
using static BattleNetPrefill.Utils.SpectreColors;

namespace BattleNetPrefill.Utils.Debug.Models
{
    //TODO comment what these fields mean
    public class ComparisonResult
    {
        public TimeSpan ElapsedTime { get; set; }

        public int RequestMadeCount { get; init; }
        public int RealRequestCount { get; init; }

        //TODO make private
        public List<Request> Misses { get; set; }
        public ByteSize MissedBandwidth => ByteSize.FromBytes(Misses.Sum(e => e.TotalBytes));
        public int MissCount => Misses.Count;

        //TODO make private
        public List<Request> UnnecessaryRequests { get; set; }
        public ByteSize WastedBandwidth => ByteSize.FromBytes(UnnecessaryRequests.Sum(e => e.TotalBytes));
        public int UnnecessaryRequestCount => UnnecessaryRequests.Count;

        public ByteSize GeneratedRequestTotalSize { get; set; }
        public ByteSize RealRequestsTotalSize { get; set; }

        public int RequestsWithoutSize { get; set; }
        public int RealRequestsWithoutSize { get; set; }

        public void PrintOutput()
        {
            // Formatting output to table
            var table = new Table();
            table.AddColumn(new TableColumn("").LeftAligned());
            table.AddColumn(new TableColumn(Blue("Current")));
            table.AddColumn(new TableColumn(Blue("Expected")).Centered());

            table.AddRow("Requests made", RequestMadeCount.ToString(), RealRequestCount.ToString());
            table.AddRow("Bandwidth required", GeneratedRequestTotalSize.ToString(), RealRequestsTotalSize.ToString());
            table.AddRow("Requests missing size", Yellow(RequestsWithoutSize.ToString()), RealRequestsWithoutSize.ToString());

            table.AddRow("Misses", Red(MissCount), "");
            table.AddRow("Misses Bandwidth", Red(ByteSize.FromBytes(Misses.Sum(e => e.TotalBytes))), "");

            table.AddRow("Unnecessary Requests", Yellow(UnnecessaryRequestCount), "");
            table.AddRow("Wasted Bandwidth", Yellow(WastedBandwidth), "");
            AnsiConsole.Write(table);

            if (MissCount > 0)
            {
                AnsiConsole.MarkupLine(Red("Missed Requests :"));
                foreach (var miss in Misses.Take(10))
                {
                    AnsiConsole.WriteLine($"{miss} {miss.LowerByteRange}-{miss.UpperByteRange}");
                }
            }

            if (UnnecessaryRequestCount > 0)
            {
                AnsiConsole.MarkupLine(Yellow("Unnecessary Requests :"));
                foreach (var req in UnnecessaryRequests.Take(10))
                {
                    AnsiConsole.WriteLine($"{req} {req.LowerByteRange}-{req.UpperByteRange}");
                }
            }

            AnsiConsole.WriteLine();
        }
    }
}