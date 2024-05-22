namespace BattleNetPrefill.Test.DebugUtilTests
{
    [TestFixture]
    public class RequestTests
    {
        [Test]
        public void DuplicateRequests_AreOverlapping()
        {
            var leftHandRequest = new Request { LowerByteRange = 0, UpperByteRange = 100 };
            var rightHandRequest = new Request { LowerByteRange = 0, UpperByteRange = 100 };

            var areOverlapping = leftHandRequest.Overlaps(rightHandRequest, false);
            Assert.AreEqual(true, areOverlapping);
        }

        [Test]
        public void NonOverlapping()
        {
            var leftHandRequest = new Request { LowerByteRange = 0, UpperByteRange = 100 };
            var rightHandRequest = new Request { LowerByteRange = 555, UpperByteRange = 2000 };

            var areOverlapping = leftHandRequest.Overlaps(rightHandRequest, false);
            Assert.AreEqual(false, areOverlapping);
        }

        [Test]
        public void Overlapping_OnUpperByteRange()
        {
            var leftHandRequest = new Request { LowerByteRange = 0, UpperByteRange = 100 };
            var rightHandRequest = new Request { LowerByteRange = 0, UpperByteRange = 999999 };

            var areOverlapping = leftHandRequest.Overlaps(rightHandRequest, false);
            Assert.AreEqual(true, areOverlapping);
        }

        [Test]
        public void Overlapping_OnLowerByteRange()
        {
            var leftHandRequest = new Request { LowerByteRange = 0, UpperByteRange = 50 };
            var rightHandRequest = new Request { LowerByteRange = 25, UpperByteRange = 100 };

            var areOverlapping = leftHandRequest.Overlaps(rightHandRequest, false);
            Assert.AreEqual(true, areOverlapping);
        }

        [Test]
        public void Overlapping_OnLowerByteRangeReversed()
        {
            var leftHandRequest = new Request { LowerByteRange = 25, UpperByteRange = 50 };
            var rightHandRequest = new Request { LowerByteRange = 0, UpperByteRange = 100 };

            var areOverlapping = leftHandRequest.Overlaps(rightHandRequest, false);
            Assert.AreEqual(true, areOverlapping);
        }

        [Test]
        public void SequentialByteRanges_AreOverlapping()
        {
            var leftHandRequest = new Request { LowerByteRange = 0, UpperByteRange = 100 };
            var rightHandRequest = new Request { LowerByteRange = 101, UpperByteRange = 200 };

            var areOverlapping = leftHandRequest.Overlaps(rightHandRequest, false);
            Assert.AreEqual(true, areOverlapping);
        }

        [Test]
        [TestCase(100)]
        [TestCase(2500)]
        [TestCase(4096)]
        public void BattleNetRequests_AreOverlappingWhenSequentialWithin4Kilobytes(int byteDelta)
        {
            // Because these ranges are within 4kb of each other, then they will be considered as "sequential"
            var leftHandRequest = new Request { LowerByteRange = 0, UpperByteRange = 1000 };

            var lowerByteStart = leftHandRequest.UpperByteRange + byteDelta;
            var rightHandRequest = new Request { LowerByteRange = lowerByteStart, UpperByteRange = lowerByteStart + 1000 };

            var areOverlapping = leftHandRequest.Overlaps(rightHandRequest, isBattleNetClient: true);
            Assert.AreEqual(true, areOverlapping);
        }
    }
}
