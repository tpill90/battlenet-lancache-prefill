using System;
using BuildBackup.Utils;

namespace BuildBackup.Structs
{
    public readonly struct MD5Hash : IEquatable<MD5Hash>
    {
        /// <summary>
        /// This is actually the "higher" part of the hash.  Left most chunk of data.
        /// </summary>
        public readonly ulong lowPart;
        public readonly ulong highPart;

        public MD5Hash(ulong lowPart, ulong highPart)
        {
            this.lowPart = lowPart;
            this.highPart = highPart;
        }

        public override int GetHashCode()
        {
            return MD5HashEqualityComparer.Instance.GetHashCode(this);
        }

        public bool Equals(MD5Hash o)
        {
            MD5Hash other = (MD5Hash)o;
            return other.lowPart == lowPart && other.highPart == highPart;
        }

        public override bool Equals(object o)
        {
            MD5Hash other = (MD5Hash)o;
            return other.lowPart == lowPart && other.highPart == highPart;
        }

        public override string ToString()
        {
            return string.Create(32, (highPart, lowPart), static (dst, state) =>
            {
                ulong highPartTemp = state.highPart;
                ulong lowPartTemp = state.lowPart;

                ulong lowMask = (ulong)15;
                ulong highMask = 15 << 4;
                int i = 0;

                while (i != 16)
                {
                    dst[i] = HexConverter.ToCharUpper((uint)((lowPartTemp >> 4) & lowMask));
                    dst[i + 1] = HexConverter.ToCharUpper((uint)(lowPartTemp & lowMask));
                    i += 2;
                    lowPartTemp >>= 8;
                }

                while (i != 32)
                {
                    dst[i] = HexConverter.ToCharUpper((uint)((highPartTemp >> 4) & highMask));
                    dst[i + 1] = HexConverter.ToCharUpper((uint)(highPartTemp & lowMask));
                    i += 2;
                    highPartTemp >>= 8;
                }
            });
        }

        public static bool operator ==(MD5Hash obj1, MD5Hash obj2)
        {
            return obj1.Equals(obj2);
        }

        public static bool operator !=(MD5Hash obj1, MD5Hash obj2)
        {
            return !obj1.Equals(obj2);
        }
    }
}
