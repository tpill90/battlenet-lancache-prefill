using System;
using System.Collections.Generic;
using System.Linq;
using ByteSizeLib;
using Spectre.Console;

namespace Shared.Models
{
    //TODO comment what these fields mean
    public class ComparisonResult
    {
        public int RequestMadeCount { get; set; }
        public int DuplicateRequests { get; set; }

        public int RealRequestCount { get; set; }

        public List<Request> Misses { get; set; }
        public List<Request> UnnecessaryRequests { get; set; }

        public ByteSize RequestTotalSize { get; set; }
        public ByteSize RealRequestsTotalSize { get; set; }

        public int RequestsWithoutSize { get; set; }
        public int RealRequestsWithoutSize { get; set; }

        public int MissCount => Misses.Count;

        public void PrintOutput()
        {
            // Formatting output to table
            var table = new Table();
            table.AddColumn(new TableColumn("").LeftAligned());
            table.AddColumn(new TableColumn(SpectreColors.Blue("Current")).Centered());
            table.AddColumn(new TableColumn(SpectreColors.Blue("Expected")).Centered());
            //TODO
            // table.AddColumn(new TableColumn(SpectreColors.Blue("Matches")).Centered()); , ((char)0x2713).ToString()

            table.AddRow("Requests made", RequestMadeCount.ToString(), RealRequestCount.ToString());
            table.AddRow("Bandwidth required", RequestTotalSize.ToString(), RealRequestsTotalSize.ToString());
            table.AddRow("Requests missing size", RequestsWithoutSize.ToString(), RealRequestsWithoutSize.ToString());
            AnsiConsole.Write(table);

            Console.WriteLine($"Total Misses : {Colors.Red(MissCount)}");
            Console.WriteLine($"Misses Bandwidth : {Colors.Yellow(ByteSize.FromBytes(Misses.Sum(e => e.TotalBytes)))}");
            Console.WriteLine($"Unnecessary Requests : {Colors.Yellow(UnnecessaryRequests.Count)}");
            Console.WriteLine($"Total Dupes : {Colors.Yellow(DuplicateRequests)}");
            //TODO log wasted bandwidth

            Console.WriteLine();

            var missedGroups = Misses.GroupBy(e => e.Uri).OrderByDescending(e => e.Count()).ToList();
            foreach (var group in missedGroups)
            {
                //Console.WriteLine($"Missed {Colors.Yellow(group.Count())} requests for uri {Colors.Magenta(group.Key)} Size : {ByteSize.FromBytes(group.Sum(e => e.TotalBytes))}");
            }
        }
    }
}