using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ByteSizeLib;
using Konsole;
using Shared;
using Shared.Models;
using Colors = Shared.Colors;

namespace BuildBackup.DebugUtil
{
    public class ComparisonUtil
    {
        private readonly IConsole _console;

        //TODO extract url to settings
        string _blizzardCdnBaseUri = "http://level3.blizzard.com";
        public ComparisonUtil(IConsole console)
        {
            _console = console;
        }
        
        public ComparisonResult CompareAgainstRealRequests(List<Request> allRequestsMade, TactProduct product)
        {
            Console.WriteLine("\nComparing requests against real request logs...");
            var timer = Stopwatch.StartNew();

            //TODO sometimes seems to incorrectly combine requests.
            //allRequestsMade = NginxLogParser.CoalesceRequests(allRequestsMade);

            var fileSizeProvider = new FileSizeProvider(product, _blizzardCdnBaseUri);
            var realRequests = NginxLogParser.ParseRequestLogs(Config.LogFileBasePath, product).ToList();

            var comparisonResult = new ComparisonResult
            {
                RequestMadeCount = allRequestsMade.Count,
                RealRequestCount = realRequests.Count,

                RealRequestsTotalSize = ByteSize.FromBytes((double)realRequests.Sum(e => e.TotalBytes)),

                RequestsWithoutSize = allRequestsMade.Count(e => e.DownloadWholeFile),
                RealRequestsWithoutSize = realRequests.Count(e => e.TotalBytes == 0)
            };

            PreLoadHeaderSizes(allRequestsMade, fileSizeProvider);
            GetRequestSizes(allRequestsMade, fileSizeProvider);
            comparisonResult.RequestTotalSize = ByteSize.FromBytes((double) allRequestsMade.Sum(e => e.TotalBytes));
            
            CompareRequests(allRequestsMade, realRequests);
            comparisonResult.Misses = realRequests;
            comparisonResult.UnnecessaryRequests = allRequestsMade;

            comparisonResult.PrintOutput();

            Console.WriteLine($"Comparison complete! {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");
            return comparisonResult;
        }

        private void PreLoadHeaderSizes(List<Request> requests, FileSizeProvider fileSizeProvider)
        {
            var timer = Stopwatch.StartNew();
            
            var wholeFileRequests = requests.Where(e => e.DownloadWholeFile && !fileSizeProvider.HasBeenCached(e)).ToList();
            if (!wholeFileRequests.Any())
            {
                return;
            }

            var progressBar = new ProgressBar(_console, PbStyle.SingleLine, wholeFileRequests.Count, 50);
            int count = 0;
            // Speeding up by pre-caching the content-length headers in parallel.
            Parallel.ForEach(wholeFileRequests, new ParallelOptions { MaxDegreeOfParallelism = 30 }, request =>
            {
                fileSizeProvider.GetContentLength(request);
                progressBar.Refresh(count, $"Getting request sizes.  {Colors.Cyan(wholeFileRequests.Count - count)} remaining");
                count++;
            });
            fileSizeProvider.Save();
            Console.WriteLine($"    Complete! {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");
        }

        private void GetRequestSizes(List<Request> allRequestsMade, FileSizeProvider fileSizeProvider)
        {
            foreach (var request in allRequestsMade)
            {
                if (request.DownloadWholeFile)
                {
                    request.DownloadWholeFile = false;
                    request.LowerByteRange = 0;
                    // Subtracting 1, because it seems like the byte ranges are "inclusive".  Ex range 0-9 == 10 bytes length.
                    request.UpperByteRange = fileSizeProvider.GetContentLength(request) - 1;
                }
            }
            
        }
        
