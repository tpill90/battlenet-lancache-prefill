using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using BuildBackup.Structs;

namespace System.IO
{
    public static class BinaryReaderExtensions
    {
        public static double ReadDouble(this BinaryReader reader, bool invertEndian = false)
        {
            if (invertEndian)
            {
                return BitConverter.ToDouble(reader.ReadInvertedBytes(8), 0);
            }

            return reader.ReadDouble();
        }

        public static Int16 ReadInt16(this BinaryReader reader, bool invertEndian = false)
        {
            if (invertEndian)
            {
                return BitConverter.ToInt16(reader.ReadInvertedBytes(2), 0);
            }

            return reader.ReadInt16();
        }

        public static short ReadInt16BE(this BinaryReader reader)
        {
            byte[] val = reader.ReadBytes(2);
            return (short)(val[1] | val[0] << 8);
        }

        public static Int32 ReadInt32(this BinaryReader reader, bool invertEndian = false)
        {
            if (invertEndian)
            {
                return BitConverter.ToInt32(reader.ReadInvertedBytes(4), 0);
            }

            return reader.ReadInt32();
        }

        public static Int64 ReadInt64(this BinaryReader reader, bool invertEndian = false)
        {
            if (invertEndian)
            {
                return BitConverter.ToInt64(reader.ReadInvertedBytes(8), 0);
            }

            return reader.ReadInt64();
        }

        public static Single ReadSingle(this BinaryReader reader, bool invertEndian = false)
        {
            if (invertEndian)
            {
                return BitConverter.ToSingle(reader.ReadInvertedBytes(4), 0);
            }

            return reader.ReadSingle();
        }

        public static UInt16 ReadUInt16(this BinaryReader reader, bool invertEndian = false)
        {
            if (invertEndian)
            {
                return BitConverter.ToUInt16(reader.ReadInvertedBytes(2), 0);
            }

            return reader.ReadUInt16();
        }

        public static UInt32 ReadUInt32(this BinaryReader reader, bool invertEndian = false)
        {
            if (invertEndian)
            {
                return BitConverter.ToUInt32(reader.ReadInvertedBytes(4), 0);
            }

            return reader.ReadUInt32();
        }

        public static UInt64 ReadUInt64(this BinaryReader reader, bool invertEndian = false)
        {
            if (invertEndian)
            {
                return BitConverter.ToUInt64(reader.ReadInvertedBytes(8), 0);
            }

            return reader.ReadUInt64();
        }

        public static UInt64 ReadUInt40(this BinaryReader reader, bool invertEndian = false)
        {
            ulong b1 = reader.ReadByte();
            ulong b2 = reader.ReadByte();
            ulong b3 = reader.ReadByte();
            ulong b4 = reader.ReadByte();
            ulong b5 = reader.ReadByte();

            if (invertEndian)
            {
                return (ulong)(b1 << 32 | b2 << 24 | b3 << 16 | b4 << 8 | b5);
            }
            else
            {
                return (ulong)(b5 << 32 | b4 << 24 | b3 << 16 | b2 << 8 | b1);
            }
        }

        private static byte[] ReadInvertedBytes(this BinaryReader reader, int byteCount)
        {
            byte[] byteArray = reader.ReadBytes(byteCount);
            Array.Reverse(byteArray);

            return byteArray;
        }

        public static T Read<T>(this BinaryReader reader) where T : unmanaged
        {
            byte[] result = reader.ReadBytes(Unsafe.SizeOf<T>());

            return Unsafe.ReadUnaligned<T>(ref result[0]);
        }

        public static string ToHexString(this byte[] data)
        {
#if NET5_0_OR_GREATER
            return Convert.ToHexString(data);
#else
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length == 0)
                return string.Empty;
            if (data.Length > int.MaxValue / 2)
                throw new ArgumentOutOfRangeException(nameof(data), "SR.ArgumentOutOfRange_InputTooLarge");
            return HexConverter.ToString(data, HexConverter.Casing.Upper);
#endif
        }

        public static bool EqualsTo(this in MD5Hash key, byte[] array)
        {
            if (array.Length != 16)
                return false;

            ref MD5Hash other = ref Unsafe.As<byte, MD5Hash>(ref array[0]);

            if (key.lowPart != other.lowPart || key.highPart != other.highPart)
                return false;

            return true;
        }

        public static bool EqualsTo9(this in MD5Hash key, byte[] array)
        {
            if (array.Length != 16)
                return false;

            ref MD5Hash other = ref Unsafe.As<byte, MD5Hash>(ref array[0]);

            return EqualsTo9(key, other);
        }

        public static bool EqualsTo9(this in MD5Hash key, in MD5Hash other)
        {
            if (key.lowPart != other.lowPart)
                return false;

            if ((key.highPart & 0xFF) != (other.highPart & 0xFF))
                return false;

            return true;
        }

        public static bool EqualsTo(this in MD5Hash key, in MD5Hash other)
        {
            return key.lowPart == other.lowPart && key.highPart == other.highPart;
        }

        public static unsafe string ToHexString(this in MD5Hash key)
        {
#if NET5_0_OR_GREATER
            ref MD5Hash md5ref = ref Unsafe.AsRef(in key);
            var md5Span = MemoryMarshal.CreateReadOnlySpan(ref md5ref, 1);
            var span = MemoryMarshal.AsBytes(md5Span);
            return Convert.ToHexString(span);
#else
            byte[] array = new byte[16];
            fixed (byte* aptr = array)
            {
                *(MD5Hash*)aptr = key;
            }
            return array.ToHexString();
#endif
        }

        public static MD5Hash ToMD5(this byte[] array)
        {
            if (array.Length != 16)
                throw new ArgumentException("array size != 16", nameof(array));

            return Unsafe.As<byte, MD5Hash>(ref array[0]);
        }
    }

    public static class CStringExtensions
    {
        /// <summary> Reads the NULL terminated string from 
        /// the current stream and advances the current position of the stream by string length + 1.
        /// <seealso cref="BinaryReader.ReadString"/>
        /// </summary>
        public static string ReadCString(this BinaryReader reader)
        {
            return reader.ReadCString(Encoding.UTF8);
        }

        /// <summary> Reads the NULL terminated string from 
        /// the current stream and advances the current position of the stream by string length + 1.
        /// <seealso cref="BinaryReader.ReadString"/>
        /// </summary>
        public static string ReadCString(this BinaryReader reader, Encoding encoding)
        {
            var bytes = new List<byte>();
            byte b;
            while ((b = reader.ReadByte()) != 0)
                bytes.Add(b);
            return encoding.GetString(bytes.ToArray());
        }

        public static void WriteCString(this BinaryWriter writer, string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            writer.Write(bytes);
            writer.Write((byte)0);
        }

        public static byte[] ToByteArray(this string str)
        {
            str = str.Replace(" ", string.Empty);

            var res = new byte[str.Length / 2];
            for (int i = 0; i < res.Length; ++i)
            {
                res[i] = Convert.ToByte(str.Substring(i * 2, 2), 16);
            }
            return res;
        }

        public static byte[] FromHexString(this string str)
        {
#if NET5_0_OR_GREATER
            return Convert.FromHexString(str);
#else
            if (str == null)
                throw new ArgumentNullException(nameof(str));
            if (str.Length == 0)
                return Array.Empty<byte>();
            if ((uint)str.Length % 2 != 0)
                throw new FormatException("SR.Format_BadHexLength");

            byte[] result = new byte[str.Length >> 1];

            if (!HexConverter.TryDecodeFromUtf16(str, result))
                throw new FormatException("SR.Format_BadHexChar");

            return result;
#endif
        }
    }
}