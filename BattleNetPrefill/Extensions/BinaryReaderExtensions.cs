namespace BattleNetPrefill.Extensions
{
    public static class BinaryReaderExtensions
    {
        public static short ReadInt16BigEndian(this BinaryReader reader)
        {
            return BinaryPrimitives.ReadInt16BigEndian(reader.ReadBytes(2));
        }

        public static int ReadInt32BigEndian(this BinaryReader reader)
        {
            return BinaryPrimitives.ReadInt32BigEndian(reader.ReadBytes(4));
        }

        public static ushort ReadUInt16BigEndian(this BinaryReader reader)
        {
            return BinaryPrimitives.ReadUInt16BigEndian(reader.ReadBytes(2));
        }

        public static uint ReadUInt32BigEndian(this BinaryReader reader)
        {
            return BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(4));
        }

        /// <summary>
        /// Reads an <seealso cref="uint"/> using a shared buffer, to reduce required allocations.
        /// Caller is required to pass a buffer of the correct size, using <seealso cref="AllocateBuffer{T}"/>
        /// </summary>
        public static uint ReadUInt32BigEndian(this BinaryReader reader, byte[] buffer)
        {
            reader.Read(buffer, 0, buffer.Length);
            return BinaryPrimitives.ReadUInt32BigEndian(buffer);
        }

        /// <summary>
        /// Reads an <seealso cref="MD5Hash"/> using a shared buffer, to reduce required allocations.
        /// Caller is required to pass a buffer of the correct size, using <seealso cref="AllocateBuffer{T}"/>
        /// </summary>
        public static MD5Hash ReadMd5Hash(this BinaryReader reader, byte[] buffer)
        {
            reader.Read(buffer, 0, buffer.Length);
            return Unsafe.ReadUnaligned<MD5Hash>(ref buffer[0]);
        }

        /// <summary>
        /// Allocates a byte array sized for type T.  Intended to be used along side <seealso cref="ReadMd5Hash"/>
        /// </summary>
        public static byte[] AllocateBuffer<T>() where T : unmanaged
        {
            return new byte[Unsafe.SizeOf<T>()];
        }

        public static T Read<T>(this BinaryReader reader) where T : unmanaged
        {
            byte[] result = reader.ReadBytes(Unsafe.SizeOf<T>());

            return Unsafe.ReadUnaligned<T>(ref result[0]);
        }

        /// <summary>
        /// Reads the NULL terminated string from the current stream and advances the current position of the stream by string length + 1.
        /// <seealso cref="BinaryReader.ReadString"/>
        /// </summary>
        public static string ReadCString(this BinaryReader reader)
        {
            var bytes = new List<byte>();
            byte b;
            while ((b = reader.ReadByte()) != 0)
            {
                bytes.Add(b);
            }
            return Encoding.UTF8.GetString(bytes.ToArray());
        }
    }
}