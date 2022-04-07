using System;
using ByteSizeLib;

namespace BuildBackup.DebugUtil.Models
{
    /// <summary>
    /// Model that represents a request that could be made to a CDN.  
    /// </summary>
    public class Request
    {
        /// <summary>
        /// Request path, without a host name.  Agnostic towards host name, since we will combine with it later if needed to make a real request.
        /// Example :
        ///     tpr/sc1live/data/b5/20/b520b25e5d4b5627025aeba235d60708
        /// </summary>
        public string Uri { get; init; }

        //TODO should this be a computed property?
        public bool DownloadWholeFile { get; set; }

        //TODO should this be null?
        public long LowerByteRange { get; set; }
        public long UpperByteRange { get; set; }

        public bool WriteToDevNull { get; set; }

        /* TODO some requests have a total bytes in the response that differs from the number of bytes requested.  Should this be handled?  Diff comparison would like to know this info
            Ex. "GET /tpr/sc1live/data/1f/79/1f797ab411c882e5f80a57e1a26d8e0d.index HTTP/1.1" 206 8268 "-" "-" "HIT" "level3.blizzard.com" "bytes=0-1048575"
            This requested a range of 0-1048575, but only got 8268 bytes back.
         */
        // Bytes are an inclusive range.  Ex bytes 0->9 == 10 bytes
        public long TotalBytes => (UpperByteRange - LowerByteRange) + 1;

        public override string ToString()
        {
            if (DownloadWholeFile)
            {
                return $"{Uri} - -";
            }
            
            var size = ByteSize.FromBytes((double)TotalBytes);
            return $"{Uri} {LowerByteRange}-{UpperByteRange} {size}";
        }

        //TODO write some individual unit tests for this
        public bool Overlaps(Request request2)
        {
            if (LowerByteRange <= request2.LowerByteRange)
            {
                var overlaps = UpperByteRange >= request2.LowerByteRange;
                return overlaps;
            }
            else
            {
                return request2.UpperByteRange >= LowerByteRange;
            }
        }

        //TODO write some individual unit tests for this
        public Request MergeWith(Request request2)
        {
            return new Request
            {
                Uri = Uri,
                LowerByteRange = Math.Min(LowerByteRange, request2.LowerByteRange),
                UpperByteRange = Math.Max(UpperByteRange, request2.UpperByteRange),

                WriteToDevNull = WriteToDevNull,
                //TODO this might not be right
                DownloadWholeFile = DownloadWholeFile
            };
        }
    }
}
