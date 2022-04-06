using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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

        private static Dictionary<string, IndexEntry> _archiveIndexDictionary;

        public static Dictionary<string, IndexEntry> BuildArchiveIndexes(string url, CDNConfigFile cdnConfig, CDN cdn)
        {
            if (_archiveIndexDictionary != null)
            {
                return _archiveIndexDictionary;
            }

            Console.Write("Building archive indexes...");
            var timer = Stopwatch.StartNew();
            var indexDictionary = new ConcurrentDictionary<string, IndexEntry>();
            Parallel.ForEach(cdnConfig.archives, new ParallelOptions { MaxDegreeOfParallelism = 20 }, (archive, state, i) =>
            {
                // Requests the actual index contents, and parses them into a useable format
                byte[] indexContent = cdn.GetIndex($"{url}/data/", cdnConfig.archives[i]);

                using (BinaryReader bin = new BinaryReader(new MemoryStream(indexContent)))
                {
                    int indexEntries = indexContent.Length / 4096;

                    for (var b = 0; b < indexEntries; b++)
                    {
                        for (var bi = 0; bi < 170; bi++)
                        {
                            var headerHash = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");

                            var entry = new IndexEntry
                            {
                                index = (short) i,
                                size = bin.ReadUInt32(true),
                                offset = bin.ReadUInt32(true),
                                IndexId = cdnConfig.archives[i]
                            };

                            if (!indexDictionary.ContainsKey(headerHash))
                            {
                                if (indexDictionary.TryAdd(headerHash, entry))
                                {
                                }
                                else
                                {
                                    Console.WriteLine($"could not add {headerHash}, it was already added.");
                                }
                            }
                        }

                        bin.ReadBytes(16);
                    }
                }
            });

            timer.Stop();
            Console.WriteLine($" Done! {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");
            _archiveIndexDictionary = indexDictionary.ToDictionary();
            return _archiveIndexDictionary;
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
