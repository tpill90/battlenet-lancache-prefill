using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Utils.Debug.Models;
using ByteSizeLib;
using CliFx.Infrastructure;
using Spectre.Console;

namespace BattleNetPrefill.Utils
{
    public static class BinaryReaderExtensions
    {
        public static short ReadInt16BigEndian(this BinaryReader reader)
        {
            byte[] val = reader.ReadBytes(2);
            return (short)(val[1] | val[0] << 8);
        }

        public static Int32 ReadInt32BigEndian(this BinaryReader reader)
        {
            return BitConverter.ToInt32(reader.ReadInvertedBytes(4), 0);
        }

        public static UInt16 ReadUInt16BigEndian(this BinaryReader reader)
        {
            return BitConverter.ToUInt16(reader.ReadInvertedBytes(2), 0);
        }

        public static UInt32 ReadUInt32BigEndian(this BinaryReader reader)
        {
            return BitConverter.ToUInt32(reader.ReadInvertedBytes(4), 0);
        }

        public static UInt64 ReadUInt40BigEndian(this BinaryReader reader)
        {
            ulong b1 = reader.ReadByte();
            ulong b2 = reader.ReadByte();
            ulong b3 = reader.ReadByte();
            ulong b4 = reader.ReadByte();
            ulong b5 = reader.ReadByte();

            return (ulong)(b1 << 32 | b2 << 24 | b3 << 16 | b4 << 8 | b5);
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

        public static MD5Hash ToMD5(this string str)
        {
            var array = Convert.FromHexString(str);
            
            if (array.Length != 16)
            {
                throw new ArgumentException("array size != 16", nameof(array));
            }

            return Unsafe.As<byte, MD5Hash>(ref array[0]);
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

    public static class AnsiConsoleExtensions
    {
        public static IAnsiConsole CreateAnsiConsole(this IConsole console)
        {
            return AnsiConsole.Create(new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.Detect,
                ColorSystem = ColorSystemSupport.Detect,
                Out = new AnsiConsoleOutput(console.Output)
            });
        }

        public static Status CreateSpectreStatusSpinner(this IAnsiConsole ansiConsole)
        {
            return ansiConsole.Status()
                              .AutoRefresh(true)
                              .SpinnerStyle(Style.Parse("green"))
                              .Spinner(Spinner.Known.Dots2);
        }

        public static Progress CreateSpectreProgress(this IAnsiConsole ansiConsole)
        {
            var spectreProgress = ansiConsole.Progress()
                                             .HideCompleted(true)
                                             .AutoClear(true)
                                             .Columns(
                                                 new TaskDescriptionColumn(),
                                                 new ProgressBarColumn(), 
                                                 new PercentageColumn(), 
                                                 new RemainingTimeColumn(), 
                                                 new DownloadedColumn(), 
                                                 new TransferSpeedColumn());
            return spectreProgress;
        }

    }

    public static class Extensions
    {
        public static ByteSize SumTotalBytes(this List<Request> requests)
        {
            return ByteSize.FromBytes(requests.Sum(e => e.TotalBytes));
        }
    }
}