        public void CompareRequests(List<Request> generatedRequests, List<Request> originalRequests)
        {
            // Copying the original requests to a temporary list, so that we can remove entries without modifying the enumeration
            var requestsToProcess = new List<Request>(originalRequests.Count);
            foreach (var request in originalRequests)
            {
                requestsToProcess.Add(request);
            }
            originalRequests.Clear();

            // Taking each "real" request, and "subtracting" it from the requests our app made.  Hoping to figure out what excess is being left behind.
            while(requestsToProcess.Any())
            {
                var current = requestsToProcess.First();

                // Special case for indexes
                if (current.Uri.Contains(".index"))
                {
                    var indexMatches = generatedRequests.Where(e => e.Uri == current.Uri).ToList();
                    if (indexMatches.Any())
                    {
                        requestsToProcess.RemoveAt(0);
                        generatedRequests.Remove(indexMatches[0]);
                        continue;
                    }
                }

                // Exact match, remove from both lists
                var exactMatches = generatedRequests.Where(e => e.Uri == current.Uri
                                                              && e.LowerByteRange == current.LowerByteRange
                                                              && e.UpperByteRange == current.UpperByteRange).ToList();
                if (exactMatches.Any())
                {
                    if (exactMatches.Count > 1)
                    {
                        //TODO
                        //Debugger.Break();
                    }

                    requestsToProcess.RemoveAt(0);
                    generatedRequests.Remove(exactMatches[0]);
                    continue;
                }

                var rangeMatches = generatedRequests.Where(e => e.Uri == current.Uri
                                                              && current.LowerByteRange >= e.LowerByteRange
                                                              && current.UpperByteRange <= e.UpperByteRange).ToList();
                if (rangeMatches.Any())
                {
                    if (rangeMatches.Count > 1)
                    {
                        //TODO how do I handle this scenario?
                    }
                    // Breaking up the remainder into new slices

                    if (rangeMatches[0].LowerByteRange != current.LowerByteRange)
                    {
                        var lowerSlice = new Request
                        {
                            Uri = rangeMatches[0].Uri,
                            //TODO should probably unit test the range calculations as well
                            LowerByteRange = rangeMatches[0].LowerByteRange,
                            UpperByteRange = current.LowerByteRange - 1,
                            CallingMethod = rangeMatches[0].CallingMethod
                        };
                        generatedRequests.Add(lowerSlice);
                    }
                    
                    // Only add an upper slice, if there is any remaining bytes to do so.
                    if (rangeMatches[0].UpperByteRange != current.UpperByteRange)
                    {
                        var upperSlice = new Request
                        {
                            Uri = rangeMatches[0].Uri,
                            //TODO should probably unit test the range calculations as well
                            LowerByteRange = current.UpperByteRange + 1,
                            UpperByteRange = rangeMatches[0].UpperByteRange,
                            CallingMethod = rangeMatches[0].CallingMethod
                        };
                        generatedRequests.Add(upperSlice);
                    }
                    generatedRequests.Remove(rangeMatches[0]);

                    requestsToProcess.RemoveAt(0);
                    continue;
                }

                var partialMatchesLower = generatedRequests.Where(e => e.Uri == current.Uri 
                                                                  && current.LowerByteRange <= e.UpperByteRange
                                                                  && current.UpperByteRange >= e.UpperByteRange).ToList();
                if (partialMatchesLower.Any())
                {
                    // Store the originals, since we need to swap them
                    var originalUpper = partialMatchesLower[0].UpperByteRange;
                    var originalLower = current.LowerByteRange;

                    // Now swap them
                    partialMatchesLower[0].UpperByteRange = originalLower - 1;
                    current.LowerByteRange = originalUpper + 1;
                    continue;
                }

                var partialMatchesUpper = generatedRequests.Where(e => e.Uri == current.Uri
                                                                       && current.UpperByteRange >= e.LowerByteRange
                                                                       && current.LowerByteRange <= e.LowerByteRange).ToList();
                if (partialMatchesUpper.Any())
                {
                    // Store the originals, since we need to swap them
                    var originalUpper = current.UpperByteRange;
                    var originalLower = partialMatchesUpper[0].LowerByteRange; 

                    // Now swap them
                    partialMatchesUpper[0].LowerByteRange = originalUpper + 1;
                    current.UpperByteRange = originalLower - 1;
                    continue;
                }

                //TODO figure out why this is happening
                if (current.TotalBytes == 0)
                {
                    requestsToProcess.RemoveAt(0);
                    continue;
                }

                // No match found - Put it back into the original array, as a "miss"
                requestsToProcess.RemoveAt(0);
                originalRequests.Add(current);
            }

            
            
        }
    }
}