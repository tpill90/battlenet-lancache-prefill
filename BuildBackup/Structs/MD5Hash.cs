using System;

namespace BuildBackup.Structs
{
    public readonly struct MD5Hash : IEquatable<MD5Hash>
    {
        public readonly ulong lowPart;
        public readonly ulong highPart;

        public bool Equals(MD5Hash other)
        {
            return other.lowPart == lowPart && other.highPart == highPart;
        }

        public override string ToString()
        {
            return (BitConverter.ToString(BitConverter.GetBytes(lowPart)) 
                    + BitConverter.ToString(BitConverter.GetBytes(highPart)))
                    .Replace("-", "");
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
