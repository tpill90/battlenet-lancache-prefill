using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Utils.Debug.Models;
using ByteSizeLib;

namespace BattleNetPrefill.Extensions
{
    public static class Extensions
    {
        public static ByteSize SumTotalBytes(this List<Request> requests)
        {
            return ByteSize.FromBytes(requests.Sum(e => e.TotalBytes));
        }

        public static MD5Hash ToMD5(this string str)
        {
            if (str.Length != 32)
            {
                throw new ArgumentException("input string length != 32", nameof(str));
            }
            var array = Convert.FromHexString(str);
            return Unsafe.As<byte, MD5Hash>(ref array[0]);
        }
    }
}