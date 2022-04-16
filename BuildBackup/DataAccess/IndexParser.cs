using System;
using System.Collections.Generic;
using System.IO;
using BuildBackup.Structs;

namespace BuildBackup.DataAccess
{
    public static class IndexParser
    {
        public static Dictionary<string, IndexEntry> ParseIndex(string hashId, CDN cdn, RootFolder folder)
        {
            byte[] indexContent = cdn.GetRequestAsBytes(folder, hashId, isIndex: true).Result;

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
                    footer.numElements = bin.ReadUInt32InvertEndian();
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
                            entry.size = bin.ReadUInt32InvertEndian();
                        }
                        else
                        {
                            throw new NotImplementedException("Index size reading other than 4 is not implemented!");
                        }

                        if (footer.offsetBytes == 4)
                        {
                            // Archive index
                            entry.offset = bin.ReadUInt32InvertEndian();
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
    }
}
