namespace BattleNetPrefill.Utils
{
    public class Md5HashEqualityComparer : IEqualityComparer<MD5Hash>
    {
        private static Md5HashEqualityComparer instance;

        private Md5HashEqualityComparer() { }

        public static Md5HashEqualityComparer Instance => instance ??= new Md5HashEqualityComparer();

        public bool Equals(MD5Hash x, MD5Hash y)
        {
            return x.lowPart == y.lowPart && x.highPart == y.highPart;
        }

        public int GetHashCode(MD5Hash obj)
        {
            return HashCode.Combine(obj.lowPart, obj.highPart);
        }
    }
}