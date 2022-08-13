using System;
using System.IO;
using System.IO.Compression;
using BattleNetPrefill.Extensions;
using BattleNetPrefill.Structs;

namespace BattleNetPrefill.EncryptDecrypt
{
    public static class BLTE
    {
        public static MemoryStream Parse(byte[] content)
        {
            var resultStream = MemoryStreamExtensions.MemoryStreamManager.GetStream();
            using var inputStream = content.GetAsMemoryStream();
            using var bin = new BinaryReader(inputStream);

            if (bin.ReadUInt32() != 0x45544c42)
            {
                throw new Exception("Not a BLTE file");
            }

            BLTEChunkInfo[] chunkInfos;

            var blteSize = bin.ReadUInt32BigEndian();
            if (blteSize == 0)
            {
                // These are always uncompressed
                chunkInfos = new BLTEChunkInfo[1];
                chunkInfos[0].isFullChunk = false;
                chunkInfos[0].compSize = Convert.ToInt32(bin.BaseStream.Length - 8);
                chunkInfos[0].decompSize = Convert.ToInt32(bin.BaseStream.Length - 8 - 1);
                chunkInfos[0].checkSum = new byte[16];
            }
            else
            {
                var bytes = bin.ReadBytes(4);
                var chunkCount = bytes[1] << 16 | bytes[2] << 8 | bytes[3] << 0;

                var supposedHeaderSize = 24 * chunkCount + 12;
                if (supposedHeaderSize != blteSize)
                {
                    throw new Exception("Invalid header size!");
                }
                if (supposedHeaderSize > bin.BaseStream.Length)
                {
                    throw new Exception("Not enough data");
                }

                chunkInfos = new BLTEChunkInfo[chunkCount];
                for (int i = 0; i < chunkCount; i++)
                {
                    chunkInfos[i].isFullChunk = true;
                    chunkInfos[i].compSize = bin.ReadInt32BigEndian();
                    chunkInfos[i].decompSize = bin.ReadInt32BigEndian();
                    chunkInfos[i].checkSum = bin.ReadBytes(16);
                }
            }

            foreach (var chunk in chunkInfos)
            {
                if (chunk.compSize > (bin.BaseStream.Length - bin.BaseStream.Position))
                {
                    throw new Exception("Trying to read more than is available!");
                }
                HandleDataBlock(bin, chunk, resultStream);
            }

            // Reset the result stream, and hand it back to the caller
            resultStream.Seek(0, SeekOrigin.Begin);
            return resultStream;
        }
        
        private static void HandleDataBlock(BinaryReader bin, BLTEChunkInfo chunk, MemoryStream result)
        {
            var chunkType = bin.ReadByte();
            switch (chunkType)
            {
                case 0x4E: // N (no compression)
                    bin.BaseStream.CopyStream(result, chunk.compSize - 1);
                    break;
                case 0x5A: // Z (zlib, compressed)
                    var buffer = bin.ReadBytes(chunk.compSize - 1);
                    using (var stream = new MemoryStream(buffer, 3 - 1, chunk.compSize - 3 - 1))
                    using (var ds = new DeflateStream(stream, CompressionMode.Decompress))
                    {
                        ds.CopyTo(result);
                    }
                    break;
                case 0x45: // E (encrypted)
                    // Removed encryption from implementation, as it is not used by TACT alone.  
                    throw new NotImplementedException("BLTE decryption not supported!");
                case 0x46: // F (frame)
                    throw new NotImplementedException("BLTE frame not supported!");
                default:
                    throw new Exception($"Unsupported mode {chunkType:X}!");
            }
        }
    }
}