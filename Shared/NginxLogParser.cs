using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MoreLinq;
using Newtonsoft.Json;
using Shared.Models;

namespace Shared
{
    public static class NginxLogParser
    {
        //TODO comment
        //TODO test
        public static string GetLatestLogVersionForProduct(string logBasePath, TactProduct product)
        {
            var logFolder = $@"{logBasePath}\{product.DisplayName}";

            var latestFile = new DirectoryInfo(logFolder)
                .GetFiles()
                .Where(e => !e.Name.Contains("coalesced"))
                .OrderByDescending(e => e.LastWriteTime)
                .FirstOrDefault();

            return latestFile.Name.Replace(".log", "");
        }

        /// <summary>
        /// Finds the most recent log file for the specified product
        /// </summary>
        /// <param name="logBasePath">Root folder where all log files are stored.</param>
        /// <param name="product">Target product to be parsed.  Used to determine subfolder to search for files</param>
        /// <returns></returns>
        public static List<Request> ParseRequestLogs(string logBasePath, TactProduct product)
        {
            var logFolder = $@"{logBasePath}\{product.DisplayName}";

            var latestFile = new DirectoryInfo(logFolder)
                                    .GetFiles()
                                    .OrderByDescending(e => e.LastWriteTime)
                                    .FirstOrDefault();

            if (latestFile.FullName.Contains("coalesced"))
            {
                return JsonConvert.DeserializeObject<List<Request>>(File.ReadAllText(latestFile.FullName));
            }
            else
            {
                var rawLogs = ParseRequestLogs(File.ReadAllLines(latestFile.FullName));
                List<Request> requestsToReplay = NginxLogParser.CoalesceRequests(rawLogs);

                var coalescedFileName = $"{logFolder}\\{latestFile.Name.Replace(".log", ".coalesced.log")}";
                File.WriteAllText(coalescedFileName, JsonConvert.SerializeObject(requestsToReplay));

                return requestsToReplay;
            }
        }

        //TODO comment
        public static List<Request> ParseRequestLogs(string[] rawRequests)
        {
            //Console.WriteLine($"Found {Colors.Cyan(rawRequests.Length)} total requests in file");

            var parsedRequests = new List<Request>();

            // Only interested in GET requests from Battle.Net.  Filtering out any other requests from other clients like Steam
            var filteredRequests = rawRequests.Where(e => e.Contains("GET") && e.Contains("[blizzard]")).ToList();
            foreach (var rawRequest in filteredRequests)
            {
                // Find all matches between double quotes.  This will be the only info that we care about in the request logs.
                var matches = Regex.Matches(rawRequest, "\"(.*?)\"");

                var httpRequest = matches[0].Value;
                // Request byte range will always be the last result
                string byteRange = matches[matches.Count - 1].Value
                    .Replace("bytes=", "")
                    .Replace("\"", "");


                var parsedRequest = new Request()
                {
                    //TODO replace this with a regex
                    // Uri will be the second item.  Example : "GET /tpr/sc1live/data/b5/20/b520b25e5d4b5627025aeba235d60708 HTTP/1.1". 
                    // Will also remove leading slash
                    Uri = httpRequest.Split(" ")[1].Remove(0, 1)
                };

                if (byteRange == "-")
                {
                    parsedRequest.DownloadWholeFile = true;
                }
                else
                {
                    parsedRequest.LowerByteRange = long.Parse(byteRange.Split("-")[0]);
                    parsedRequest.UpperByteRange = long.Parse(byteRange.Split("-")[1]);
                }

                parsedRequests.Add(parsedRequest);
            }

            //Console.WriteLine($"     {Colors.Cyan(parsedRequests.Count)} raw requests parsed");

            return parsedRequests;
        }

        //TODO comment + unit test
        internal static List<Request> CoalesceRequests(List<Request> initialRequests)
        {
            // De-duplicating requests
            var dedupedRequests = initialRequests.DistinctBy(e => new
                {
                    e.Uri, 
                    e.LowerByteRange, 
                    e.UpperByteRange, 
                    e.DownloadWholeFile
                })
                .OrderBy(e => e.Uri)
                .ThenBy(e => e.LowerByteRange)
                .ToList();

            //Coalescing any requests to the same URI that have sequential byte ranges.  
            var coalesced = new List<Request>();
            
            var requestsGroupedByUri = dedupedRequests.GroupBy(e => e.Uri).ToList();
            foreach (var grouping in requestsGroupedByUri)
            {
                var requestsToProcess = grouping.ToList();
                // Pulling out our first node
                var current = requestsToProcess[0];
                requestsToProcess.RemoveAt(0);

                // Iterate through the list until there is nothing left to combine
                while (requestsToProcess.Any())
                {
                    var matched = requestsToProcess.FirstOrDefault(e => e.Uri == current.Uri && e.LowerByteRange == (current.UpperByteRange + 1));
                    if (matched != null)
                    {
                        current.UpperByteRange = matched.UpperByteRange;
                        requestsToProcess.Remove(matched);
                    }
                    else
                    {
                        coalesced.Add(current);
                        current = requestsToProcess[0];
                        requestsToProcess.RemoveAt(0);
                    }
                }

                // Have to add our final loop iteration otherwise we'll skip it by accident
                coalesced.Add(current);
            }
            
            return coalesced;
        }
    }
}