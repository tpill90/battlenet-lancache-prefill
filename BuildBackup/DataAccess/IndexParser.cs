using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using BuildBackup.Structs;
using BuildBackup.Utils;
using MoreLinq;
using Shared;

namespace BuildBackup.DataAccess
{
    public static class IndexParser
    {
        public static Dictionary<string, IndexEntry> ParseIndex(string url, string hashId, CDN cdn, string folder)
        {
            byte[] indexContent = cdn.GetIndex($"{url}/{folder}/", hashId);

            var returnDict = new Dictionary<string, IndexEntry>();

            using (BinaryReader bin = new BinaryReader(new MemoryStream(indexContent)))
            {
                bin.BaseStream.Position = bin.BaseStream.Length - 28;

                var footer = new IndexFooter
                {
                    tocHash = bin.ReadBytes(8),
                    version = bin.ReadByte(),
                    unk0 = bin.ReadByte(),
                    unk1 = bin.ReadByte(),
                    blockSizeKB = bin.ReadByte(),
                    offsetBytes = bin.ReadByte(),
                    sizeBytes = bin.ReadByte(),
                    keySizeInBytes = bin.ReadByte(),
                    checksumSize = bin.ReadByte(),
                    numElements = bin.ReadUInt32()
                };

                footer.footerChecksum = bin.ReadBytes(footer.checksumSize);

                // TODO: Read numElements as BE if it is wrong as LE
                if ((footer.numElements & 0xff000000) != 0)
                {
                    bin.BaseStream.Position -= footer.checksumSize + 4;
                    footer.numElements = bin.ReadUInt32(true);
                }

                bin.BaseStream.Position = 0;

                var indexBlockSize = 1024 * footer.blockSizeKB;

                int indexEntries = indexContent.Length / indexBlockSize;
                var recordSize = footer.keySizeInBytes + footer.sizeBytes + footer.offsetBytes;
                var recordsPerBlock = indexBlockSize / recordSize;
                var blockPadding = indexBlockSize - (recordsPerBlock * recordSize);

                for (var b = 0; b < indexEntries; b++)
                {
                    for (var bi = 0; bi < recordsPerBlock; bi++)
                    {
                        var headerHash = BitConverter.ToString(bin.ReadBytes(footer.keySizeInBytes)).Replace("-", "");
                        var entry = new IndexEntry();

                        if (footer.sizeBytes == 4)
                        {
                            entry.size = bin.ReadUInt32(true);
                        }
                        else
                        {
                            throw new NotImplementedException("Index size reading other than 4 is not implemented!");
                        }

                        if (footer.offsetBytes == 4)
                        {
                            // Archive index
                            entry.offset = bin.ReadUInt32(true);
                        }
                        else if (footer.offsetBytes == 6)
                        {
                            // Group index
                            throw new NotImplementedException("Group index reading is not implemented!");
                        }
                        else
                        {
                            // File index
                            //Debugger.Break();
                        }

                        if (entry.size != 0)
                        {
                            if (!returnDict.ContainsKey(headerHash))
                            {
                                //TODO determine why hearthstone has an issue with this line
                                returnDict.Add(headerHash, entry);
                            }
                        }
                    }

                    bin.ReadBytes(blockPadding);
                }
            }

            return returnDict;
        }

