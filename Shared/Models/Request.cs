using ByteSizeLib;

namespace Shared.Models
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

        public long TotalBytes => UpperByteRange - LowerByteRange;

        public override string ToString()
        {
            if (!DownloadWholeFile)
            {
                var size = ByteSize.FromBytes((double)TotalBytes);
                return $"{Uri} {LowerByteRange}-{UpperByteRange} {size.MegaBytes.ToString("##.##")}mb";
            }
            return $"{Uri} - -";
        }
    }
}
