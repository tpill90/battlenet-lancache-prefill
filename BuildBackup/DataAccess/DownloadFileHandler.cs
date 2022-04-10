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

                int numMaskBytes = (int)((download.numEntries + 7) / 8);

                //TODO implement
                //uint32 entryExtra = 6;
                //if (has_checksum_in_entry)
                //{
                //    entryExtra += 4;
                //}
                //if (version >= 2)
                //{
                //    uint8 number_of_flag_bytes = file.read8();
                //    entryExtra += number_of_flag_bytes;
                //    if (version >= 3)
                //    {
                //        file.seek(4, SEEK_CUR);
                //    }
                //}

                // Reading the download entries
                bin.BaseStream.Seek(16, SeekOrigin.Begin);
                download.entries = new DownloadEntry[download.numEntries];
                for (int i = 0; i < download.numEntries; i++)
                {
                    download.entries[i].hash = bin.Read<MD5Hash>();
                    bin.ReadBytes(10);
                }

                // Reading the tags
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

        public static void HandleDownloadFile(DownloadFile download, Dictionary<MD5Hash, IndexEntry> archiveIndexDictionary, CDNConfigFile cdnConfigFile, CDN _cdn)
        {
            Console.Write("Parsing download file list...".PadRight(Config.PadRight));
            var timer = Stopwatch.StartNew();

            Dictionary<string, IndexEntry> fileIndexList = IndexParser.ParseIndex(cdnConfigFile.fileIndex, _cdn, RootFolder.data);

            //TODO make this more flexible/multi region.  Should probably be passed in/ validated per product.
            //TODO do a check to make sure that the tags being used are actually valid for the product
            var enUsTag = download.tags.Single(e => e.Name.Contains("enUS"));
            var tagToUse2 = download.tags.FirstOrDefault(e => e.Name.Contains("Windows"));
            var x86Tag = download.tags.FirstOrDefault(e => e.Name.Contains("x86"));
            var noIgrTag = download.tags.FirstOrDefault(e => e.Name.Contains("noigr"));


            for (var i = 0; i < download.entries.Length; i++)
            {
                DownloadEntry current = download.entries[i];

                // Filtering out files that shouldn't be downloaded by tag.  Ex. only want English audio files for a US install
                //TODO I don't think this filtering is working correctly for all products
                if (enUsTag.Bits[i] == false || tagToUse2?.Bits[i] == false || x86Tag?.Bits[i] == false || noIgrTag?.Bits[i] == false)
                {
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
            }

            Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}".PadLeft(Config.Padding));
        }
    }
}
