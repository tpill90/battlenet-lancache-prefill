using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shared.Models;

namespace Shared.Test
{
    [TestFixture]
    public class NginxLogParserTests
    {
        [Test]
        public void DuplicateRequests_GetCombined()
        {
            var requests = new List<Request>
            {
                // Creating two requests that are exact duplicates
                new Request() { Uri = "SampleUri", LowerByteRange = 0, UpperByteRange = 100, DownloadWholeFile = true },
                new Request() { Uri = "SampleUri", LowerByteRange = 0, UpperByteRange = 100, DownloadWholeFile = true }
            };

            var result = NginxLogParser.CoalesceRequests(requests);

            // Expect there to be 1 result left, since we deduped the final request.
            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void DuplicatesCreatedFromCombinedRequests_WillGetRemoved()
        {
            var requests = new List<Request>
            {
                new Request { Uri = "SampleUri", LowerByteRange = 0, UpperByteRange = 100, DownloadWholeFile = true },
                // Creating two requests that will be combined into a single request, that is a duplicate of the above
                new Request { Uri = "SampleUri", LowerByteRange = 0, UpperByteRange = 49, DownloadWholeFile = true },
                new Request { Uri = "SampleUri", LowerByteRange = 50, UpperByteRange = 100, DownloadWholeFile = true }
            };

            var result = NginxLogParser.CoalesceRequests(requests);

            // Expect there to be 1 result left, since we deduped the final request.
            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void RequestsThatDontMatch_OnUri_WontGetCombined()
        {
            var requests = new List<Request>
            {
                // Requests differ by URI, won't be combined
                new Request() { Uri = "SampleUri", LowerByteRange = 0, UpperByteRange = 100, DownloadWholeFile = true },
                new Request() { Uri = "DifferentURI", LowerByteRange = 0, UpperByteRange = 100, DownloadWholeFile = true }
            };

            var result = NginxLogParser.CoalesceRequests(requests);

            // Expect 2 results, since they wont get combined
            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void RequestsThatDontMatch_OnLowerByteRange_WontGetCombined()
        {
            var requests = new List<Request>
            {
                // Requests differ by LowerByteRange, won't be combined
                new Request() { Uri = "SampleUri", LowerByteRange = 0, UpperByteRange = 100, DownloadWholeFile = true },
                new Request() { Uri = "SampleUri", LowerByteRange = 9999, UpperByteRange = 100, DownloadWholeFile = true }
            };

            var result = NginxLogParser.CoalesceRequests(requests);

            // Expect 2 results, since they wont get combined
            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void RequestsThatDontMatch_OnUpperByteRange_WontGetCombined()
        {
            var requests = new List<Request>
            {
                // Requests differ by UpperByteRange, won't be combined
                new Request() { Uri = "SampleUri", LowerByteRange = 0, UpperByteRange = 100, DownloadWholeFile = true },
                new Request() { Uri = "SampleUri", LowerByteRange = 0, UpperByteRange = 999999, DownloadWholeFile = true }
            };

            var result = NginxLogParser.CoalesceRequests(requests);

            // Expect 2 results, since they wont get combined
            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void RequestsThatDontMatch_OnDownloadWholeFile_WontGetCombined()
        {
            var requests = new List<Request>
            {
                // Requests differ by DownloadWholeFile, won't be combined
                new Request() { Uri = "SampleUri", LowerByteRange = 0, UpperByteRange = 100, DownloadWholeFile = true },
                new Request() { Uri = "SampleUri", LowerByteRange = 0, UpperByteRange = 100, DownloadWholeFile = false }
            };

            var result = NginxLogParser.CoalesceRequests(requests);

            // Expect 2 results, since they wont get combined
            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void SequentialByteRanges_WillBeCombined()
        {
            var requests = new List<Request>
            {
                // These two requests have sequential byte ranges (0-100 -> 101-200), so they should be combined
                new Request() { Uri = "SampleUri", LowerByteRange = 0, UpperByteRange = 100 },
                new Request() { Uri = "SampleUri", LowerByteRange = 101, UpperByteRange = 200 }
            };

            var results = NginxLogParser.CoalesceRequests(requests);

            // Expect 1 result, with the byte range being the combination of the two
            Assert.AreEqual(1, results.Count);

            var combinedResult = results.FirstOrDefault();
            Assert.AreEqual(0, combinedResult.LowerByteRange);
            Assert.AreEqual(200, combinedResult.UpperByteRange);
        }

        [Test]
        public void SequentialByteRanges_DifferentUri_WontBeCombined()
        {
            var requests = new List<Request>
            {
                // These two requests have sequential byte ranges, however they are not to the same request so they will not be combined
                new Request { Uri = "SampleUri", LowerByteRange = 0, UpperByteRange = 100 },
                new Request { Uri = "DifferentUri", LowerByteRange = 101, UpperByteRange = 200 }
            };

            var results = NginxLogParser.CoalesceRequests(requests);

            // Expect 2 results, since they wont get combined
            Assert.AreEqual(2, results.Count);

            // Validating that the requests are untouched
            var firstRequest = results.Single(e => e.Uri == "SampleUri");
            Assert.AreEqual(0, firstRequest.LowerByteRange);
            Assert.AreEqual(100, firstRequest.UpperByteRange);

            var secondRequest = results.Single(e => e.Uri == "DifferentUri");
            Assert.AreEqual(101, secondRequest.LowerByteRange);
            Assert.AreEqual(200, secondRequest.UpperByteRange);
        }
    }
}
