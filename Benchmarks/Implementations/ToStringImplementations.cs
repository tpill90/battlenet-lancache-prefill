namespace Benchmarks.Implementations
{
    public static class ToStringImplementations
    {
        public static string HexmateConvertStructToByteArray(MD5 hash)
        {
            var bytes = new byte[16];
            BitConverter.TryWriteBytes(bytes.AsSpan(0, 8), hash.highPart);
            BitConverter.TryWriteBytes(bytes.AsSpan(8, 8), hash.lowPart);
            return HexMate.Convert.ToHexString(bytes);
        }

        /// <summary>
        /// I wrote this method as a replacement for BitConverter.ToString().  It was a dramatic speedup, and used significantly less memory.
        /// However, it is significantly more difficult to read and comprehend than other options.
        /// </summary>
        public static string StringCreate(ulong highPart, ulong lowPart)
        {
            return string.Create(32, (highPart, lowPart), static (dst, state) =>
            {
                ulong highPartTemp = state.highPart;
                ulong lowPartTemp = state.lowPart;

                ulong lowMask = 15;
                ulong highMask = 15 << 4;
                int i = 0;

                while (i != 16)
                {
                    dst[i] = ToCharUpper((uint)((lowPartTemp & highMask) >> 4));
                    dst[i + 1] = ToCharUpper((uint)(lowPartTemp & lowMask));
                    i += 2;
                    lowPartTemp >>= 8;
                }

                while (i != 32)
                {
                    dst[i] = ToCharUpper((uint)((highPartTemp & highMask) >> 4));
                    dst[i + 1] = ToCharUpper((uint)(highPartTemp & lowMask));
                    i += 2;
                    highPartTemp >>= 8;
                }
            });

            static char ToCharUpper(uint value)
            {
                value &= 0xF;
                value += '0';

                if (value > '9')
                {
                    value += ('A' - ('9' + 1));
                }

                return (char)value;
            }
        }

        /// <summary>
        /// This was the method originally used by BuildBackup.  I swapped it out because it was incredibly slow compared to other options.
        /// </summary>
        public static string BitConverterToString(byte[] bytes)
        {
            var hash = BitConverter.ToString(bytes)
                                   .Replace("-", string.Empty)
                                   .ToUpper();
            return hash;
        }
    }
}
