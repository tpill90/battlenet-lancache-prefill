using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BuildBackup.Structs;
using BuildBackup.Utils;
using BuildBackup.Web;
using Shared;

namespace BuildBackup.DataAccess
{
    public class EncodingFileHandler
    {
        private readonly CDN _cdn;

        public EncodingFileHandler(CDN cdn)
        {
            _cdn = cdn;
        }

        public EncodingTable BuildEncodingTable(BuildConfigFile buildConfig)
        {
            Console.Write("Loading encoding table...".PadRight(Config.PadRight));
            var timer = Stopwatch.StartNew();

            EncodingFile encodingFile = GetEncoding(buildConfig);

            EncodingTable encodingTable = new EncodingTable();
            encodingTable.encodingFile = encodingFile;

            timer.Stop();
            Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}".PadLeft(Config.Padding));

            return encodingTable;
        }

        private EncodingFile GetEncoding(BuildConfigFile buildConfig, bool parseTableB = false, bool checkStuff = false)
        {
            int encodingSize;
            if (buildConfig.encodingSize == null || buildConfig.encodingSize.Count() < 2)
            {
                encodingSize = 0;
            }
            else
            {
                encodingSize = int.Parse(buildConfig.encodingSize[1]);
            }

            var encoding = new EncodingFile();

            byte[] content = _cdn.GetRequestAsBytes(RootFolder.data, buildConfig.encoding[1]).Result;

            if (encodingSize != 0 && encodingSize != content.Length)
            {
                content = _cdn.GetRequestAsBytes(RootFolder.data, buildConfig.encoding[1]).Result;

                if (encodingSize != content.Length && encodingSize != 0)
                {
                    throw new Exception($"File corrupt/not fully downloaded! Remove data / {buildConfig.encoding[1].ToString()} from cache.");
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

                //encoding.aEntries = new Dictionary<MD5Hash, MD5Hash>(MD5HashEqualityComparer.Instance);
                encoding.aEntriesReversed = new Dictionary<MD5Hash, MD5Hash>(MD5HashEqualityComparer.Instance);
                for (int i = 0; i < encoding.numEntriesA; i++)
                {
                    ushort keysCount;
                    while ((keysCount = bin.ReadUInt16()) != 0)
                    {
                        bin.BaseStream.Position += 4; // Size
                        var hash2 = bin.Read<MD5Hash>();
                        var key = bin.Read<MD5Hash>();
                        var keyCount = keysCount;

                        // @TODO add support for multiple encoding keys
                        bin.BaseStream.Position += (keyCount - 1) * 16;

                        //encoding.aEntries.Add(hash2, key);
                        encoding.aEntriesReversed.Add(key, hash2);
                    }

                    var remaining = 4096 - ((bin.BaseStream.Position - tableAstart) % 4096);
                    if (remaining > 0)
                    {
                        bin.BaseStream.Position += remaining;
                    }
                }

                if (!parseTableB)
                {
                    return encoding;
                }

                ParseTableB(checkStuff, encoding, bin);

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

        private static void ParseTableB(bool checkStuff, EncodingFile encoding, BinaryReader bin)
        {
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
        }
    }
}