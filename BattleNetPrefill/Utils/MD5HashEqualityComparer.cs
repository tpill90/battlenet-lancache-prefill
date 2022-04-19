using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BattleNetPrefill.Structs;

namespace BattleNetPrefill.Utils
{
    public class MD5HashEqualityComparer : IEqualityComparer<MD5Hash>
    {
        const uint FnvPrime32 = 16777619;
        const uint FnvOffset32 = 2166136261;

        private static MD5HashEqualityComparer instance;

        private MD5HashEqualityComparer() { }

        public static MD5HashEqualityComparer Instance => instance ?? (instance = new MD5HashEqualityComparer());

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
}