using System;
using System.Diagnostics;
using BuildBackup.Utils;

namespace BuildBackup.Structs
{
    public readonly struct MD5Hash : IEquatable<MD5Hash>
    {
        public readonly ulong lowPart;
        public readonly ulong highPart;

        public MD5Hash(ulong lowPart, ulong highPart)
        {
            this.lowPart = lowPart;
            this.highPart = highPart;
        }

        public bool Equals(MD5Hash other)
        {
            return other.lowPart == lowPart && other.highPart == highPart;
        }

        public override string ToString()
        {
            return ToStringNew();
        }

        public string ToStringOld()
        {
            return (BitConverter.ToString(BitConverter.GetBytes(lowPart))
                    + BitConverter.ToString(BitConverter.GetBytes(highPart)))
                .Replace("-", "");
        }

        //TODO unit test this vs expected
        //var hash = new MD5Hash(6051113216891152126L, 49107880105117937L).ToString();
        public string ToStringNew()
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
                    dst[i] = HexConverter.ToCharUpper((uint)((lowPartTemp & highMask) >> 4));
                    dst[i + 1] = HexConverter.ToCharUpper((uint)(lowPartTemp & lowMask));
                    i += 2;
                    lowPartTemp >>= 8;
                }

                while (i != 32)
                {
                    dst[i] = HexConverter.ToCharUpper((uint)((highPartTemp & highMask) >> 4));
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
