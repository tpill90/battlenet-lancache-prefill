using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BuildBackup.Structs;
using Shared;

namespace BuildBackup.DataAccess
{
    public class EncodingFileHandler
    {
        private readonly CdnsFile _cdns;
        private readonly CDN _cdn;

        public EncodingFileHandler(CdnsFile cdns, CDN cdn)
        {
            _cdns = cdns;
            _cdn = cdn;
        }

        public EncodingTable BuildEncodingTable(BuildConfigFile buildConfig, CdnsFile cdns)
        {
            Console.Write("Loading encoding table...");
            var timer = Stopwatch.StartNew();

            EncodingFile encodingFile = GetEncoding(buildConfig, cdns);
            EncodingTable encodingTable = new EncodingTable();

            if (buildConfig.install.Length == 2)
            {
                encodingTable.installKey = buildConfig.install[1];
            }

            if (buildConfig.download.Length == 2)
            {
                encodingTable.downloadKey = buildConfig.download[1];
            }

            foreach (var entry in encodingFile.aEntries)
            {
                if (entry.hash == buildConfig.rootUpper)
                {
                    encodingTable.rootKey = entry.key.ToLower();
                }

                if (encodingTable.downloadKey == "" && entry.hash == buildConfig.download[0].ToUpper())
                {
                    encodingTable.downloadKey = entry.key.ToLower();
                }

                if (encodingTable.installKey == "" && entry.hash == buildConfig.install[0].ToUpper())
                {
                    encodingTable.installKey = entry.key.ToLower();
                }

                if (!encodingTable.EncodingDictionary.ContainsKey(entry.key))
                {
                    encodingTable.EncodingDictionary.Add(entry.key, entry.hash);
                }
            }

            timer.Stop();
            Console.WriteLine($" Done! {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");

            return encodingTable;
        }

        public EncodingFile GetEncoding(BuildConfigFile buildConfig, CdnsFile cdns)
        {
            if (buildConfig.encodingSize == null || buildConfig.encodingSize.Count() < 2)
            {
                return GetEncoding(cdns.entries[0].path, buildConfig.encoding[1], 0);
            }
            else
            {
                return GetEncoding(cdns.entries[0].path, buildConfig.encoding[1], int.Parse(buildConfig.encodingSize[1]));
            }
        }

        private EncodingFile GetEncoding(string url, string hash, int encodingSize = 0, bool parseTableB = false, bool checkStuff = false)
        {
            var encoding = new EncodingFile();

            byte[] content = _cdn.Get($"{url}/data/", hash);

            if (encodingSize != 0 && encodingSize != content.Length)
            {
                content = _cdn.Get($"{url}/data/", hash);

                if (encodingSize != content.Length && encodingSize != 0)
                {
                    throw new Exception("File corrupt/not fully downloaded! Remove " + "data / " + hash[0] + hash[1] + " / " + hash[2] + hash[3] + " / " + hash + " from cache.");
                }
            }

            using (BinaryReader bin = new BinaryReader(new MemoryStream(BLTE.Parse(content))))
            {
                if (Encoding.UTF8.GetString(bin.ReadBytes(2)) != "EN") { throw new Exception("Error while parsing encoding file. Did BLTE header size change?"); }
                encoding.unk1 = bin.ReadByte();
                encoding.checksumSizeA = bin.ReadByte();
                encoding.checksumSizeB = bin.ReadByte();
                encoding.sizeA = bin.ReadUInt16(true);
                encoding.sizeB = bin.ReadUInt16(true);
                encoding.numEntriesA = bin.ReadUInt32(true);
                encoding.numEntriesB = bin.ReadUInt32(true);
                bin.ReadByte(); // unk
                encoding.stringBlockSize = bin.ReadUInt32(true);

                var headerLength = bin.BaseStream.Position;
                var stringBlockEntries = new List<string>();

                if (parseTableB)
                {
                    while ((bin.BaseStream.Position - headerLength) != (long)encoding.stringBlockSize)
                    {
                        stringBlockEntries.Add(bin.ReadCString());
                    }

                    encoding.stringBlockEntries = stringBlockEntries.ToArray();
                }
                else
                {
                    bin.BaseStream.Position += (long)encoding.stringBlockSize;
                }

                /* Table A */
                if (checkStuff)
                {
                    encoding.aHeaders = new EncodingHeaderEntry[encoding.numEntriesA];

                    for (int i = 0; i < encoding.numEntriesA; i++)
                    {
                        encoding.aHeaders[i].firstHash = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");
                        encoding.aHeaders[i].checksum = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");
                    }
                }
                else
                {
                    bin.BaseStream.Position += encoding.numEntriesA * 32;
                }

                var tableAstart = bin.BaseStream.Position;

                List<EncodingFileEntry> entries = new List<EncodingFileEntry>();

                for (int i = 0; i < encoding.numEntriesA; i++)
                {
                    ushort keysCount;
                    while ((keysCount = bin.ReadUInt16()) != 0)
                    {
                        EncodingFileEntry entry = new EncodingFileEntry()
                        {
                            keyCount = keysCount,
                            size = bin.ReadUInt32(true),
                            hash = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", ""),
                            key = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "")
                        };

                        // @TODO add support for multiple encoding keys
                        for (int key = 0; key < entry.keyCount - 1; key++)
                        {
                            bin.ReadBytes(16);
                        }

                        entries.Add(entry);
                    }

                    var remaining = 4096 - ((bin.BaseStream.Position - tableAstart) % 4096);
                    if (remaining > 0) { bin.BaseStream.Position += remaining; }
                }

                encoding.aEntries = entries.ToArray();

                if (!parseTableB)
                {
                    return encoding;
                }

                /* Table B */
                if (checkStuff)
                {
                    encoding.bHeaders = new EncodingHeaderEntry[encoding.numEntriesB];

                    for (int i = 0; i < encoding.numEntriesB; i++)
                    {
                        encoding.bHeaders[i].firstHash = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");
                        encoding.bHeaders[i].checksum = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");
                    }
                }
                else
                {
                    bin.BaseStream.Position += encoding.numEntriesB * 32;
                }

                var tableBstart = bin.BaseStream.Position;

                encoding.bEntries = new Dictionary<string, EncodingFileDescEntry>();

                while (bin.BaseStream.Position < tableBstart + 4096 * encoding.numEntriesB)
                {
                    var remaining = 4096 - (bin.BaseStream.Position - tableBstart) % 4096;

                    if (remaining < 25)
                    {
                        bin.BaseStream.Position += remaining;
                        continue;
                    }

                    var key = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");
                    EncodingFileDescEntry entry = new EncodingFileDescEntry()
                    {
                        stringIndex = bin.ReadUInt32(true),
                        compressedSize = bin.ReadUInt40(true)
                    };

                    if (entry.stringIndex == uint.MaxValue) break;

                    encoding.bEntries.Add(key, entry);
                }

                // Go to the end until we hit a non-NUL byte
                while (bin.BaseStream.Position < bin.BaseStream.Length)
                {
                    if (bin.ReadByte() != 0)
                        break;
                }

                bin.BaseStream.Position -= 1;
                var eespecSize = bin.BaseStream.Length - bin.BaseStream.Position;
                encoding.encodingESpec = new string(bin.ReadChars(int.Parse(eespecSize.ToString())));
            }

            return encoding;
        }
    }
}
