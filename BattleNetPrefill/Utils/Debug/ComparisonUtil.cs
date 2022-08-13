using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Utils.Debug.Models;
using Spectre.Console;

namespace BattleNetPrefill.Utils.Debug
{
    public class ComparisonUtil
    {
        string _blizzardCdnBaseUri = "http://level3.blizzard.com";
      
        public async Task<ComparisonResult> CompareAgainstRealRequestsAsync(List<Request> generatedRequests, TactProduct product)
        {
            AnsiConsole.WriteLine("Comparing requests against real request logs...");
            var timer = Stopwatch.StartNew();

            // Need to re-coalesce, in the case that we made duplicate requests.  Doesn't really matter, since the lancache can serve them again so quickly
            generatedRequests = RequestUtils.CoalesceRequests(generatedRequests, true);
            var realRequests = NginxLogParser.GetSavedRequestLogs(AppConfig.LogFileBasePath, product);
            
            var comparisonResult = new ComparisonResult
            {
                RequestMadeCount = generatedRequests.Count,
                RealRequestCount = realRequests.Count,

                RequestsWithoutSize = generatedRequests.Count(e => e.DownloadWholeFile),
                RealRequestsWithoutSize = realRequests.Count(e => e.TotalBytes == 0 || e.DownloadWholeFile)
            };

            // Populating the response size for any "whole file" requests
            using var fileSizeProvider = new FileSizeProvider(product, _blizzardCdnBaseUri);
            await fileSizeProvider.PopulateRequestSizesAsync(generatedRequests);
            await fileSizeProvider.PopulateRequestSizesAsync(realRequests);
            fileSizeProvider.Save();

            comparisonResult.GeneratedRequestTotalSize = generatedRequests.SumTotalBytes();
            comparisonResult.RealRequestsTotalSize = realRequests.SumTotalBytes();

            CompareRequests(generatedRequests, realRequests);
            comparisonResult.Misses.AddRange(realRequests);
            comparisonResult.UnnecessaryRequests.AddRange(generatedRequests);

            comparisonResult.PrintOutput();

            AnsiConsole.Console.MarkupLineTimer("Comparison complete!", timer);
            return comparisonResult;
        }
        
        public void CompareRequests(List<Request> generatedRequests, List<Request> originalRequests)
        {
            CompareExactMatches(generatedRequests, originalRequests);
            CompareRangeMatches(generatedRequests, originalRequests);
            CompareRangeMatches(originalRequests, generatedRequests);
            ComparePartialMatches(generatedRequests, originalRequests);
        }

        private static void ComparePartialMatches(List<Request> generatedRequests, List<Request> originalRequests)
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

                var partialMatchesLower = generatedRequests.Where(e => e.CdnKey == current.CdnKey
                                                                       && e.RootFolder == current.RootFolder
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
                                                                       && e.RootFolder == current.RootFolder
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

            // Bucketing requests by MD5 to speed up comparisons
            Dictionary<MD5Hash, List<Request>> generatedGrouped = generatedRequests.GroupBy(e => e.CdnKey).ToDictionary(e => e.Key, e => e.ToList());
            
            // Taking each "real" request, and "subtracting" it from the requests our app made.  Hoping to figure out what excess is being left behind.
            while (requestsToProcess.Any())
            {
                var current = requestsToProcess.First();

                // Checking to see if we have any requests that match on MD5
                List<Request> group;
                if (!generatedGrouped.TryGetValue(current.CdnKey, out group))
                {
                    // No match found, continuing
                    requestsToProcess.RemoveAt(0);
                    originalRequests.Add(current);
                    continue;
                }

                // Special case for indexes
                if (current.IsIndex)
                {
                    var indexMatch = group.FirstOrDefault(e => e.IsIndex && e.RootFolder == current.RootFolder);
                    if (indexMatch != null)
                    {
                        requestsToProcess.RemoveAt(0);
                        group.Remove(indexMatch);
                        continue;
                    }
                }

                // Exact match, remove from both lists
                var exactMatch = group.FirstOrDefault(e => e.LowerByteRange == current.LowerByteRange
                                                           && e.UpperByteRange == current.UpperByteRange
                                                           && e.RootFolder == current.RootFolder);
                if (exactMatch != null)
                {
                    requestsToProcess.RemoveAt(0);
                    group.Remove(exactMatch);
                    continue;
                }

                // No match found - Put it back into the original array, as a "miss"
                requestsToProcess.RemoveAt(0);
                originalRequests.Add(current);
            }

            // Finally, take the leftover generated requests, and aggregate them back into their original list
            generatedRequests.Clear(); 
            generatedRequests.AddRange(generatedGrouped.SelectMany(e => e.Value).ToList());
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

            // Bucketing requests by MD5 to speed up comparisons
            Dictionary<MD5Hash, List<Request>> generatedGrouped = generatedRequests.GroupBy(e => e.CdnKey).ToDictionary(e => e.Key, e => e.ToList());

            // Taking each "real" request, and "subtracting" it from the requests our app made.  Hoping to figure out what excess is being left behind.
            while (requestsToProcess.Any())
            {
                var current = requestsToProcess.First();

                // Checking to see if we have any requests that match on MD5
                List<Request> group;
                if (!generatedGrouped.TryGetValue(current.CdnKey, out group))
                {
                    // No match found, continuing
                    requestsToProcess.RemoveAt(0);
                    originalRequests.Add(current);
                    continue;
                }

                var rangeMatch = group.SingleOrDefault(e => e.RootFolder == current.RootFolder
                                                            && current.LowerByteRange >= e.LowerByteRange
                                                            && current.UpperByteRange <= e.UpperByteRange);
                if (rangeMatch != null)
                {
                    // Breaking up the remainder into new slices
                    group.AddRange(SplitRequests(rangeMatch, current));
                    group.Remove(rangeMatch);

                    requestsToProcess.RemoveAt(0);
                    continue;
                }
               
                // No match found - Put it back into the original array, as a "miss"
                requestsToProcess.RemoveAt(0);
                originalRequests.Add(current);
            }
            // Finally, take the leftover generated requests, and aggregate them back into their original list
            generatedRequests.Clear();
            generatedRequests.AddRange(generatedGrouped.SelectMany(e => e.Value).ToList());
        }

        private List<Request> SplitRequests(Request match, Request current)
        {
            var results = new List<Request>();
            if (match.LowerByteRange != current.LowerByteRange)
            {
                var lowerSlice = new Request
                {
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