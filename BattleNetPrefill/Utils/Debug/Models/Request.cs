﻿using System;
using BattleNetPrefill.Structs;

namespace BattleNetPrefill.DebugUtil.Models
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
        private string _uri;
        public string Uri2
        {
            get
            {
                if (_uri == null)
                {
                    _uri = this.ToString();
                }
                return _uri;
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

        //TODO should this be nullable?
        public long LowerByteRange { get; set; }
        public long UpperByteRange { get; set; }

        public bool WriteToDevNull { get; set; }

        /* TODO some requests have a total bytes in the response that differs from the number of bytes requested.  Should this be handled?  Diff comparison would like to know this info
            Ex. "GET /tpr/sc1live/data/1f/79/1f797ab411c882e5f80a57e1a26d8e0d.index HTTP/1.1" 206 8268 "-" "-" "HIT" "level3.blizzard.com" "bytes=0-1048575"
            This requested a range of 0-1048575, but only got 8268 bytes back.
         */
        // Bytes are an inclusive range.  Ex bytes 0->9 == 10 bytes
        public long TotalBytes => (UpperByteRange - LowerByteRange) + 1;

        //TODO benchmark and see if this is slowing down anything
        public override string ToString()
        {
            //TODO remove this ToLower() call
            var hashId = CdnKey.ToString().ToLower();
            var uri = $"{ProductRootUri}/{RootFolder.Name}/{hashId.Substring(0, 2)}/{hashId.Substring(2, 2)}/{hashId}";
            if (IsIndex)
            {
                uri = $"{uri}.index";
            }
            return uri;
        }

        //TODO write some individual unit tests for this
        public bool Overlaps(Request request2, bool isBattleNetClient)
        {
            int overlap = 1;
            if (isBattleNetClient)
            {
                //TODO do some more testing on this?  The client considers anything within 4kb as being combined?
                //TODO comment why this is even necessary
                overlap = 4096;
            }

            if (LowerByteRange <= request2.LowerByteRange)
            {
                var overlaps = UpperByteRange >= request2.LowerByteRange;
                if (!overlaps)
                {
                    // Seeing if adjacent ranges can be combined
                    if (UpperByteRange + overlap >= request2.LowerByteRange)
                    {
                        return true;
                    }
                }
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
                ProductRootUri = ProductRootUri,
                RootFolder = RootFolder,
                CdnKey = CdnKey,
                IsIndex = IsIndex,

                LowerByteRange = Math.Min(LowerByteRange, request2.LowerByteRange),
                UpperByteRange = Math.Max(UpperByteRange, request2.UpperByteRange),

                WriteToDevNull = WriteToDevNull,
                //TODO this might not be right
                DownloadWholeFile = DownloadWholeFile
            };
        }
    }
}