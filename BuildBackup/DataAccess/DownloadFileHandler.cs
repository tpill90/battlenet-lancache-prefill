using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuildBackup.Structs;
using Shared;

namespace BuildBackup.DataAccess
{
    public static class DownloadFileHandler
    {
        public static DownloadFile ParseDownloadFile(CDN cdn, BuildConfigFile buildConfig)
        {
            var download = new DownloadFile();

            var hash = buildConfig.download[1].ToString().ToLower();

            byte[] content = cdn.Get(RootFolder.data, hash);

            using (BinaryReader bin = new BinaryReader(new MemoryStream(BLTE.Parse(content))))
            {
                if (Encoding.UTF8.GetString(bin.ReadBytes(2)) != "DL")
                {
                    throw new Exception("Error while parsing download file. Did BLTE header size change?");
                }
                byte version = bin.ReadBytes(1)[0];
                byte hash_size_ekey = bin.ReadBytes(1)[0];
                byte has_checksum_in_entry = bin.ReadBytes(1)[0];
                download.numEntries = bin.ReadUInt32(true);
                download.numTags = bin.ReadUInt16(true);

                

                int entryExtra = 6;
                if (has_checksum_in_entry > 0)
                {
                    entryExtra += 4;
                }
                if (version >= 2)
                {
                    byte number_of_flag_bytes = bin.ReadBytes(1)[0];
                    entryExtra += number_of_flag_bytes;
                    if (version >= 3)
                    {
                        bin.BaseStream.Seek(16, SeekOrigin.Begin);
                    }
                }

                // Reading the download entries
                //bin.BaseStream.Seek(16, SeekOrigin.Begin);
                download.entries = new DownloadEntry[download.numEntries];
                for (int i = 0; i < download.numEntries; i++)
                {
                    download.entries[i].hash = bin.Read<MD5Hash>();
                    bin.ReadBytes(entryExtra);
                }

                // Reading the tags
                int numMaskBytes = (int)((download.numEntries + 7) / 8);
                download.tags = new DownloadTag[download.numTags];

                for (int i = 0; i < download.numTags; i++)
                {
                    DownloadTag tag = new DownloadTag();
                    tag.Name = bin.ReadCString();
                    tag.Type = bin.ReadInt16BE();

                    byte[] bits = bin.ReadBytes(numMaskBytes);

                    for (int j = 0; j < numMaskBytes; j++)
                        bits[j] = (byte)((bits[j] * 0x0202020202 & 0x010884422010) % 1023);

                    tag.Bits = new BitArray(bits);

                    download.tags[i] = tag;
                }
            }

            return download;
        }

        /// <summary>
        /// Downloads all of the currently listed "archive" files from Battle.Net's CDN.  Each archive file is 256mb.
        ///
        /// More details on archive files : https://wowdev.wiki/TACT#Archives
        /// </summary>
        public static void DownloadFullArchives(CDNConfigFile cdnConfig, CDN _cdn)
        {
            Console.WriteLine("Downloading full archive files....");

            int count = 0;
            var timer = Stopwatch.StartNew();

            Parallel.ForEach(cdnConfig.archives, new ParallelOptions { MaxDegreeOfParallelism = 10 }, (entry) =>
            {
                _cdn.Get(RootFolder.data, entry.hashId, writeToDevNull: true);
                count++;
            });
            timer.Stop();
        }

        public static void HandleDownloadFile(DownloadFile download, Dictionary<MD5Hash, IndexEntry> archiveIndexDictionary,
            CDNConfigFile cdnConfigFile, CDN _cdn, TactProduct targetProduct)
        {
            Console.Write("Parsing download file list...".PadRight(Config.PadRight));
            var timer = Stopwatch.StartNew();

            Dictionary<string, IndexEntry> fileIndexList = IndexParser.ParseIndex(cdnConfigFile.fileIndex, _cdn, RootFolder.data);

            //TODO make this more flexible/multi region.  Should probably be passed in/ validated per product.
            //TODO do a check to make sure that the tags being used are actually valid for the product
            List<DownloadTag> tagsToUse = new List<DownloadTag>();
            if (targetProduct.DefaultTags != null)
            {
                foreach (var tag in targetProduct.DefaultTags)
                {
                    tagsToUse.Add(download.tags.FirstOrDefault(e => e.Name.Contains(tag)));
                }
            }
            else
            {
                tagsToUse = download.tags.Where(e => e.Name.Contains("enUS") ||
                                                     e.Name.Contains("Windows") ||
                                                     e.Name.Contains("x86") ||
                                                     e.Name.Contains("noigr")).ToList();
            }

            
          
            int downloads = 0;
            for (var i = 0; i < download.entries.Length; i++)
            {
                DownloadEntry current = download.entries[i];

                // Filtering out files that shouldn't be downloaded by tag.  Ex. only want English audio files for a US install
                //TODO I don't think this filtering is working correctly for all products
                if (!tagsToUse.All(e => e.Bits[i] == true))
                {
                    //TODO this is not filtering correctly for wow_classic
                    continue;
                }
                if (!archiveIndexDictionary.ContainsKey(current.hash))
                {
                    if (fileIndexList.ContainsKey(current.hash.ToString()))
                    {
                        // Handles downloading unarchived files unarchived files
                        var file = fileIndexList[current.hash.ToString()];
                        var startBytes2 = file.offset;
                        var endBytes2 = file.offset + file.size - 1;

                        _cdn.QueueRequest(RootFolder.data, current.hash.ToString(), startBytes2, endBytes2, writeToDevNull: true);
                        downloads++;
                    }
                    continue;
                }

                IndexEntry e = archiveIndexDictionary[current.hash];
                uint chunkSize = 4096;
                var startBytes = e.offset;

                // Need to subtract 1, since the byte range is "inclusive"
                uint numChunks = (e.offset + e.size - 1) / chunkSize;
                uint upperByteRange = (e.offset + e.size - 1) + 4096;
                _cdn.QueueRequest(RootFolder.data, e.IndexId, startBytes, upperByteRange, writeToDevNull: true);
                downloads++;
            }

            Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}".PadLeft(Config.Padding));
        }
    }
}
