using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using BuildBackup.Structs;
using Newtonsoft.Json;
using Colors = Shared.Colors;

namespace BuildBackup
{
    public class Logic
    {
        private CDN cdn;
        private readonly Uri _battleNetPatchUri;

        public Logic(CDN cdn, Uri battleNetPatchUri)
        {
            this.cdn = cdn;
            _battleNetPatchUri = battleNetPatchUri;
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
                        cdnConfig.archives = archives.Select(e => new Archive()
                        {
                            hashId = e
                        }).ToArray();
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


            Console.WriteLine($"GetVersionEntry took {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");
            return targetVersion;
        }

        public VersionsFile GetVersions(TactProduct tactProduct)
        {
            var cacheFile = $"{Config.CacheDir}/versions-{tactProduct.ProductCode}.json";
            
            // Load cached version.  
            if (File.Exists(cacheFile) && DateTime.Now < File.GetLastWriteTime(cacheFile).AddHours(1))
            {
                return JsonConvert.DeserializeObject<VersionsFile>(File.ReadAllText(cacheFile));
            }

            string content;
            var versions = new VersionsFile();

            var url = $"{_battleNetPatchUri}{tactProduct.ProductCode}/versions";
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

        

        public GameBlobFile GetGameBlob(string program)
        {
            string content;

            var gblob = new GameBlobFile();

            try
            {
                using (HttpResponseMessage response = cdn.client.GetAsync(new Uri(_battleNetPatchUri + program + "/" + "blob/game")).Result)
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

        public void GetBuildConfigAndEncryption(TactProduct product, CDNConfigFile cdnConfig, VersionsEntry targetVersion, CDN cdn, CdnsFile cdns)
        {
            var timer = Stopwatch.StartNew();
            // Not required by these products
            if (product == TactProducts.Starcraft1)
            {
                return;
            }

            Console.Write("Loading encryption...".PadRight(Config.PadRight));

            if (cdnConfig.builds != null)
            {
                BuildConfigFile[] cdnBuildConfigs = new BuildConfigFile[cdnConfig.builds.Count()];
            }

            if (!string.IsNullOrEmpty(targetVersion.keyRing))
            {
                // Starcraft 2 calls this
                cdn.Get($"{cdns.entries[0].path}/config/", targetVersion.keyRing);
            }

            //Let us ignore this whole encryption thing if archives are set, surely this will never break anything and it'll back it up perfectly fine.
            //var decryptionKeyName = GetDecryptionKeyName(cdns, product, targetVersion);
            //if (!string.IsNullOrEmpty(decryptionKeyName) && cdnConfig.archives == null)
            //{
            //    if (!File.Exists(decryptionKeyName + ".ak"))
            //    {
            //        Console.WriteLine("Decryption key is set and not available on disk, skipping.");
            //        cdn.isEncrypted = false;
            //        return true;
            //    }
            //    else
            //    {
            //        cdn.isEncrypted = true;
            //    }
            //}
            //else
            //{
            //    cdn.isEncrypted = false;
            //}

            Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}".PadLeft(Config.Padding));
        }
    }
}
