using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BuildBackup.Structs;

namespace BuildBackup.Utils
{
    public class MD5HashComparer : IEqualityComparer<MD5Hash>
    {
        const uint FnvPrime32 = 16777619;
        const uint FnvOffset32 = 2166136261;

        private static MD5HashComparer instance;

        private MD5HashComparer() { }

        public static MD5HashComparer Instance => instance ?? (instance = new MD5HashComparer());

        public bool Equals(MD5Hash x, MD5Hash y)
        {
            return x.lowPart == y.lowPart && x.highPart == y.highPart;
        }

        public int GetHashCode(MD5Hash obj)
        {
            uint hash = FnvOffset32;

            ref uint ptr = ref Unsafe.As<MD5Hash, uint>(ref obj);

            for (int i = 0; i < 4; i++)
            {
                hash ^= Unsafe.Add(ref ptr, i);
                hash *= FnvPrime32;
            }

            return unchecked((int)hash);
        }
    }

    public class MD5HashComparer9 : IEqualityComparer<MD5Hash>
    {
        const uint FnvPrime32 = 16777619;
        const uint FnvOffset32 = 2166136261;

        private static MD5HashComparer9 instance;

        private MD5HashComparer9() { }

        public static MD5HashComparer9 Instance => instance ?? (instance = new MD5HashComparer9());

        public bool Equals(MD5Hash x, MD5Hash y)
        {
            return x.lowPart == y.lowPart && (x.highPart & 0xFF) == (y.highPart & 0xFF);
        }

        public int GetHashCode(MD5Hash obj)
        {
            uint hash = FnvOffset32;

            ref uint ptr = ref Unsafe.As<MD5Hash, uint>(ref obj);

            for (int i = 0; i < 2; i++)
            {
                hash ^= Unsafe.Add(ref ptr, i);
                hash *= FnvPrime32;
            }
            hash ^= Unsafe.Add(ref ptr, 2) & 0xFF;
            hash *= FnvPrime32;

            return unchecked((int)hash);
        }
    }
}