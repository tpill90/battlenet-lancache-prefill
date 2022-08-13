using System;
using System.IO;
using Microsoft.IO;

namespace BattleNetPrefill.Extensions
{
    public static class MemoryStreamExtensions
    {
        public static readonly RecyclableMemoryStreamManager MemoryStreamManager = new RecyclableMemoryStreamManager();

        public static MemoryStream GetAsMemoryStream(this byte[] buffer)
        {
            MemoryStream stream = MemoryStreamManager.GetStream();
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
    }
}
