using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BuildBackup.DataAccess;
using BuildBackup.Structs;
using Colors = Shared.Colors;

namespace BuildBackup.Handlers
{
    public class InstallFileHandler
    {
        private CDN _cdn;

        public InstallFileHandler(CDN cdn)
        {
            _cdn = cdn;
        }

        //TODO comment
        public void HandleInstallFile(BuildConfigFile buildConfig, ArchiveIndexHandler archiveIndexHandler, CDNConfigFile cdnConfigFile,
            TactProduct product)
        {
            var timer = Stopwatch.StartNew();

            var installKey = buildConfig.install[1].ToString();
            InstallFile installFile = ParseInstallFile(installKey);

            List<InstallFileEntry> filtered;
            //TODO make this more flexible/multi region.  Should probably be passed in/ validated per product.
            //TODO do a check to make sure that the tags being used are actually valid for the product
            if (product == TactProducts.CodVanguard)
            {
                filtered = installFile.entries.Where(e => e.tags.Contains("2=enUS")).ToList();
            }
            else
            {
                filtered = installFile.entries
                    .Where(e => e.tags.Contains("1=enUS") && e.tags.Contains("2=Windows"))
                    .ToList();
            }

            if (!filtered.Any())
            {
                if (timer.Elapsed.TotalMilliseconds > 10)
                {
                    Console.Write("Parsed install file...".PadRight(Config.PadRight));
                    Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}".PadLeft(Config.Padding));
                }
                return;
            }

            var encodingFileHandler = new EncodingFileHandler(_cdn);
            EncodingTable encodingTable = encodingFileHandler.BuildEncodingTable(buildConfig);

            foreach (var file in filtered)
            {
                //The manifest contains pairs of IndexId-ContentHash, reverse lookup for matches based on the ContentHash
                if (!encodingTable.ReversedEncodingDictionary.ContainsKey(file.contentHash))
                {
                    continue;
                }

                // If we found a match for the archive content, look into the archive index to see where the file can be downloaded from
                MD5Hash upperHash = encodingTable.ReversedEncodingDictionary[file.contentHash];

                IndexEntry? archiveIndex = archiveIndexHandler.TryGet(upperHash);
                if (archiveIndex == null)
                {
                    continue;
                }

                IndexEntry e = archiveIndex.Value;

                // Need to subtract 1, since the byte range is "inclusive"
                var upperByteRange = ((int)e.offset + (int)e.size - 1);
                string archiveIndexKey = cdnConfigFile.archives[e.index].hashId;

                _cdn.QueueRequest(RootFolder.data, archiveIndexKey, (int)e.offset, upperByteRange);
            }
            Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}".PadLeft(Config.Padding));
        }

        private InstallFile ParseInstallFile(string hash)
        {
            var install = new InstallFile();

            byte[] content = _cdn.GetRequestAsBytes(RootFolder.data, hash).Result;

            using (BinaryReader bin = new BinaryReader(new MemoryStream(BLTE.Parse(content))))
            {
                if (Encoding.UTF8.GetString(bin.ReadBytes(2)) != "IN")
                {
                    throw new Exception("Error while parsing install file. Did BLTE header size change?");
                }

                bin.ReadByte();

                install.hashSize = bin.ReadByte();
                if (install.hashSize != 16) throw new Exception("Unsupported install hash size!");

                install.numTags = bin.ReadUInt16(true);
                install.numEntries = bin.ReadUInt32(true);

                int bytesPerTag = ((int)install.numEntries + 7) / 8;

                install.tags = new InstallTagEntry[install.numTags];

                for (var i = 0; i < install.numTags; i++)
                {
                    install.tags[i].name = bin.ReadCString();
                    install.tags[i].type = bin.ReadUInt16(true);

                    var filebits = bin.ReadBytes(bytesPerTag);

                    for (int j = 0; j < bytesPerTag; j++)
                        filebits[j] = (byte)((filebits[j] * 0x0202020202 & 0x010884422010) % 1023);

                    install.tags[i].files = new BitArray(filebits);
                }

                install.entries = new InstallFileEntry[install.numEntries];

                for (var i = 0; i < install.numEntries; i++)
                {
                    install.entries[i].name = bin.ReadCString();
                    install.entries[i].contentHash = bin.Read<MD5Hash>();
                    install.entries[i].size = bin.ReadUInt32(true);
                    install.entries[i].tags = new List<string>();
                    for (var j = 0; j < install.numTags; j++)
                    {
                        if (install.tags[j].files[i] == true)
                        {
                            install.entries[i].tags.Add(install.tags[j].type + "=" + install.tags[j].name);
                        }
                    }
                }
            }

            return install;
        }

    }
}