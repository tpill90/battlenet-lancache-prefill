using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using BuildBackup.Structs;

//TODO rename file to just Extensions.cs
namespace System.IO
{
    public static class BinaryReaderExtensions
    {
        public static int ReadInt32BE(this BinaryReader reader)
        {
            int val = reader.ReadInt32();
            int ret = (val >> 24 & 0xFF) << 0;
            ret |= (val >> 16 & 0xFF) << 8;
            ret |= (val >> 8 & 0xFF) << 16;
            ret |= (val >> 0 & 0xFF) << 24;
            return ret;
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

        //TODO comment.  This is the opposite of ReadUInt32
        public static UInt32 ReadUInt32InvertEndian(this BinaryReader reader)
        {
            return BitConverter.ToUInt32(reader.ReadInvertedBytes(4), 0);
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

        public static MD5Hash ToMD5(this string str)
        {
            var array = Convert.FromHexString(str);
            
            if (array.Length != 16)
                throw new ArgumentException("array size != 16", nameof(array));

            return Unsafe.As<byte, MD5Hash>(ref array[0]);
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
            return Convert.FromHexString(str);
        }
    }

    public static class Extensions
    {
        // copies whole stream
        public static MemoryStream CopyToMemoryStream(this Stream src)
        {
            MemoryStream ms = new MemoryStream();
            src.CopyTo(ms);
            ms.Position = 0;
            return ms;
        }
    }
}