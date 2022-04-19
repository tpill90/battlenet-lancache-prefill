using System.Collections.Generic;
using BattleNetPrefill.DebugUtil;
using BattleNetPrefill.DebugUtil.Models;
using BattleNetPrefill.Structs;
using NUnit.Framework;

namespace BattleNetPrefill.Test.DebugUtilTests
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
                new Request { RootFolder = RootFolder.data, CdnKey = new MD5Hash(100, 0), LowerByteRange = 0, UpperByteRange = 10}
            };
            var expectedRequests = new List<Request>
            {
                new Request { RootFolder = RootFolder.data, CdnKey = new MD5Hash(200, 0), LowerByteRange = 0, UpperByteRange = 10},
                new Request { RootFolder = RootFolder.data, CdnKey = new MD5Hash(0, 555), LowerByteRange = 0, UpperByteRange = 10}
            };

            var comparisonUtil = new ComparisonUtil();
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
                new Request { RootFolder = RootFolder.data, CdnKey = new MD5Hash(100, 0), LowerByteRange = 0, UpperByteRange = 10}
            };
            var expectedRequests = new List<Request>
            {
                new Request { RootFolder = RootFolder.data, CdnKey = new MD5Hash(100, 0), LowerByteRange = 0, UpperByteRange = 10}
            };

            var comparisonUtil = new ComparisonUtil();
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
                new Request { RootFolder = RootFolder.data, CdnKey = new MD5Hash(100, 0), ProductRootUri = "/example", LowerByteRange = 0, UpperByteRange = 100}
            };
            var expectedRequests = new List<Request>
            {
                new Request { RootFolder = RootFolder.data, CdnKey = new MD5Hash(100, 0), ProductRootUri = "/example", LowerByteRange = 25, UpperByteRange = 75}
            };

            var comparisonUtil = new ComparisonUtil();
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
                new Request { RootFolder = RootFolder.data, CdnKey = new MD5Hash(100, 0), ProductRootUri = "/example", LowerByteRange = 0, UpperByteRange = 100}
            };
            var expectedRequests = new List<Request>
            {
                new Request { RootFolder = RootFolder.data, CdnKey = new MD5Hash(100, 0), ProductRootUri = "/example", LowerByteRange = 50, UpperByteRange = 100}
            };

            var comparisonUtil = new ComparisonUtil();
            comparisonUtil.CompareRequests(generatedRequests, expectedRequests);

            // There is a partial match, so the remaining request byte ranges should be split into two.
            Assert.AreEqual(1, generatedRequests.Count);
            // Since this matched fully, should be completely removed
            Assert.IsEmpty(expectedRequests);
        }

        //TODO comment
        [Test]
        public void RangeMatches_FullSubset_FoundAtEndOfRange_Reversed()
        {
            var generatedRequests = new List<Request>
            {
                // This request will be larger than what is expected, but should still be considered a match
                new Request { RootFolder = RootFolder.data, CdnKey = new MD5Hash(100, 0), ProductRootUri = "/example", LowerByteRange = 50, UpperByteRange = 100 }
            };
            var expectedRequests = new List<Request>
            {
                new Request { RootFolder = RootFolder.data, CdnKey = new MD5Hash(100, 0), ProductRootUri = "/example", LowerByteRange = 0, UpperByteRange = 100}
            };

            var comparisonUtil = new ComparisonUtil();
            comparisonUtil.CompareRequests(generatedRequests, expectedRequests);
            
            // Since this matched fully, should be completely removed
            Assert.IsEmpty(generatedRequests);

            // There is a partial match, so the remaining request byte ranges should be split into two.
            Assert.AreEqual(1, expectedRequests.Count);
            Assert.AreEqual(0, expectedRequests[0].LowerByteRange);
            Assert.AreEqual(49, expectedRequests[0].UpperByteRange);
        }

        //TODO comment
        [Test]
        public void RangeMatches_FullSubset_FoundAtBeginningOfRange()
        {
            var generatedRequests = new List<Request>
            {
                // This request will be larger than what is expected, but should still be considered a match
                new Request { RootFolder = RootFolder.data, CdnKey = new MD5Hash(100, 0), ProductRootUri = "/example", LowerByteRange = 0, UpperByteRange = 100}
            };
            var expectedRequests = new List<Request>
            {
                new Request { RootFolder = RootFolder.data, CdnKey = new MD5Hash(100, 0), ProductRootUri = "/example", LowerByteRange = 0, UpperByteRange = 50}
            };

            var comparisonUtil = new ComparisonUtil();
            comparisonUtil.CompareRequests(generatedRequests, expectedRequests);

            // There is a partial match, so the remaining request byte ranges should be split into two.
            Assert.AreEqual(1, generatedRequests.Count);
            // Since this matched fully, should be completely removed
            Assert.IsEmpty(expectedRequests);
        }

        //TODO comment
        [Test]
        public void RangeMatches_FullSubset_FoundAtBeginningOfRange_Reversed()
        {
            var generatedRequests = new List<Request>
            {
                // This request will be larger than what is expected, but should still be considered a match
                new Request { RootFolder = RootFolder.data, CdnKey = new MD5Hash(100, 0), ProductRootUri = "/example", LowerByteRange = 0, UpperByteRange = 50}
            };
            var expectedRequests = new List<Request>
            {
                new Request { RootFolder = RootFolder.data, CdnKey = new MD5Hash(100, 0), ProductRootUri = "/example", LowerByteRange = 0, UpperByteRange = 100}
            };

            var comparisonUtil = new ComparisonUtil();
            comparisonUtil.CompareRequests(generatedRequests, expectedRequests);

            
            // Since this matched fully, should be completely removed
            Assert.IsEmpty(generatedRequests);

            // There is a partial match, so the remaining request byte ranges should be split into two.
            Assert.AreEqual(51, expectedRequests[0].LowerByteRange);
            Assert.AreEqual(100, expectedRequests[0].UpperByteRange);
            Assert.AreEqual(1, expectedRequests.Count);
        }

        //TODO comment
        [Test]
        public void PartialMatch_FoundAtBeginningOfRange()
        {
            var generatedRequests = new List<Request>
            {
                new Request { RootFolder = RootFolder.data, CdnKey = new MD5Hash(100, 0), ProductRootUri = "/example", LowerByteRange = 0, UpperByteRange = 50 }
            };
            var expectedRequests = new List<Request>
            {
                new Request { RootFolder = RootFolder.data, CdnKey = new MD5Hash(100, 0), ProductRootUri = "/example", LowerByteRange = 25, UpperByteRange = 75 }
            };

            var comparisonUtil = new ComparisonUtil();
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
                new Request { RootFolder = RootFolder.data, CdnKey = new MD5Hash(100, 0), ProductRootUri = "/example", LowerByteRange = 50, UpperByteRange = 100 }
            };
            var expectedRequests = new List<Request>
            {
                new Request { RootFolder = RootFolder.data, CdnKey = new MD5Hash(100, 0), ProductRootUri = "/example", LowerByteRange = 25, UpperByteRange = 75 }
            };

            var comparisonUtil = new ComparisonUtil();
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
        //TODO rename
        [Test]
        public void PartialMatchLower_WholeGeneratedRequestMatches_EndOfRange()
        {
            var generatedRequests = new List<Request>
            {
                new Request { RootFolder = RootFolder.data, CdnKey = new MD5Hash(100, 0), ProductRootUri = "/example", LowerByteRange = 44527, UpperByteRange = 64327 },
                new Request { RootFolder = RootFolder.data, CdnKey = new MD5Hash(100, 0), ProductRootUri = "/example", LowerByteRange = 16997, UpperByteRange = 40744 }
            };
            var expectedRequests = new List<Request>
            {
                new Request { RootFolder = RootFolder.data, CdnKey = new MD5Hash(100, 0), ProductRootUri = "/example", LowerByteRange = 16997, UpperByteRange = 64327 }
            };

            var comparisonUtil = new ComparisonUtil();
            comparisonUtil.CompareRequests(generatedRequests, expectedRequests);

            Assert.AreEqual(0, generatedRequests.Count);

            Assert.AreEqual(1, expectedRequests.Count);
            Assert.AreEqual(40745, expectedRequests[0].LowerByteRange);
            Assert.AreEqual(44526, expectedRequests[0].UpperByteRange);
        }

        //TODO comment
        [Test]
        public void Indexes_IgnoreByteRanges()
        {
            var generatedRequests = new List<Request>
            {
                new Request { ProductRootUri = "/sample.index", IsIndex = true, LowerByteRange = 0, UpperByteRange = 1122, RootFolder = RootFolder.data, CdnKey = new MD5Hash(0,0)}
            };
            var expectedRequests = new List<Request>
            {
                new Request { ProductRootUri = "/sample.index", IsIndex = true, LowerByteRange = 0, UpperByteRange = 102400000 , RootFolder = RootFolder.data, CdnKey = new MD5Hash(0,0)}
            };

            var comparisonUtil = new ComparisonUtil();
            comparisonUtil.CompareRequests(generatedRequests, expectedRequests);

            // When an index is matched, it should only consider the Uri.  Battle.net for whatever reason chooses to request the wrong byte range
            Assert.AreEqual(0, generatedRequests.Count);
            Assert.AreEqual(0, expectedRequests.Count);
        }
    }
}
