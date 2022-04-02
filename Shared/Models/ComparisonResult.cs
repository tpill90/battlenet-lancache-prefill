using System.Collections.Generic;
using ByteSizeLib;

namespace Shared.Models
{
    //TODO comment what these fields mean
    public class ComparisonResult
    {
        public int RequestMadeCount { get; set; }
        public int DuplicateRequests { get; set; }

        public int RealRequestCount { get; set; }

        public List<ComparedRequest> Hits { get; set; }
        public List<ComparedRequest> Misses { get; set; }

        public ByteSize RequestTotalSize { get; set; }
        public ByteSize RealRequestsTotalSize { get; set; }

        public int RequestsWithoutSize { get; set; }
        public int RealRequestsWithoutSize { get; set; }


        public int HitCount => Hits.Count;
        public int MissCount => Misses.Count;
    }
}