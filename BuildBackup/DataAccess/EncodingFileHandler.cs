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

        public EncodingTable BuildEncodingTable(BuildConfigFile buildConfig)
        {
            Console.Write("Loading encoding table...");
            var timer = Stopwatch.StartNew();

            EncodingFile encodingFile = GetEncoding(buildConfig);

            EncodingTable encodingTable = new EncodingTable();
            if (buildConfig.install.Length == 2)
            {
                encodingTable.installKey = buildConfig.install[1].ToString();
            }

            if (buildConfig.download.Length == 2)
            {
                encodingTable.downloadKey = buildConfig.download[1].ToString();
            }

            foreach (var entry in encodingFile.aEntries)
            {
                if (entry.hash == buildConfig.root)
                {
                    encodingTable.rootKey = entry.key.ToString().ToLower();
                }

                if (encodingTable.downloadKey == "" && entry.hash == buildConfig.download[0])
                {
                    encodingTable.downloadKey = entry.key.ToString().ToLower();
                }

                if (encodingTable.installKey == "" && entry.hash == buildConfig.install[0])
                {
                    encodingTable.installKey = entry.key.ToString().ToLower();
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

        private EncodingFile GetEncoding(BuildConfigFile buildConfig, bool parseTableB = false, bool checkStuff = false)
        {
            string url = _cdns.entries[0].path;
            var hash = buildConfig.encoding[1];
            int encodingSize = 0;
            if (buildConfig.encodingSize == null || buildConfig.encodingSize.Count() < 2)
            {
                encodingSize = 0;
            }
            else
            {
                encodingSize = int.Parse(buildConfig.encodingSize[1]);
            }

            var encoding = new EncodingFile();

            byte[] content = _cdn.Get($"{url}/data/", hash);

            if (encodingSize != 0 && encodingSize != content.Length)
            {
                content = _cdn.Get($"{url}/data/", hash);

                if (encodingSize != content.Length && encodingSize != 0)
                {
                    throw new Exception($"File corrupt/not fully downloaded! Remove data / {hash[0]}{hash[1]} / {hash[2]}{hash[3]} / {hash} from cache.");
                }
            }

            using (BinaryReader bin = new BinaryReader(new MemoryStream(BLTE.Parse(content))))
            {
                if (Encoding.UTF8.GetString(bin.ReadBytes(2)) != "EN")
                {
                    throw new Exception("Error while parsing encoding file. Did BLTE header size change?");
                }
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
                        encoding.aHeaders[i].firstHash = bin.Read<MD5Hash>();
                        encoding.aHeaders[i].checksum = bin.Read<MD5Hash>();
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
                        EncodingFileEntry entry = new EncodingFileEntry
                        {
                            keyCount = keysCount,
                            size = bin.ReadUInt32(true),
                            hash = bin.Read<MD5Hash>(),
                            key = bin.Read<MD5Hash>()
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
                        encoding.bHeaders[i].firstHash = bin.Read<MD5Hash>();
                        encoding.bHeaders[i].checksum = bin.Read<MD5Hash>();
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
