using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using BuildBackup.Structs;
using BuildBackup.Utils;
using Shared;

namespace BuildBackup.Handlers
{
    //TODO document this class
    //TODO remove static
    public static class ArchiveIndexHandler
    {
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

                if (lowerLimit > cdnConfig.archives.Length)
                {
                    continue;
                }
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

            //TODO does pre allocating help performance?
            var indexDictionary = new Dictionary<MD5Hash, IndexEntry>(MD5HashEqualityComparer.Instance);

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
