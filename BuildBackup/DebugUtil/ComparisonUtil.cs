using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BuildBackup.DebugUtil.Models;
using Konsole;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
        
        public ComparisonResult CompareAgainstRealRequests(List<Request> generatedRequests, TactProduct product, bool writeOutputFiles)
        {
            Console.WriteLine("\nComparing requests against real request logs...");
            var timer = Stopwatch.StartNew();

            var fileSizeProvider = new FileSizeProvider(product, _blizzardCdnBaseUri);

            generatedRequests = RequestUtils.CoalesceRequests(generatedRequests, true);
            var requestsWithoutSize = generatedRequests.Count(e => e.DownloadWholeFile);
            GetRequestSizes(generatedRequests, fileSizeProvider);

            var realRequests = NginxLogParser.GetSavedRequestLogs(Config.LogFileBasePath, product).ToList();
            GetRequestSizes(realRequests, fileSizeProvider);

            if (writeOutputFiles)
            {
                var baseUri = @"C:\Users\Tim\Dropbox\Programming\dotnet-public";
                var jsonSettings = new JsonConverter[] { new StringEnumConverter() };
                File.WriteAllText($@"{baseUri}\generated.json", JsonConvert.SerializeObject(generatedRequests.OrderBy(e => e.RootFolder).ThenBy(e => e.CdnKey).ThenBy(e => e.LowerByteRange),
                    Formatting.Indented, jsonSettings));
                File.WriteAllText($@"{baseUri}\real.json", JsonConvert.SerializeObject(realRequests.OrderBy(e => e.RootFolder).ThenBy(e => e.CdnKey).ThenBy(e => e.LowerByteRange),
                    Formatting.Indented, new JsonConverter[] { new StringEnumConverter() }));
            }
            var comparisonResult = new ComparisonResult
            {
                GeneratedRequests = FastDeepCloner.DeepCloner.Clone(generatedRequests),
                RealRequests = FastDeepCloner.DeepCloner.Clone(realRequests),

                RequestsWithoutSize = requestsWithoutSize,
                RealRequestsWithoutSize = realRequests.Count(e => e.TotalBytes == 0)
            };

            CompareRequests(generatedRequests, realRequests);
            comparisonResult.Misses = realRequests;
            comparisonResult.UnnecessaryRequests = generatedRequests;

            comparisonResult.PrintOutput();
            if (writeOutputFiles)
            {
                comparisonResult.SaveToDisk(@"C:\Users\Tim\Dropbox\Programming\dotnet-public");
            }

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
            Parallel.ForEach(wholeFileRequests, new ParallelOptions { MaxDegreeOfParallelism = 25 }, request =>
            {
                fileSizeProvider.GetContentLength(request);
                progressBar.Refresh(count, $"Getting request sizes.  {Colors.Cyan(wholeFileRequests.Count - count)} remaining");
                count++;
            });
            fileSizeProvider.Save();
            Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}".PadLeft(Config.Padding));
        }

        private void GetRequestSizes(List<Request> requests, FileSizeProvider fileSizeProvider)
        {
            PreLoadHeaderSizes(requests, fileSizeProvider);

            foreach (var request in requests)
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
            CompareExactMatches(generatedRequests, originalRequests);
            CompareRangeMatches(generatedRequests, originalRequests);
            CompareRangeMatches(originalRequests, generatedRequests);
            //TODO not sure why this is required.  but it is
            CompareRangeMatches(generatedRequests, originalRequests);
            CompareRangeMatches(originalRequests, generatedRequests);

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

                var partialMatchesLower = generatedRequests.Where(e => e.CdnKey == current.CdnKey
                                                                       && e.RootFolder.Name == current.RootFolder.Name
                                                                       && current.LowerByteRange <= e.UpperByteRange
                                                                       && current.UpperByteRange >= e.UpperByteRange).ToList();
                if (partialMatchesLower.Any())
                {
                    // Case where the request we are testing against satisfies the whole match - lower end match
                    var generatedRequest = partialMatchesLower[0];

                    // Store the originals, since we need to swap them
                    var originalUpper = generatedRequest.UpperByteRange;
                    var originalLower = current.LowerByteRange;

                    // Now swap them
                    generatedRequest.UpperByteRange = originalLower - 1;
                    current.LowerByteRange = originalUpper + 1;

                    continue;
                }

                var partialMatchesUpper = generatedRequests.Where(e => e.CdnKey == current.CdnKey
                                                                       && e.RootFolder.Name == current.RootFolder.Name
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

        private void CompareExactMatches(List<Request> generatedRequests, List<Request> originalRequests)
        {
            // Copying the original requests to a temporary list, so that we can remove entries without modifying the enumeration
            var requestsToProcess = new List<Request>(originalRequests.Count);
            foreach (var request in originalRequests)
            {
                requestsToProcess.Add(request);
            }
            originalRequests.Clear();

            // Taking each "real" request, and "subtracting" it from the requests our app made.  Hoping to figure out what excess is being left behind.
            while (requestsToProcess.Any())
            {
                var current = requestsToProcess.First();

                // Special case for indexes
                if (current.IsIndex)
                {
                    //TODO doesn't look like RootFolder is being deserialized correctly.
                    var indexMatch = generatedRequests.FirstOrDefault(e => e.IsIndex && e.CdnKey == current.CdnKey
                                                                                     && e.RootFolder.Name == current.RootFolder.Name);
                    if (indexMatch != null)
                    {
                        requestsToProcess.RemoveAt(0);
                        generatedRequests.Remove(indexMatch);
                        continue;
                    }
                }

                // Exact match, remove from both lists
                var exactMatch = generatedRequests.FirstOrDefault(e => e.CdnKey == current.CdnKey
                                                                       && e.RootFolder.Name == current.RootFolder.Name
                                                                       && e.LowerByteRange == current.LowerByteRange
                                                                       && e.UpperByteRange == current.UpperByteRange);
                if (exactMatch != null)
                {
                    requestsToProcess.RemoveAt(0);
                    generatedRequests.Remove(exactMatch);
                    continue;
                }

                // No match found - Put it back into the original array, as a "miss"
                requestsToProcess.RemoveAt(0);
                originalRequests.Add(current);
            }
        }

        private void CompareRangeMatches(List<Request> generatedRequests, List<Request> originalRequests)
        {
            // Copying the original requests to a temporary list, so that we can remove entries without modifying the enumeration
            var requestsToProcess = new List<Request>(originalRequests.Count);
            foreach (var request in originalRequests)
            {
                requestsToProcess.Add(request);
            }
            originalRequests.Clear();

            // Taking each "real" request, and "subtracting" it from the requests our app made.  Hoping to figure out what excess is being left behind.
            while (requestsToProcess.Any())
            {
                var current = requestsToProcess.First();

                var rangeMatches = generatedRequests.Where(e => e.CdnKey == current.CdnKey
                                                                && e.RootFolder.Name == current.RootFolder.Name
                                                                && current.LowerByteRange >= e.LowerByteRange
                                                                && current.UpperByteRange <= e.UpperByteRange).ToList();
                if (rangeMatches.Any())
                {
                    if (rangeMatches.Count > 1)
                    {
                        //TODO how do I handle this scenario?
                    }
                    // Breaking up the remainder into new slices
                    var match = rangeMatches[0];
                    generatedRequests.AddRange(SplitRequests(match, current));
                    generatedRequests.Remove(match);

                    requestsToProcess.RemoveAt(0);
                    continue;
                }

                // No match found - Put it back into the original array, as a "miss"
                requestsToProcess.RemoveAt(0);
                originalRequests.Add(current);
            }
        }

        private static List<Request> SplitRequests(Request match, Request current)
        {
            var results = new List<Request>();
            if (match.LowerByteRange != current.LowerByteRange)
            {
                var lowerSlice = new Request
                {
                    //TODO should probably unit test the range calculations as well
                    LowerByteRange = match.LowerByteRange,
                    UpperByteRange = current.LowerByteRange - 1,

                    ProductRootUri = current.ProductRootUri,
                    RootFolder = current.RootFolder,
                    CdnKey = current.CdnKey,
                    IsIndex = current.IsIndex
                };
                results.Add(lowerSlice);
            }

            // Only add an upper slice, if there is any remaining bytes to do so.
            if (match.UpperByteRange != current.UpperByteRange)
            {
                var upperSlice = new Request
                {
                    //TODO should probably unit test the range calculations as well
                    LowerByteRange = current.UpperByteRange + 1,
                    UpperByteRange = match.UpperByteRange,

                    ProductRootUri = current.ProductRootUri,
                    RootFolder = current.RootFolder,
                    CdnKey = current.CdnKey,
                    IsIndex = current.IsIndex
                };
                results.Add(upperSlice);
            }

            return results;
        }
    }
}