namespace BattleNetPrefill.Utils.Debug.Models
{
    //TODO probably shouldn't be in debug namespace
    /// <summary>
    /// Model that represents a request that could be made to a CDN.  
    /// </summary>
    public sealed class Request
    {
        public Request()
        {

        }

        public Request(string productRootUri, RootFolder rootFolder, MD5Hash cdnKey, long? startBytes = null, long? endBytes = null,
                        bool writeToDevNull = false, bool isIndex = false)
        {
            ProductRootUri = productRootUri;
            RootFolder = rootFolder;
            CdnKey = cdnKey;
            IsIndex = isIndex;
            WriteToDevNull = writeToDevNull;

            if (startBytes != null && endBytes != null)
            {
                LowerByteRange = startBytes.Value;
                UpperByteRange = endBytes.Value;
            }
            else
            {
                DownloadWholeFile = true;
            }
        }

        /// <summary>
        /// Root uri of the target product.  Ex. tpr/sc1live
        /// </summary>
        public string ProductRootUri { get; set; }

        public RootFolder RootFolder { get; set; }

        public MD5Hash CdnKey { get; set; }

        public bool IsIndex { get; set; }

        public bool DownloadWholeFile { get; set; }

        public long LowerByteRange { get; set; }
        public long UpperByteRange { get; set; }

        //TODO this name kind of sucks
        public bool WriteToDevNull { get; set; }

        // Bytes are an inclusive range.  Ex bytes 0->9 == 10 bytes
        public long TotalBytes => (UpperByteRange - LowerByteRange) + 1;

        /// <summary>
        /// Request path, without a host name.  Agnostic towards host name, since we will combine with it later if needed to make a real request.
        /// Example :
        ///     tpr/sc1live/data/b5/20/b520b25e5d4b5627025aeba235d60708
        /// </summary>
        private string _uri;
        public string Uri
        {
            get
            {
                if (_uri == null)
                {
                    var hashId = CdnKey.ToStringLower();
                    _uri = $"{ProductRootUri}/{RootFolder.Name}/{hashId.Substring(0, 2)}/{hashId.Substring(2, 2)}/{hashId}";
                    if (IsIndex)
                    {
                        _uri = $"{_uri}.index";
                    }
                }
                return _uri;
            }
        }

        public override string ToString()
        {
            if (DownloadWholeFile)
            {
                return $"{Uri} - -";
            }

            var size = ByteSize.FromBytes((double)TotalBytes);
            return $"{Uri} {LowerByteRange}-{UpperByteRange} {size}";
        }

        public bool Overlaps(Request request2, bool isBattleNetClient)
        {
            int overlap = 1;
            if (isBattleNetClient)
            {
                // For some reason, the real Battle.Net client seems to combine requests if their ranges are within 4kb of each other.  
                // This does not make intuitive sense, as looking at the entries in the Archive Index shows that the range should not be requested,
                // ex. only bytes 0-340 and bytes 1400-2650 should be individually requested.  However these two individual requests get combined into 0-2650
                overlap = 4096;
            }

            if (LowerByteRange <= request2.LowerByteRange)
            {
                // Checks to see if ranges are overlapping ex 0-100 and 50-200
                var areOverlapping = UpperByteRange >= request2.LowerByteRange;
                if (!areOverlapping)
                {
                    // Seeing if adjacent ranges can be combined, ex 0-100 and 101-200
                    if (UpperByteRange + overlap >= request2.LowerByteRange)
                    {
                        return true;
                    }
                }
                return areOverlapping;
            }
            return request2.UpperByteRange >= LowerByteRange;
        }

        public void MergeWith(Request request2)
        {
            LowerByteRange = Math.Min(LowerByteRange, request2.LowerByteRange);
            UpperByteRange = Math.Max(UpperByteRange, request2.UpperByteRange);
        }
    }
}
