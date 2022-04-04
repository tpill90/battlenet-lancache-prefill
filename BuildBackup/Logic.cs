using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using BuildBackup.Structs;
using Newtonsoft.Json;
using Shared;
using Colors = Shared.Colors;

namespace BuildBackup
{
    public class Logic
    {
        private CDN cdn;
        private readonly Uri _baseUrl;

        public Logic(CDN cdn, Uri baseUrl)
        {
            this.cdn = cdn;
            _baseUrl = baseUrl;
        }

        public CDNConfigFile GetCDNconfig(string url, VersionsEntry targetVersion)
        {
            var timer = Stopwatch.StartNew();

            var cdnConfig = new CDNConfigFile();

            var content = Encoding.UTF8.GetString(cdn.Get($"{url}/config/", targetVersion.cdnConfig));
            var cdnConfigLines = content.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < cdnConfigLines.Count(); i++)
            {
                if (cdnConfigLines[i].StartsWith("#") || cdnConfigLines[i].Length == 0) { continue; }
                var cols = cdnConfigLines[i].Split(new string[] { " = " }, StringSplitOptions.RemoveEmptyEntries);
                switch (cols[0])
                {
                    case "archives":
                        var archives = cols[1].Split(' ');
                        cdnConfig.archives = archives;
                        break;
                    case "archive-group":
                        cdnConfig.archiveGroup = cols[1];
                        break;
                    case "patch-archives":
                        if (cols.Length > 1)
                        {
                            var patchArchives = cols[1].Split(' ');
                            cdnConfig.patchArchives = patchArchives;
                        }
                        break;
                    case "patch-archive-group":
                        cdnConfig.patchArchiveGroup = cols[1];
                        break;
                    case "builds":
                        var builds = cols[1].Split(' ');
                        cdnConfig.builds = builds;
                        break;
                    case "file-index":
                        cdnConfig.fileIndex = cols[1];
                        break;
                    case "file-index-size":
                        cdnConfig.fileIndexSize = cols[1];
                        break;
                    case "patch-file-index":
                        cdnConfig.patchFileIndex = cols[1];
                        break;
                    case "patch-file-index-size":
                        cdnConfig.patchFileIndexSize = cols[1];
                        break;
                    default:
                        //Console.WriteLine("!!!!!!!! Unknown cdnconfig variable '" + cols[0] + "'");
                        break;
                }
            }

            if (cdnConfig.archives == null)
            {
                throw new Exception("Invalid CDNconfig");
            }

            Console.WriteLine($"CDNConfig loaded, {Colors.Magenta(cdnConfig.archives.Count())} archives.  {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");
            return cdnConfig;
        }

        //TODO comment
        //TODO this takes about 200ms.  Can it be sped up?
        public VersionsEntry GetVersionEntry(TactProduct tactProduct)
        {
            var timer = Stopwatch.StartNew();
            VersionsFile versions = GetVersions(tactProduct);

            VersionsEntry targetVersion = versions.entries[0];

            //TODO toggle this output with a flag
            //Console.WriteLine($"Found {Colors.Magenta(versions.entries.Count())} total versions.  Using version with info :");
            //targetVersion.PrintTable();

            Console.WriteLine($"GetVersionEntry took {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");
            return targetVersion;
        }

