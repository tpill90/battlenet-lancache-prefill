namespace BattleNetPrefill.Test
{
    [TestFixture]
    public class MD5HashTests
    {
        [Test]
        [TestCase(6051113216891152126UL, 49107880105117937UL, "FEE6771C69DFF953F18856555B77AE00")]
        [TestCase(8298574650443404551UL, 5955211075102420685UL, "0795BD45AD752A73CDAEF3B5EB28A552")]
        [TestCase(7043859728839941717UL, 17617057390324886482UL, "551E2F791AD1C061D22BFC795B5C7CF4")]
        [TestCase(8794752618514365567UL, 2740871278436135544UL, "7F3CFCF1E83C0D7A78B241EEE7870926")]
        public void ToString_ProducesCorrectResults(ulong lowPart, ulong highPart, string expected)
        {
            var hash = new MD5Hash(lowPart, highPart).ToString();
            Assert.AreEqual(expected, hash);
        }

        [Test]
        [TestCase(6051113216891152126UL, 49107880105117937UL, "FEE6771C69DFF953F18856555B77AE00")]
        [TestCase(8298574650443404551UL, 5955211075102420685UL, "0795BD45AD752A73CDAEF3B5EB28A552")]
        [TestCase(7043859728839941717UL, 17617057390324886482UL, "551E2F791AD1C061D22BFC795B5C7CF4")]
        [TestCase(8794752618514365567UL, 2740871278436135544UL, "7F3CFCF1E83C0D7A78B241EEE7870926")]
        public void BitConverterAsArray_IsCorrect(ulong lowPart, ulong highPart, string expected)
        {
            // Converting back to arrays for the sake of having test cases be easily readible
            var bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(lowPart));
            bytes.AddRange(BitConverter.GetBytes(highPart));

            var hash = ToStringImplementations.BitConverterToString(bytes.ToArray());
            Assert.AreEqual(expected, hash);
        }

        [Test]
        [TestCase(6051113216891152126UL, 49107880105117937UL, "FEE6771C69DFF953F18856555B77AE00")]
        [TestCase(8298574650443404551UL, 5955211075102420685UL, "0795BD45AD752A73CDAEF3B5EB28A552")]
        [TestCase(7043859728839941717UL, 17617057390324886482UL, "551E2F791AD1C061D22BFC795B5C7CF4")]
        [TestCase(8794752618514365567UL, 2740871278436135544UL, "7F3CFCF1E83C0D7A78B241EEE7870926")]
        public void HexmateConverted_IsCorrect(ulong lowPart, ulong highPart, string expected)
        {
            var hash = ToStringImplementations.HexmateConverted(lowPart,highPart);
            Assert.AreEqual(expected, hash);
        }

        [Test]
        [TestCase(6051113216891152126UL, 49107880105117937UL, "FEE6771C69DFF953F18856555B77AE00")]
        [TestCase(8298574650443404551UL, 5955211075102420685UL, "0795BD45AD752A73CDAEF3B5EB28A552")]
        [TestCase(7043859728839941717UL, 17617057390324886482UL, "551E2F791AD1C061D22BFC795B5C7CF4")]
        [TestCase(8794752618514365567UL, 2740871278436135544UL, "7F3CFCF1E83C0D7A78B241EEE7870926")]
        public void HexMate_IsCorrect(ulong lowPart, ulong highPart, string expected)
        {
            // Converting back to arrays for the sake of having test cases be easily readible
            var bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(lowPart));
            bytes.AddRange(BitConverter.GetBytes(highPart));

            var hash = ToStringImplementations.Hexmate(bytes.ToArray());
            Assert.AreEqual(expected, hash);
        }

        //TODO
        //[Test]
        //[TestCase(6051113216891152126UL, 49107880105117937UL, "FEE6771C69DFF953F18856555B77AE00")]
        //[TestCase(8298574650443404551UL, 5955211075102420685UL, "0795BD45AD752A73CDAEF3B5EB28A552")]
        //[TestCase(7043859728839941717UL, 17617057390324886482UL, "551E2F791AD1C061D22BFC795B5C7CF4")]
        //[TestCase(8794752618514365567UL, 2740871278436135544UL, "7F3CFCF1E83C0D7A78B241EEE7870926")]
        //public void StringFormat_IsCorrect(ulong lowPart, ulong highPart, string expected)
        //{
        //    var hash = ToStringImplementations.StringFormat(lowPart, highPart);
        //    Assert.AreEqual(expected, hash);
        //}
    }
}
