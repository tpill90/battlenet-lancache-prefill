using System.Collections.Generic;
using System.Linq;
using BuildBackup.DebugUtil.Models;

namespace BuildBackup.DebugUtil
{
    public static class RequestUtils
    {
        //TODO comment + unit test
        public static List<Request> CoalesceRequests(List<Request> initialRequests, bool isBattleNetClient = false)
        {
            //TODO handle the case where there are "whole file downloads".  If there is a whole file download, then any other requests should just be removed at this step
            var coalesced = new List<Request>();

            //Coalescing any requests to the same URI that have sequential/overlapping byte ranges.  
            var requestsGroupedByUri = initialRequests.GroupBy(e => new {e.RootFolder, e.CdnKey, e.IsIndex }).ToList();
            foreach (var grouping in requestsGroupedByUri)
            {
                var merged = grouping.OrderBy(e => e.LowerByteRange).MergeOverlapping(isBattleNetClient).ToList();

                coalesced.AddRange(merged);
            }

            return coalesced;
        }

        public static IEnumerable<Request> MergeOverlapping(this IEnumerable<Request> source, bool isBattleNetClient)
        {
            using (var enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                {
                    yield break;
                }

                var previousInterval = enumerator.Current;
                while (enumerator.MoveNext())
                {
                    var nextInterval = enumerator.Current;
                    if (!previousInterval.Overlaps(nextInterval, isBattleNetClient))
                    {
                        yield return previousInterval;
                        previousInterval = nextInterval;
                    }
                    else
                    {
                        previousInterval = previousInterval.MergeWith(nextInterval);
                    }
                }
                yield return previousInterval;
            }
        }
    }
}
