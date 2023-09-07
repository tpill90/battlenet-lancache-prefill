namespace BattleNetPrefill.Utils.Debug
{
    //TODO should this be in debug?
    public static class RequestUtils
    {
        /// <summary>
        /// Combines overlapping, duplicate, and sequential requests.  This should ideally help with further operations on the list of requests,
        /// as there will be less entries to process.
        /// </summary>
        /// <param name="initialRequests">Requests that should be combined</param>
        /// <param name="isBattleNetClient">Should only be set to true if simulating the real Battle.Net client.  Combines requests in 4kb chunks.</param>
        /// <returns></returns>
        public static List<Request> CoalesceRequests(List<Request> initialRequests, bool isBattleNetClient = false)
        {
            var coalesced = new List<Request>();

            // Coalescing any requests to the same URI that have sequential/overlapping byte ranges.  
            var requestsGroupedByUri = initialRequests.GroupBy(e => new { e.RootFolder, e.CdnKey, e.IsIndex }).ToList();
            foreach (var grouping in requestsGroupedByUri)
            {
                var merged = grouping.OrderBy(e => e.LowerByteRange)
                                     .MergeOverlapping(isBattleNetClient)
                                     .ToList();

                coalesced.AddRange(merged);
            }

            return coalesced;
        }

        public static List<Request> CoalesceRequests(Dictionary<MD5Hash, List<Request>> initialRequests, bool isBattleNetClient = false)
        {
            var coalesced = new List<Request>();

            // Coalescing any requests to the same URI that have sequential/overlapping byte ranges.  
            foreach (var grouping in initialRequests.Values)
            {
                grouping.Sort((x, y) => x.LowerByteRange.CompareTo(y.LowerByteRange));
                coalesced.AddRange(grouping.MergeOverlapping(isBattleNetClient));
            }

            return coalesced;
        }

        private static IEnumerable<Request> MergeOverlapping(this IEnumerable<Request> source, bool isBattleNetClient)
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
                        previousInterval.MergeWith(nextInterval);
                    }
                }
                yield return previousInterval;
            }
        }
    }
}
