using System.Collections.Generic;
using ByteSizeLib;

namespace Shared.Models
{
    public class ComparisonResult
    {
        public List<ComparedRequest> Hits { get; set; }
        public List<ComparedRequest> Misses { get; set; }

        public ByteSize RequestTotalSize { get; set; }
        public ByteSize RealRequestsTotalSize { get; set; }

        public int HitCount => Hits.Count;
        public int MissCount => Misses.Count;
    }
}