using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Utils;
using Spectre.Console;

namespace BattleNetPrefill.EncryptDecrypt
{
    //TODO determine if this is really needed to be kept around
    public static class BLTE
    {
        public static byte[] Parse(byte[] content)
        {
            using (var result = new MemoryStream())
            using (var bin = new BinaryReader(new MemoryStream(content)))
            {
                if (bin.ReadUInt32() != 0x45544c42)
                {
                    throw new Exception("Not a BLTE file");
                }

                var blteSize = bin.ReadUInt32(true);

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
                        chunkInfos[i].compSize = bin.ReadInt32(true);
                        chunkInfos[i].decompSize = bin.ReadInt32(true);
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

                    HandleDataBlock(bin.ReadBytes(chunk.compSize), index, chunk, result);
                }

                return result.ToArray();
            }
        }

        private static void HandleDataBlock(byte[] data, int index, BLTEChunkInfo chunk, MemoryStream result)
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
                    byte[] decrypted = new byte[data.Length - 15];
                    decrypted[0] = 0x4E; // N
                    try
                    {
                        decrypted = Decrypt(data, index);
                    }
                    catch (KeyNotFoundException e)
                    {
                        AnsiConsole.WriteLine(e.Message);
                        result.Write(new byte[chunk.decompSize], 0, chunk.decompSize);
                        break;
                    }

                    // Override inFileSize with decrypted length because it now differs from original encrypted chunk.compSize which breaks decompression
                    chunk.compSize = decrypted.Length;

                    HandleDataBlock(decrypted, index, chunk, result);
                    break;
                case 0x46: // F (frame)
                default:
                    throw new Exception("Unsupported mode " + data[0].ToString("X") + "!");
            }
        }

        private static byte[] Decrypt(byte[] data, int index)
        {
            byte keyNameSize = data[1];

            if (keyNameSize == 0 || keyNameSize != 8)
                throw new Exception("keyNameSize == 0 || keyNameSize != 8");

            byte[] keyNameBytes = new byte[keyNameSize];
            Array.Copy(data, 2, keyNameBytes, 0, keyNameSize);

            ulong keyName = BitConverter.ToUInt64(keyNameBytes, 0);

            byte IVSize = data[keyNameSize + 2];

            if (IVSize != 4 || IVSize > 0x10)
                throw new Exception("IVSize != 4 || IVSize > 0x10");

            byte[] IVpart = new byte[IVSize];
            Array.Copy(data, keyNameSize + 3, IVpart, 0, IVSize);

            if (data.Length < IVSize + keyNameSize + 4)
                throw new Exception("data.Length < IVSize + keyNameSize + 4");

            int dataOffset = keyNameSize + IVSize + 3;

            byte encType = data[dataOffset];

            if (encType != 'S' && encType != 'A') // 'S' or 'A'
                throw new Exception("encType != ENCRYPTION_SALSA20 && encType != ENCRYPTION_ARC4");

            dataOffset++;

            // expand to 8 bytes
            byte[] IV = new byte[8];
            Array.Copy(IVpart, IV, IVpart.Length);

            // magic
            for (int shift = 0, i = 0; i < sizeof(int); shift += 8, i++)
            {
                IV[i] ^= (byte)((index >> shift) & 0xFF);
            }

            byte[] key = KeyService.GetKey(keyName);

            if (key == null)
                throw new KeyNotFoundException("Unknown keyname " + keyName.ToString("X16"));

            if (encType == 'S')
            {
                ICryptoTransform decryptor = KeyService.SalsaInstance.CreateDecryptor(key, IV);

                return decryptor.TransformFinalBlock(data, dataOffset, data.Length - dataOffset);
            }
            else
            {
                // ARC4 ?
                throw new Exception("encType ENCRYPTION_ARC4 not implemented");
            }
        }
    }
}