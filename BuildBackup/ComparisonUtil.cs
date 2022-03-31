using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AutoMapper;
using ByteSizeLib;
using Shared;
using Shared.Models;
using Spectre.Console;

namespace BuildBackup
{
    public static class ComparisonUtil
    {
        private static Mapper mapper;

        static ComparisonUtil()
        {
            var mapperConfig = new MapperConfiguration(cfg => cfg.CreateMap<Request, ComparedRequest>());
            mapper = new Mapper(mapperConfig);
        }

        //TODO need to calculate total bandwidth waste as well.
        public static ComparisonResult CompareToRealRequests(List<Request> allRequestsMade, TactProduct product)
        {
            //TODO re-implement coalescing + dedupe.  However this messes with the FullDownloadProperty
            //allRequestsMade = NginxLogParser.CoalesceRequests(allRequestsMade);
            
            var realRequests = NginxLogParser.CoalesceRequests(NginxLogParser.ParseRequestLogs(Config.LogFileBasePath, product));

            List<ComparedRequest> realRequestMatches = realRequests.Select(e => mapper.Map<ComparedRequest>(e)).ToList();

            allRequestsMade = allRequestsMade.Where(e => e != null).ToList();
            if (allRequestsMade.Any(e => e == null))
            {
                //TODO debug this, probably a threading issue.
                Debugger.Break();
            }

            foreach (var realRequest in realRequestMatches)
            {
                // Finding any requests that match on URI
                var uriMatches = allRequestsMade.Where(e => e.Uri == realRequest.Uri).ToList();

                // Handle each one of the matches
                foreach (var match in uriMatches)
                {
                    if (match.DownloadWholeFile)
                    {
                        realRequest.Matched = true;
                        realRequest.MatchedRequest = match;
                    }
                }
            }

            var comparisonResult = new ComparisonResult
            {
                Hits = realRequestMatches.Where(e => e.Matched == true).ToList(),
                Misses = realRequestMatches.Where(e => e.Matched == false).ToList(),

                RequestTotalSize = ByteSize.FromBytes((double)allRequestsMade.Sum(e => e.TotalBytes)),
                RealRequestsTotalSize = ByteSize.FromBytes((double)realRequests.Sum(e => e.TotalBytes))
            };

            PrintOutput(allRequestsMade, realRequests, comparisonResult);

            return comparisonResult;
        }

        private static void PrintOutput(List<Request> allRequestsMade, List<Request> realRequests, ComparisonResult comparisonResult)
        {
            // Formatting output to table
            var table = new Table();
            table.AddColumn(new TableColumn("").LeftAligned());
            table.AddColumn(new TableColumn(SpectreColors.Blue("Current")).Centered());
            table.AddColumn(new TableColumn(SpectreColors.Blue("Expected")).Centered());
            //TODO
            // table.AddColumn(new TableColumn(SpectreColors.Blue("Matches")).Centered()); , ((char)0x2713).ToString()

            table.AddRow("Requests made", allRequestsMade.Count.ToString(), realRequests.Count.ToString());

            table.AddRow("Bandwidth required", comparisonResult.RequestTotalSize.ToString(), comparisonResult.RealRequestsTotalSize.ToString());

            table.AddRow("Requests missing size", allRequestsMade.Count(e => e.TotalBytes == 0).ToString(), realRequests.Count(e => e.TotalBytes == 0).ToString());
            AnsiConsole.Write(table);

            Console.WriteLine($"Total Hits : {Colors.Green(comparisonResult.HitCount)}");
            Console.WriteLine($"Total Misses : {Colors.Red(comparisonResult.MissCount)}");
            Console.WriteLine();
        }
    }
}