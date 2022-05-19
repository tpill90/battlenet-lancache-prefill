using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Utils.Debug.Models;
using ByteSizeLib;
using CliFx.Infrastructure;
using Microsoft.IO;
using Spectre.Console;

namespace BattleNetPrefill.Utils
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

        public static ulong ReadUInt40BigEndian(this BinaryReader reader)
        {
            ulong b1 = reader.ReadByte();
            ulong b2 = reader.ReadByte();
            ulong b3 = reader.ReadByte();
            ulong b4 = reader.ReadByte();
            ulong b5 = reader.ReadByte();

            return (ulong)(b1 << 32 | b2 << 24 | b3 << 16 | b4 << 8 | b5);
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
        public static readonly RecyclableMemoryStreamManager MemoryStreamManager = new RecyclableMemoryStreamManager();

        public static MemoryStream GetAsMemoryStream(this byte[] buffer)
        {
            MemoryStream stream = Extensions.MemoryStreamManager.GetStream();
            stream.Write(buffer, 0, buffer.Length);
            stream.Position = 0;

            return stream;
        }

        /// <summary>
        /// Copies the specified number of bytes to another stream
        /// </summary>
        /// <param name="input">Stream to copy from</param>
        /// <param name="output">Stream to copy to</param>
        /// <param name="bytes">Number of bytes to copy</param>
        public static void CopyStream(this Stream input, Stream output, int bytes)
        {
            byte[] buffer = new byte[4096];
            int read;
            while (bytes > 0 && (read = input.Read(buffer, 0, Math.Min(buffer.Length, bytes))) > 0)
            {
                output.Write(buffer, 0, read);
                bytes -= read;
            }
        }

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