        //TODO get URI from settings
        public static Dictionary<MD5Hash, IndexEntry> BuildArchiveIndexes(string url, CDNConfigFile cdnConfig, CDN cdn, TactProduct product, Uri blizzardCdnUri)
        {
            int CHUNK_SIZE = 4096;
            uint BlockSize = (1 << 20);

            Console.Write("Building archive indexes...".PadRight(Config.PadRight));
            var timer = Stopwatch.StartNew();

            var indexDictionary = new ConcurrentDictionary<MD5Hash, IndexEntry>(MD5HashComparer.Instance);

            Parallel.ForEach(cdnConfig.archives, new ParallelOptions { MaxDegreeOfParallelism = 20 }, (archive, state, i) =>
            {
                // Requests the actual index contents, and parses them into a useable format
                byte[] indexContent = cdn.GetIndex($"{url}/data/", cdnConfig.archives[i].hashId);

                using (var stream = new MemoryStream(indexContent))
                using (BinaryReader br = new BinaryReader(stream))
                {
                    #region footer
                    stream.Seek(-20, SeekOrigin.End);

                    byte version = br.ReadByte();

                    if (version != 1)
                        throw new InvalidDataException("ParseIndex -> version");

                    byte unk1 = br.ReadByte();

                    if (unk1 != 0)
                        throw new InvalidDataException("ParseIndex -> unk1");

                    byte unk2 = br.ReadByte();

                    if (unk2 != 0)
                        throw new InvalidDataException("ParseIndex -> unk2");

                    byte blockSizeKb = br.ReadByte();

                    if (blockSizeKb != 4)
                        throw new InvalidDataException("ParseIndex -> blockSizeKb");

                    byte offsetBytes = br.ReadByte();

                    if (offsetBytes != 4)
                        throw new InvalidDataException("ParseIndex -> offsetBytes");

                    byte sizeBytes = br.ReadByte();

                    if (sizeBytes != 4)
                        throw new InvalidDataException("ParseIndex -> sizeBytes");

                    byte keySizeBytes = br.ReadByte();

                    if (keySizeBytes != 16)
                        throw new InvalidDataException("ParseIndex -> keySizeBytes");

                    byte checksumSize = br.ReadByte();

                    if (checksumSize != 8)
                        throw new InvalidDataException("ParseIndex -> checksumSize");

                    int numElements = br.ReadInt32();

                    if (numElements * (keySizeBytes + sizeBytes + offsetBytes) > stream.Length)
                        throw new Exception("ParseIndex failed");

                    stream.Seek(0, SeekOrigin.Begin);

                    #endregion

                    for (int j = 0; j < numElements; j++)
                    {
                        MD5Hash key = br.Read<MD5Hash>();

                        var entry = new IndexEntry
                        {
                            index = (short)i,
                            size = br.ReadUInt32(true),
                            offset = br.ReadUInt32(true),
                            IndexId = cdnConfig.archives[i].hashId
                        };
                        if (!indexDictionary.ContainsKey(key))
                        {
                            if (indexDictionary.TryAdd(key, entry))
                            {
                            }
                            else
                            {
                                Console.WriteLine($"could not add {key.ToString()}, it was already added.");
                            }
                        }

                        // each chunk is 4096 bytes, and zero padding at the end
                        long remaining = CHUNK_SIZE - (stream.Position % CHUNK_SIZE);

                        // skip padding
                        if (remaining < 16 + 4 + 4)
                        {
                            stream.Position += remaining;
                        }
                    }
                }
            });


            // Building mask sizes
            //TODO reenable later
            //var fileSizeProvider = new FileSizeProvider(product, blizzardCdnUri.ToString());
            //for (int i = 0; i < cdnConfig.archives.Length; i++)
            //{
            //    var hashId = cdnConfig.archives[i].hashId.ToLower();
            //    var uri = $"{url}/data/{hashId.Substring(0, 2)}/{hashId.Substring(2, 2)}/{hashId}";
            //    var contentLength = fileSizeProvider.GetContentLength(new Request() { Uri = uri });

            //    long chunks = (contentLength + BlockSize - 1) / BlockSize;
            //    var size = (chunks + 7) / 8;
            //    cdnConfig.archives[i].mask = new byte[(int)size];

            //    for (int k = 0; k < size; k++)
            //    {
            //        cdnConfig.archives[i].mask[k] = 0xFF;
            //    }
            //}
            //fileSizeProvider.Save();

            timer.Stop();
            Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}".PadLeft(Config.Padding));

            

            return indexDictionary.ToDictionary();
        }

        private static List<string> ParsePatchFileIndex(string url, string hash, CDN cdn)
        {
            byte[] indexContent = cdn.Get(url + "/patch/", hash);

            var list = new List<string>();

            using (BinaryReader bin = new BinaryReader(new MemoryStream(indexContent)))
            {
                int indexEntries = indexContent.Length / 4096;

                for (var b = 0; b < indexEntries; b++)
                {
                    for (var bi = 0; bi < 170; bi++)
                    {
                        var headerHash = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");

                        var size = bin.ReadUInt32(true);

                        list.Add(headerHash);
                    }
                    bin.ReadBytes(16);
                }
            }

            return list;
        }
    }
}
