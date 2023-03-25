namespace BattleNetPrefill.Extensions
{
    public static class MiscExtensions
    {
        public static MD5Hash ToMD5(this string str)
        {
            if (str.Length != 32)
            {
                throw new ArgumentException("input string length != 32", nameof(str));
            }
            var array = Convert.FromHexString(str);
            return Unsafe.As<byte, MD5Hash>(ref array[0]);
        }

        public static ByteSize SumTotalBytes(this List<Request> requests)
        {
            return ByteSize.FromBytes(requests.Sum(e => e.TotalBytes));
        }
    }
}