using System.Collections.Generic;

namespace BattleNetPrefill.Integration.Test.Handlers
{
    [TestFixture]
    public class DownloadHandlerTests
    {
        [Test]
        public void TagsOfTheSameType_GetCombinedLogicalOR()
        {
            var tagsToUse = new List<DownloadTag>
            {
                // These two tags are the same type, so they should be combined
                new DownloadTag { Mask = new byte[] { 0b00001111 }, Name = "SinglePlayer", Type = 1 },
                new DownloadTag { Mask = new byte[] { 0b11110000 }, Name =  "MultiPlayer", Type = 1 }
            };

            var downloadHandler = new DownloadFileHandler(null);
            var result = downloadHandler.BuildDownloadMask(tagsToUse);

            // Because the two tags are part of the same group, we will be expecting the result to be a logical OR 
            Assert.AreEqual(0b11111111, result.Mask[0]);
        }

        [Test]
        public void TagsOfTheSameType_GetCombinedLogicalOR_LargerNumberOfTags()
        {
            var tagsToUse = new List<DownloadTag>
            {
                // These two tags are the same type, so they should be combined
                new DownloadTag { Mask = new byte[] { 0b00000011 }, Name = "SinglePlayer", Type = 1 },
                new DownloadTag { Mask = new byte[] { 0b00001100 }, Name =  "MultiPlayer", Type = 1 },
                new DownloadTag { Mask = new byte[] { 0b00110000 }, Name =      "Zombies", Type = 1 },
                new DownloadTag { Mask = new byte[] { 0b01000000 }, Name =       "Arcade", Type = 1 },
                new DownloadTag { Mask = new byte[] { 0b10000000 }, Name =    "HD Assets", Type = 1 }
            };

            var downloadHandler = new DownloadFileHandler(null);
            var result = downloadHandler.BuildDownloadMask(tagsToUse);

            // Because the two tags are part of the same group, we will be expecting the result to be a logical OR 
            Assert.AreEqual(0b11111111, result.Mask[0]);
        }

        [Test]
        public void TagsWithDifferentType_CombineOverlappingBits()
        {
            var tagsToUse = new List<DownloadTag>
            {
                // Because these tags are different types, they should only combine on the last two bits
                new DownloadTag { Mask = new byte[] { 0b00001111 }, Name = "Windows", Type = 1 },
                new DownloadTag { Mask = new byte[] { 0b00000011 }, Name =    "enUS", Type = 2 }
            };

            var downloadHandler = new DownloadFileHandler(null);
            var result = downloadHandler.BuildDownloadMask(tagsToUse);

            // These tags should only combine on the last two bits, as those are the only ones in common
            Assert.AreEqual(0b00000011, result.Mask[0]);
        }

        [Test]
        public void TagsWithDifferentType_WontCombineNonOverlappingBits()
        {
            var tagsToUse = new List<DownloadTag>
            {
                // Because these tags are different types, they will combine on none of these bits
                new DownloadTag { Mask = new byte[] { 0b00001111 }, Name = "Windows", Type = 1 },
                new DownloadTag { Mask = new byte[] { 0b11110000 }, Name =    "enUS", Type = 2 }
            };

            var downloadHandler = new DownloadFileHandler(null);
            var result = downloadHandler.BuildDownloadMask(tagsToUse);

            // These tags should combine on no bits, since they have no bits in common
            Assert.AreEqual(0b00000000, result.Mask[0]);
        }
    }
}
