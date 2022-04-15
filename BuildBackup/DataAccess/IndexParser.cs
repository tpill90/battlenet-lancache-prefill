using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using BuildBackup.Structs;
using BuildBackup.Utils;
using Shared;

namespace BuildBackup.DataAccess
{
    public static class IndexParser
    {
        public static Dictionary<string, IndexEntry> ParseIndex(string hashId, CDN cdn, RootFolder folder)
        {
            byte[] indexContent = cdn.GetIndex(folder, hashId).Result;

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
                        //TODO get rid of this
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

        //TODO break this out into ArchiveIndexHandler
        public static List<Dictionary<MD5Hash, IndexEntry>> BuildArchiveIndexes(CDNConfigFile cdnConfig, CDN cdn, TactProduct targetProduct)
        {
            Console.Write("Building archive indexes...".PadRight(Config.PadRight));
            var timer = Stopwatch.StartNew();

            var tasks = new List<Task<Dictionary<MD5Hash, IndexEntry>>>();

            // This default performs well for most TactProducts.
            int maxTasks = 3;
            // Overwatch's indexes parse significantly faster when increasing the concurrency.
            if (targetProduct == TactProducts.Overwatch)
            {
                maxTasks = 6;
            }

            int sliceAmount = cdnConfig.archives.Length / maxTasks;

            for (int i = 0; i <= maxTasks; i++)
            {
                var lowerLimit = (i * sliceAmount);
                int upperLimit = Math.Min(((i + 1) * sliceAmount) - 1, cdnConfig.archives.Length - 1);

                tasks.Add(ProcessArchive(cdnConfig, cdn, lowerLimit, upperLimit));
            }

            Task.WhenAll(tasks).Wait();
            var indexDictList = new List<Dictionary<MD5Hash, IndexEntry>>();
            foreach (var task in tasks)
            {
                indexDictList.Add(task.GetAwaiter().GetResult());
            }
            timer.Stop();
            Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}".PadLeft(Config.Padding));

            return indexDictList;
        }

        private static async Task<Dictionary<MD5Hash, IndexEntry>> ProcessArchive(CDNConfigFile cdnConfig, CDN cdn, int start, int finish)
        {
            int CHUNK_SIZE = 4096;

			//TODO does pre allocating help?
            var indexDictionary = new Dictionary<MD5Hash, IndexEntry>(1000, MD5HashEqualityComparer.Instance);

            for (int i = start; i <= finish; i++)
            {
                byte[] indexContent = await cdn.GetIndex(RootFolder.data, cdnConfig.archives[i].hashId);

                using (var stream = new MemoryStream(indexContent))
                using (BinaryReader br = new BinaryReader(stream))
                {
                    var numElements = ValidateArchiveIndexFooter(stream, br);

                    for (int j = 0; j < numElements; j++)
                    {
                        MD5Hash key = br.Read<MD5Hash>();

                        var entry = new IndexEntry
                        {
                            //key = br.Read<MD5Hash>(),
                            index = (short)i,
                            size = br.ReadUInt32(true),
                            offset = br.ReadUInt32(true)
                        };
                        indexDictionary.Add(key, entry);

                        // each chunk is 4096 bytes, and zero padding at the end
                        long remaining = CHUNK_SIZE - (stream.Position % CHUNK_SIZE);

                        // skip padding
                        if (remaining < 16 + 4 + 4)
                        {
                            stream.Position += remaining;
                        }
                    }
                }
            }


            return indexDictionary;
        }

        //TODO comment
        private static int ValidateArchiveIndexFooter(MemoryStream stream, BinaryReader br)
        {
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
            return numElements;
        }

        public static int ContainsKey(List<Dictionary<MD5Hash, IndexEntry>> indexDictionaries, MD5Hash lookupKey)
        {
            for (var index = 0; index < indexDictionaries.Count; index++)
            {
                var dict = indexDictionaries[index];
                if (dict.ContainsKey(lookupKey))
                {
                    return index;
                }
            }

            return -1;
        }

        public static bool ContainsKeyBool(List<Dictionary<MD5Hash, IndexEntry>> indexDictionaries, MD5Hash lookupKey)
        {
            foreach (var dict in indexDictionaries)
            {
                if (dict.ContainsKey(lookupKey))
                {
                    return true;
                }
            }

            return false;
        }

        public static IndexEntry TryGet(List<Dictionary<MD5Hash, IndexEntry>> indexDictionaries, MD5Hash lookupKey, int archiveIndex)
        {
            return indexDictionaries[archiveIndex][lookupKey];
        }

        public static IndexEntry? TryGet(List<Dictionary<MD5Hash, IndexEntry>> indexDictionaries, MD5Hash lookupKey)
        {
            foreach (var dict in indexDictionaries)
            {
                if (dict.TryGetValue(lookupKey, out IndexEntry returnValue))
                {
                    return returnValue;
                }
            }

            return null;
        }
    }
}
