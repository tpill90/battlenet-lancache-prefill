using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BattleNetPrefill.Structs;

namespace BattleNetPrefill.Extensions
{
    public static class MiscExtensions
    {
        public static MD5Hash ToMD5(this string str)
        {
            if (str.Length != 32)
            {
                throw new ArgumentException("input string length != 32", nameof(str));
            }
            var array = Convert.FromHexString(str);
            return Unsafe.As<byte, MD5Hash>(ref array[0]);
        }

        public static string FormatElapsedString(this Stopwatch stopwatch)
        {
            var elapsed = stopwatch.Elapsed;
            if (elapsed.TotalHours > 1)
            {
                return elapsed.ToString(@"h\:mm\:ss\.FF");
            }
            if (elapsed.TotalMinutes > 1)
            {
                return elapsed.ToString(@"mm\:ss\.FF");
            }
            return elapsed.ToString(@"ss\.FF");
        }
    }
}