        public VersionsFile GetVersions(TactProduct tactProduct)
        {
            var cacheFile = $"{Configuration.CacheDir}/versions-{tactProduct.ProductCode}.json";
            
            // Load cached version.  
            if (File.Exists(cacheFile) && File.GetLastWriteTime(cacheFile) < DateTime.Now.AddHours(1))
            {
                return JsonConvert.DeserializeObject<VersionsFile>(File.ReadAllText(cacheFile));
            }

            string content;
            var versions = new VersionsFile();

            var url = $"{_baseUrl}{tactProduct.ProductCode}/versions";
            using (HttpResponseMessage response = cdn.client.GetAsync(new Uri(url)).Result)
            {
                if (response.IsSuccessStatusCode)
                {
                    using HttpContent res = response.Content;
                    content = res.ReadAsStringAsync().Result;
                }
                else
                {
                    Console.WriteLine("Error during retrieving HTTP versions: Received bad HTTP code " + response.StatusCode);
                    return versions;
                }
            }

            content = content.Replace("\0", "");
            var lines = content.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            var lineList = new List<string>();

            for (var i = 0; i < lines.Count(); i++)
            {
                if (lines[i][0] != '#')
                {
                    lineList.Add(lines[i]);
                }
            }

            lines = lineList.ToArray();

            if (lines.Count() > 0)
            {
                versions.entries = new VersionsEntry[lines.Count() - 1];

                var cols = lines[0].Split('|');

                for (var c = 0; c < cols.Count(); c++)
                {
                    var friendlyName = cols[c].Split('!').ElementAt(0);

                    for (var i = 1; i < lines.Count(); i++)
                    {
                        var row = lines[i].Split('|');

                        switch (friendlyName)
                        {
                            case "Region":
                                versions.entries[i - 1].region = row[c];
                                break;
                            case "BuildConfig":
                                versions.entries[i - 1].buildConfig = row[c];
                                break;
                            case "CDNConfig":
                                versions.entries[i - 1].cdnConfig = row[c];
                                break;
                            case "Keyring":
                            case "KeyRing":
                                versions.entries[i - 1].keyRing = row[c];
                                break;
                            case "BuildId":
                                versions.entries[i - 1].buildId = row[c];
                                break;
                            case "VersionName":
                            case "VersionsName":
                                versions.entries[i - 1].versionsName = row[c].Trim('\r');
                                break;
                            case "ProductConfig":
                                versions.entries[i - 1].productConfig = row[c];
                                break;
                            default:
                                Console.WriteLine("!!!!!!!! Unknown versions variable '" + friendlyName + "'");
                                break;
                        }
                    }
                }
            }

            // Writes results to disk, to be used as cache later
            File.WriteAllText(cacheFile, JsonConvert.SerializeObject(versions));

            return versions;
        }

