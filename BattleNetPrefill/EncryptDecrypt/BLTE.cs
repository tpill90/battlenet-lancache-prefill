using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Utils;

namespace BattleNetPrefill.EncryptDecrypt
{
    //TODO determine if this is really needed to be kept around
    public static class BLTE
    {
        public static byte[] Parse(byte[] content)
        {
            using var result = new MemoryStream();
            using var bin = new BinaryReader(new MemoryStream(content));

            if (bin.ReadUInt32() != 0x45544c42)
            {
                throw new Exception("Not a BLTE file");
            }

            var blteSize = bin.ReadUInt32BigEndian();

            BLTEChunkInfo[] chunkInfos;

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
                    chunkInfos[i].checkSum = new byte[16];
                    chunkInfos[i].checkSum = bin.ReadBytes(16);
                }
            }

            for (var index = 0; index < chunkInfos.Count(); index++)
            {
                var chunk = chunkInfos[index];

                if (chunk.compSize > (bin.BaseStream.Length - bin.BaseStream.Position))
                {
                    throw new Exception("Trying to read more than is available!");
                }

                HandleDataBlock(bin.ReadBytes(chunk.compSize), chunk, result);
            }

            return result.ToArray();
        }

        private static void HandleDataBlock(byte[] data, BLTEChunkInfo chunk, MemoryStream result)
        {
            switch (data[0])
            {
                case 0x4E: // N (no compression)
                    result.Write(data, 1, data.Length - 1);
                    break;
                case 0x5A: // Z (zlib, compressed)
                    using (var stream = new MemoryStream(data, 3, chunk.compSize - 3))
                    using (var ds = new DeflateStream(stream, CompressionMode.Decompress))
                    {
                        ds.CopyTo(result);
                    }
                    break;
                case 0x45: // E (encrypted)
                    // Removed encryption from implementation, as it is not used by TACT alone.  
                    throw new NotImplementedException("BLTE decryption not supported!");
                case 0x46: // F (frame)
                default:
                    throw new Exception("Unsupported mode " + data[0].ToString("X") + "!");
            }
        }
    }
}