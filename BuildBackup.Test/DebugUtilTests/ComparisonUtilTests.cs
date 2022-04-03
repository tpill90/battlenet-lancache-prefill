using System.Collections.Generic;
using BuildBackup.DebugUtil;
using NUnit.Framework;
using Shared.Models;

namespace BuildBackup.Test.DebugUtilTests
{
    [TestFixture]
    public class ComparisonUtilTests
    {
        //TODO comment
        [Test]
        public void NoMatchesFound()
        {
            var generatedRequests = new List<Request>
            {
                new Request {Uri = "/miss", LowerByteRange = 0, UpperByteRange = 10}
            };
            var expectedRequests = new List<Request>
            {
                new Request {Uri = "/example", LowerByteRange = 0, UpperByteRange = 10},
                new Request {Uri = "/example2", LowerByteRange = 0, UpperByteRange = 10}
            };

            var comparisonUtil = new ComparisonUtil(null);
            comparisonUtil.CompareRequests(generatedRequests, expectedRequests);

            // Since there were no matches found, we should expect the two lists to have their original contents
            Assert.AreEqual(1, generatedRequests.Count);
            Assert.AreEqual(2, expectedRequests.Count);
        }

        //TODO comment
        [Test]
        public void ExactMatches_AreRemoved()
        {
            var generatedRequests = new List<Request>
            {
                // This will be exactly the same as the expected requests
                new Request {Uri = "/example", LowerByteRange = 0, UpperByteRange = 10}
            };
            var expectedRequests = new List<Request>
            {
                new Request {Uri = "/example", LowerByteRange = 0, UpperByteRange = 10}
            };

            var comparisonUtil = new ComparisonUtil(null);
            comparisonUtil.CompareRequests(generatedRequests, expectedRequests);

            // Because there is an exact match between the requests, the requests should be removed from each array.
            Assert.IsEmpty(generatedRequests);
            Assert.IsEmpty(expectedRequests);
        }

        //TODO comment
        [Test]
        public void RangeMatches_FullSubset_FoundInCenterOfRange()
        {
            var generatedRequests = new List<Request>
            {
                // This request will be larger than what is expected, but should still be considered a match
                new Request {Uri = "/example", LowerByteRange = 0, UpperByteRange = 100}
            };
            var expectedRequests = new List<Request>
            {
                new Request {Uri = "/example", LowerByteRange = 25, UpperByteRange = 75}
            };

            var comparisonUtil = new ComparisonUtil(null);
            comparisonUtil.CompareRequests(generatedRequests, expectedRequests);

            // There is a partial match, so the remaining request byte ranges should be split into two.
            Assert.AreEqual(2, generatedRequests.Count);
            // Since this matched fully, should be completely removed
            Assert.IsEmpty(expectedRequests);
        }

        //TODO comment
        [Test]
        public void RangeMatches_FullSubset_FoundAtEndOfRange()
        {
            var generatedRequests = new List<Request>
            {
                // This request will be larger than what is expected, but should still be considered a match
                new Request {Uri = "/example", LowerByteRange = 0, UpperByteRange = 100}
            };
            var expectedRequests = new List<Request>
            {
                new Request {Uri = "/example", LowerByteRange = 50, UpperByteRange = 100}
            };

            var comparisonUtil = new ComparisonUtil(null);
            comparisonUtil.CompareRequests(generatedRequests, expectedRequests);

            // There is a partial match, so the remaining request byte ranges should be split into two.
            Assert.AreEqual(1, generatedRequests.Count);
            // Since this matched fully, should be completely removed
            Assert.IsEmpty(expectedRequests);
        }

        //TODO comment
        [Test]
        public void RangeMatches_FullSubset_FoundAtBeginningOfRange()
        {
            var generatedRequests = new List<Request>
            {
                // This request will be larger than what is expected, but should still be considered a match
                new Request {Uri = "/example", LowerByteRange = 0, UpperByteRange = 100}
            };
            var expectedRequests = new List<Request>
            {
                new Request {Uri = "/example", LowerByteRange = 0, UpperByteRange = 50}
            };

            var comparisonUtil = new ComparisonUtil(null);
            comparisonUtil.CompareRequests(generatedRequests, expectedRequests);

            // There is a partial match, so the remaining request byte ranges should be split into two.
            Assert.AreEqual(1, generatedRequests.Count);
            // Since this matched fully, should be completely removed
            Assert.IsEmpty(expectedRequests);
        }

        //TODO comment
        [Test]
        public void PartialMatch_FoundAtBeginningOfRange()
        {
            var generatedRequests = new List<Request>
            {
                new Request {Uri = "/example", LowerByteRange = 0, UpperByteRange = 50 }
            };
            var expectedRequests = new List<Request>
            {
                new Request {Uri = "/example", LowerByteRange = 25, UpperByteRange = 75 }
            };

            var comparisonUtil = new ComparisonUtil(null);
            comparisonUtil.CompareRequests(generatedRequests, expectedRequests);

            Assert.AreEqual(1, generatedRequests.Count);
            // Should only have the remainder left
            Assert.AreEqual(0, generatedRequests[0].LowerByteRange);
            Assert.AreEqual(24, generatedRequests[0].UpperByteRange);

            Assert.AreEqual(1, expectedRequests.Count);
            Assert.AreEqual(51, expectedRequests[0].LowerByteRange);
            Assert.AreEqual(75, expectedRequests[0].UpperByteRange);
        }

        //TODO comment
        [Test]
        public void PartialMatch_FoundAtEndOfRange()
        {
            var generatedRequests = new List<Request>
            {
                new Request {Uri = "/example", LowerByteRange = 50, UpperByteRange = 100 }
            };
            var expectedRequests = new List<Request>
            {
                new Request {Uri = "/example", LowerByteRange = 25, UpperByteRange = 75 }
            };

            var comparisonUtil = new ComparisonUtil(null);
            comparisonUtil.CompareRequests(generatedRequests, expectedRequests);

            Assert.AreEqual(1, generatedRequests.Count);
            // Should only have the remainder left
            Assert.AreEqual(76, generatedRequests[0].LowerByteRange);
            Assert.AreEqual(100, generatedRequests[0].UpperByteRange);

            Assert.AreEqual(1, expectedRequests.Count);
            Assert.AreEqual(25, expectedRequests[0].LowerByteRange);
            Assert.AreEqual(49, expectedRequests[0].UpperByteRange);
        }

        //TODO comment
        [Test]
        public void Indexes_IgnoreByteRanges()
        {
            var generatedRequests = new List<Request>
            {
                new Request {Uri = "/sample.index", LowerByteRange = 0, UpperByteRange = 1122 }
            };
            var expectedRequests = new List<Request>
            {
                new Request {Uri = "/sample.index", LowerByteRange = 0, UpperByteRange = 102400000 }
            };

            var comparisonUtil = new ComparisonUtil(null);
            comparisonUtil.CompareRequests(generatedRequests, expectedRequests);

            // When an index is matched, it should only consider the Uri.  Battle.net for whatever reason chooses to request the wrong byte range
            Assert.AreEqual(0, generatedRequests.Count);
            Assert.AreEqual(0, expectedRequests.Count);
        }
    }
}