        //TODO should this be part of the CDN class?
        public CdnsFile GetCDNs(TactProduct tactProduct)
        {
            var cacheFile = $"{Configuration.CacheDir}/cdns-{tactProduct.ProductCode}.json";
            
            // Load cached version, only valid for 2 hours
            if (File.Exists(cacheFile) && File.GetLastWriteTime(cacheFile) < DateTime.Now.AddHours(2))
            {
                return JsonConvert.DeserializeObject<CdnsFile>(File.ReadAllText(cacheFile));
            }

            string content;

            CdnsFile cdns = new CdnsFile();

            using (HttpResponseMessage response = cdn.client.GetAsync(new Uri($"{_baseUrl}{tactProduct.ProductCode}/cdns")).Result)
            {
                if (response.IsSuccessStatusCode)
                {
                    using HttpContent res = response.Content;
                    content = res.ReadAsStringAsync().Result;
                }
                else
                {
                    Console.WriteLine("Error during retrieving HTTP cdns: Received bad HTTP code " + response.StatusCode);
                    return cdns;
                }
            }
        
            var lines = content.Split(new string[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

            var lineList = new List<string>();

            for (var i = 0; i < lines.Count(); i++)
            {
                if (lines[i][0] != '#')
                {
                    lineList.Add(lines[i]);
                }
            }

            lines = lineList.ToArray();

            if (lines.Count() > 0)
            {
                cdns.entries = new CdnsEntry[lines.Count() - 1];

                var cols = lines[0].Split('|');

                for (var c = 0; c < cols.Count(); c++)
                {
                    var friendlyName = cols[c].Split('!').ElementAt(0);

                    for (var i = 1; i < lines.Count(); i++)
                    {
                        var row = lines[i].Split('|');

                        switch (friendlyName)
                        {
                            case "Name":
                                cdns.entries[i - 1].name = row[c];
                                break;
                            case "Path":
                                cdns.entries[i - 1].path = row[c];
                                break;
                            case "Hosts":
                                var hosts = row[c].Split(' ');
                                cdns.entries[i - 1].hosts = new string[hosts.Count()];
                                for (var h = 0; h < hosts.Count(); h++)
                                {
                                    cdns.entries[i - 1].hosts[h] = hosts[h];
                                }
                                break;
                            case "ConfigPath":
                                cdns.entries[i - 1].configPath = row[c];
                                break;
                            default:
                                //Console.WriteLine("!!!!!!!! Unknown cdns variable '" + friendlyName + "'");
                                break;
                        }
                    }
                }

                foreach (var subcdn in cdns.entries)
                {
                    foreach (var cdnHost in subcdn.hosts)
                    {
                        if (!cdn.cdnList.Contains(cdnHost))
                        {
                            cdn.cdnList.Add(cdnHost);
                        }
                    }
                }
            }

            if (cdns.entries == null || !cdns.entries.Any())
            {
                Console.WriteLine($"Invalid CDNs file for {tactProduct.DisplayName}, skipping!");
                throw new Exception($"Invalid CDNs file for {tactProduct.DisplayName}, skipping!");
            }
            Console.WriteLine($"Loaded {Colors.Cyan(cdns.entries.Count())} CDNs");

            // Writes results to disk, to be used as cache later
            File.WriteAllText(cacheFile, JsonConvert.SerializeObject(cdns));

            return cdns;
        }

        public GameBlobFile GetGameBlob(string program)
        {
            string content;

            var gblob = new GameBlobFile();

            try
            {
                using (HttpResponseMessage response = cdn.client.GetAsync(new Uri(_baseUrl + program + "/" + "blob/game")).Result)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        using (HttpContent res = response.Content)
                        {
                            content = res.ReadAsStringAsync().Result;
                        }
                    }
                    else
                    {
                        throw new Exception("Bad HTTP code while retrieving");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error retrieving game blob file: " + e.Message);
                return gblob;
            }

            if (string.IsNullOrEmpty(content))
            {
                Console.WriteLine("Empty gameblob :(");
                return gblob;
            }

            dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(content);
            if (json.all.config.decryption_key_name != null)
            {
                gblob.decryptionKeyName = json.all.config.decryption_key_name.Value;
            }
            return gblob;
        }

        public GameBlobFile GetProductConfig(string url, string hash)
        {
            string content = Encoding.UTF8.GetString(cdn.Get(url, hash));
         
            if (string.IsNullOrEmpty(content))
            {
                Console.WriteLine("Error reading product config!");
                return new GameBlobFile();
            }

            var gblob = new GameBlobFile();
            dynamic json = JsonConvert.DeserializeObject(content);
            if (json.all.config.decryption_key_name != null)
            {
                gblob.decryptionKeyName = json.all.config.decryption_key_name.Value;
            }
            return gblob;
        }

        //TODO refactor this cdns file out
        public string GetDecryptionKeyName(CdnsFile cdns, TactProduct tactProduct, VersionsEntry targetVersion)
        {
            string decryptionKeyName = null;
            var gameblob = GetGameBlob(tactProduct.ProductCode);
            if (!string.IsNullOrEmpty(gameblob.decryptionKeyName))
            {
                decryptionKeyName = gameblob.decryptionKeyName;
            }

            if (!string.IsNullOrEmpty(targetVersion.productConfig))
            {
                var productConfig = GetProductConfig(cdns.entries[0].configPath + "/", targetVersion.productConfig);
                if (!string.IsNullOrEmpty(productConfig.decryptionKeyName))
                {
                    decryptionKeyName = productConfig.decryptionKeyName;
                }
            }

            return decryptionKeyName;
        }

        public RootFile GetRoot(string url, string hash, bool parseIt = false)
        {
            var root = new RootFile
            {
                entriesLookup = new MultiDictionary<ulong, RootEntry>(),
                entriesFDID = new MultiDictionary<uint, RootEntry>()
            };

            byte[] content = cdn.Get($"{url}/data/", hash);
            if (!parseIt) return root;

            var hasher = new Jenkins96();

            var namedCount = 0;
            var unnamedCount = 0;
            uint totalFiles = 0;
            uint namedFiles = 0;
            var newRoot = false;
            using (BinaryReader bin = new BinaryReader(new MemoryStream(BLTE.Parse(content))))
            {
                var header = bin.ReadUInt32();

                if (header == 1296454484)
                {
                    totalFiles = bin.ReadUInt32();
                    namedFiles = bin.ReadUInt32();
                    newRoot = true;
                }
                else
                {
                    bin.BaseStream.Position = 0;
                }

                var blockCount = 0;

                while (bin.BaseStream.Position < bin.BaseStream.Length)
                {
                    var count = bin.ReadUInt32();
                    var contentFlags = (ContentFlags)bin.ReadUInt32();
                    var localeFlags = (LocaleFlags)bin.ReadUInt32();

                    //Console.WriteLine("[Block " + blockCount + "] " + count + " entries. Content flags: " + contentFlags.ToString() + ", Locale flags: " + localeFlags.ToString());
                    var entries = new RootEntry[count];
                    var filedataIds = new int[count];

                    var fileDataIndex = 0;
                    for (var i = 0; i < count; ++i)
                    {
                        entries[i].localeFlags = localeFlags;
                        entries[i].contentFlags = contentFlags;

                        filedataIds[i] = fileDataIndex + bin.ReadInt32();
                        entries[i].fileDataID = (uint)filedataIds[i];
                        fileDataIndex = filedataIds[i] + 1;
                    }

                    var blockFdids = new List<string>();
                    if (!newRoot)
                    {
                        for (var i = 0; i < count; ++i)
                        {
                            entries[i].md5 = bin.ReadBytes(16);
                            entries[i].lookup = bin.ReadUInt64();
                            root.entriesLookup.Add(entries[i].lookup, entries[i]);
                            root.entriesFDID.Add(entries[i].fileDataID, entries[i]);
                            blockFdids.Add(entries[i].fileDataID.ToString());
                        }
                    }
                    else
                    {
                        for (var i = 0; i < count; ++i)
                        {
                            entries[i].md5 = bin.ReadBytes(16);
                        }

                        for (var i = 0; i < count; ++i)
                        {
                            if (contentFlags.HasFlag(ContentFlags.NoNames))
                            {
                                entries[i].lookup = 0;
                                unnamedCount++;
                            }
                            else
                            {
                                entries[i].lookup = bin.ReadUInt64();
                                root.entriesLookup.Add(entries[i].lookup, entries[i]);
                                namedCount++;
                            }

                            root.entriesFDID.Add(entries[i].fileDataID, entries[i]);
                            blockFdids.Add(entries[i].fileDataID.ToString());
                        }
                    }

                    //File.WriteAllLinesAsync("blocks/Block" + blockCount + ".txt", blockFdids);
                    blockCount++;
                }
            }

            if ((namedFiles > 0) && namedFiles != namedCount)
                throw new Exception("Didn't read correct amount of named files! Read " + namedCount + " but expected " + namedFiles);

            if ((totalFiles > 0) && totalFiles != (namedCount + unnamedCount))
                throw new Exception("Didn't read correct amount of total files! Read " + (namedCount + unnamedCount) + " but expected " + totalFiles);

            return root;
        }

        public DownloadFile GetDownload(string url, string hash)
        {
            var download = new DownloadFile();

            byte[] content = cdn.Get($"{url}/data/", hash);

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

                bin.BaseStream.Seek(16, SeekOrigin.Begin);

                download.entries = new DownloadEntry[download.numEntries];
                for (int i = 0; i < download.numEntries; i++)
                {
                    download.entries[i].hash = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");
                    bin.ReadBytes(10);
                }
            }

            return download;
        }

        public InstallFile GetInstall(string url, string hash)
        {
            var install = new InstallFile();

            byte[] content = cdn.Get($"{url}/data/", hash);

            using (BinaryReader bin = new BinaryReader(new MemoryStream(BLTE.Parse(content))))
            {
                if (Encoding.UTF8.GetString(bin.ReadBytes(2)) != "IN") { throw new Exception("Error while parsing install file. Did BLTE header size change?"); }

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
                    install.entries[i].contentHash = bin.ReadBytes(install.hashSize);
                    install.entries[i].contentHashString = BitConverter.ToString(install.entries[i].contentHash).Replace("-", "");
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
                if (entry.hash == buildConfig.root.ToUpper())
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

            byte[] content = cdn.Get($"{url}/data/", hash);

            if (encodingSize != 0 && encodingSize != content.Length)
            {
                content = cdn.Get($"{url}/data/", hash);

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
