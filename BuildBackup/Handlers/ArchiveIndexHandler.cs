﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using BuildBackup.Structs;
using BuildBackup.Utils;
using BuildBackup.Web;
using Shared;

namespace BuildBackup.Handlers
{
    //TODO document this class
    //TODO remove static
    public class ArchiveIndexHandler
    {
        private readonly CDN _cdn;
        private readonly TactProduct _targetProduct;

        private const int CHUNK_SIZE = 4096;

        //TODO comment
        private List<Dictionary<MD5Hash, IndexEntry>> _indexDictionaries = new List<Dictionary<MD5Hash, IndexEntry>>();

        public ArchiveIndexHandler(CDN cdn, TactProduct targetProduct)
        {
            _cdn = cdn;
            _targetProduct = targetProduct;
        }

        //TODO cleanup
        public void BuildArchiveIndexes(CDNConfigFile cdnConfig)
        {
            Console.Write("Building archive indexes...".PadRight(Config.PadRight));
            var timer = Stopwatch.StartNew();

            var tasks = new List<Task<Dictionary<MD5Hash, IndexEntry>>>();

            // This default performs well for most TactProducts.
            int maxTasks = 3;
            // Overwatch's indexes parse significantly faster when increasing the concurrency.
            if (_targetProduct == TactProducts.Overwatch)
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
                tasks.Add(ProcessArchiveAsync(cdnConfig, _cdn, lowerLimit, upperLimit));
            }

            Task.WhenAll(tasks).Wait();
            
            foreach (var task in tasks)
            {
                _indexDictionaries.Add(task.GetAwaiter().GetResult());
            }
            timer.Stop();
            Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}".PadLeft(Config.Padding));
        }
        
        private async Task<Dictionary<MD5Hash, IndexEntry>> ProcessArchiveAsync(CDNConfigFile cdnConfig, CDN cdn, int start, int finish)
        {
            //TODO does pre allocating help performance?
            var indexDictionary = new Dictionary<MD5Hash, IndexEntry>(MD5HashEqualityComparer.Instance);
            
            for (int i = start; i <= finish; i++)
            {
                byte[] indexContent = await cdn.GetRequestAsBytes(RootFolder.data, cdnConfig.archives[i].hashId, isIndex: true);

                using (var stream = new MemoryStream(indexContent))
                using (BinaryReader br = new BinaryReader(stream))
                {
                    var numElements = ValidateArchiveIndexFooter(stream, br);

                    for (int j = 0; j < numElements; j++)
                    {
                        MD5Hash key = br.Read<MD5Hash>();

                        var entry = new IndexEntry
                        {
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
        private int ValidateArchiveIndexFooter(MemoryStream stream, BinaryReader br)
        {
            // Footer should always be the last 20 bytes of the file
            stream.Seek(-20, SeekOrigin.End);

            if (br.ReadByte() != 1)
            {
                throw new InvalidDataException("ParseIndex -> version");
            }
            if (br.ReadByte() != 0)
            {
                throw new InvalidDataException("ParseIndex -> unk1");
            }
            if (br.ReadByte() != 0)
            {
                throw new InvalidDataException("ParseIndex -> unk2");
            }
            if (br.ReadByte() != 4)
            {
                throw new InvalidDataException("ParseIndex -> blockSizeKb");
            }

            byte offsetBytes = br.ReadByte();
            if (offsetBytes != 4)
                throw new InvalidDataException("ParseIndex -> offsetBytes");

            byte sizeBytes = br.ReadByte();
            if (sizeBytes != 4)
                throw new InvalidDataException("ParseIndex -> sizeBytes");

            byte keySizeBytes = br.ReadByte();
            if (keySizeBytes != 16)
                throw new InvalidDataException("ParseIndex -> keySizeBytes");

            if (br.ReadByte() != 8)
                throw new InvalidDataException("ParseIndex -> checksumSize");

            int numElements = br.ReadInt32();
            if (numElements * (keySizeBytes + sizeBytes + offsetBytes) > stream.Length)
                throw new Exception("ParseIndex failed");

            stream.Seek(0, SeekOrigin.Begin);
            return numElements;
        }

        //TODO document
        public IndexEntry? TryGet(in MD5Hash lookupKey)
        {
            foreach (var dict in _indexDictionaries)
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