using HexMate;

namespace BattleNetPrefill.Structs
{
    public readonly struct MD5Hash : IEquatable<MD5Hash>
    {
        /// <summary>
        /// This is actually the "higher" part of the hash.  Left most chunk of data.
        /// </summary>
        [JsonInclude]
        public readonly ulong lowPart;

        [JsonInclude]
        public readonly ulong highPart;

        [JsonConstructor]
        public MD5Hash(ulong lowPart, ulong highPart)
        {
            this.lowPart = lowPart;
            this.highPart = highPart;
        }

        public override int GetHashCode()
        {
            return Md5HashEqualityComparer.Instance.GetHashCode(this);
        }

        //TODO benchmark equality again
        public bool Equals(MD5Hash other)
        {
            return other.lowPart == lowPart && other.highPart == highPart;
        }

        public override bool Equals(object obj)
        {
            MD5Hash other = (MD5Hash)obj;
            return other.lowPart == lowPart && other.highPart == highPart;
        }

        public override string ToString()
        {
            var bytes = new byte[16];
            BitConverter.TryWriteBytes(bytes.AsSpan(0, 8), lowPart);
            BitConverter.TryWriteBytes(bytes.AsSpan(8, 8), highPart);
            return HexMate.Convert.ToHexString(bytes);
        }

        public string ToStringLower()
        {
            var bytes = new byte[16];
            BitConverter.TryWriteBytes(bytes.AsSpan(0, 8), lowPart);
            BitConverter.TryWriteBytes(bytes.AsSpan(8, 8), highPart);
            return HexMate.Convert.ToHexString(bytes, HexFormattingOptions.Lowercase);
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
