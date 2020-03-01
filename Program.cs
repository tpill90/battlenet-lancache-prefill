using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BuildBackup
{
    class Program
    {
        private static readonly Uri baseUrl = new Uri("http://eu.patch.battle.net:1119/");

        private static string[] checkPrograms;
        private static string[] backupPrograms;

        private static VersionsFile versions;
        private static CdnsFile cdns;
        private static GameBlobFile gameblob;
        private static GameBlobFile productConfig;
        private static BuildConfigFile buildConfig;
        private static BuildConfigFile[] cdnBuildConfigs;
        private static CDNConfigFile cdnConfig;
        private static EncodingFile encoding;
        private static InstallFile install;
        private static DownloadFile download;
        private static RootFile root;
        private static PatchFile patch;

        private static bool overrideVersions;
        private static string overrideBuildconfig;
        private static string overrideCDNconfig;

        private static Dictionary<string, IndexEntry> indexDictionary = new Dictionary<string, IndexEntry>();
        private static Dictionary<string, IndexEntry> patchIndexDictionary = new Dictionary<string, IndexEntry>();
        private static Dictionary<string, IndexEntry> fileIndexList = new Dictionary<string, IndexEntry>();
        private static Dictionary<string, IndexEntry> patchFileIndexList = new Dictionary<string, IndexEntry>();
        private static ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();

        private static CDN cdn = new CDN();

        static void Main(string[] args)
        {
            cdn.cacheDir = SettingsManager.cacheDir;
            cdn.client = new HttpClient();
            cdn.client.Timeout = new TimeSpan(0, 5, 0);
            cdn.cdnList = new List<string> {
                "level3.blizzard.com",      // Level3
                "eu.cdn.blizzard.com",      // Official EU CDN
                "blzddist1-a.akamaihd.net", // Akamai first
                "cdn.blizzard.com",         // Official regionless CDN
                "us.cdn.blizzard.com",      // Official US CDN
                "client01.pdl.wow.battlenet.com.cn", // China 1
                "client02.pdl.wow.battlenet.com.cn", // China 2
                "client03.pdl.wow.battlenet.com.cn", // China 3
                "client04.pdl.wow.battlenet.com.cn", // China 4
                "client04.pdl.wow.battlenet.com.cn", // China 5
                "blizzard.nefficient.co.kr", // Korea 
            };

            // Check if cache/backup directory exists
            if (!Directory.Exists(cdn.cacheDir)) { Directory.CreateDirectory(cdn.cacheDir); }

            if (args.Length > 0)
            {
                if (args[0] == "dumpinfo")
                {
                    if (args.Length != 4) throw new Exception("Not enough arguments. Need mode, product, buildconfig, cdnconfig");

                    cdns = GetCDNs(args[1]);

                    buildConfig = GetBuildConfig(Path.Combine(cdn.cacheDir, cdns.entries[0].path), args[2]);
                    if (string.IsNullOrWhiteSpace(buildConfig.buildName)) { Console.WriteLine("Invalid buildConfig!"); }

                    cdnConfig = GetCDNconfig(Path.Combine(cdn.cacheDir, cdns.entries[0].path), args[3]);
                    if (cdnConfig.archives == null) { Console.WriteLine("Invalid cdnConfig"); }

                    encoding = GetEncoding(Path.Combine(cdn.cacheDir, cdns.entries[0].path), buildConfig.encoding[1]);

                    string rootKey = "";
                    string downloadKey = "";
                    string installKey = "";

                    if (buildConfig.download.Length == 2)
                    {
                        downloadKey = buildConfig.download[1];
                        Console.WriteLine("download = " + downloadKey.ToLower());
                    }

                    if (buildConfig.install.Length == 2)
                    {
                        installKey = buildConfig.install[1];
                        Console.WriteLine("install = " + installKey.ToLower());
                    }

                    Dictionary<string, string> hashes = new Dictionary<string, string>();

                    foreach (var entry in encoding.aEntries)
                    {
                        if (entry.hash == buildConfig.root.ToUpper()) { rootKey = entry.key; Console.WriteLine("root = " + entry.key.ToLower()); }
                        if (string.IsNullOrEmpty(downloadKey) && entry.hash == buildConfig.download[0].ToUpper()) { downloadKey = entry.key; Console.WriteLine("download = " + entry.key.ToLower()); }
                        if (string.IsNullOrEmpty(installKey) && entry.hash == buildConfig.install[0].ToUpper()) { installKey = entry.key; Console.WriteLine("install = " + entry.key.ToLower()); }
                        if (!hashes.ContainsKey(entry.key)) { hashes.Add(entry.key, entry.hash); }
                    }

                    GetIndexes(Path.Combine(cdn.cacheDir, cdns.entries[0].path), cdnConfig.archives);

                    foreach (var entry in indexDictionary)
                    {
                        hashes.Remove(entry.Key.ToUpper());
                    }

                    int h = 1;
                    var tot = hashes.Count;

                    foreach (var entry in hashes)
                    {
                        Console.WriteLine("unarchived = " + entry.Key.ToLower());
                        h++;
                    }

                    Environment.Exit(0);
                }
                if (args[0] == "dumproot")
                {
                    if (args.Length != 2) throw new Exception("Not enough arguments. Need mode, root");
                    cdns = GetCDNs("wow");

                    var fileNames = new Dictionary<ulong, string>();
                    UpdateListfile();
                    var hasher = new Jenkins96();
                    foreach (var line in File.ReadLines("listfile.txt"))
                    {
                        fileNames.Add(hasher.ComputeHash(line), line);
                    }

                    var root = GetRoot(cdns.entries[0].path + "/", args[1], true);

                    foreach (var entry in root.entriesFDID)
                    {
                        foreach (var subentry in entry.Value)
                        {
                            if (subentry.contentFlags.HasFlag(ContentFlags.LowViolence)) continue;

                            if (!subentry.localeFlags.HasFlag(LocaleFlags.All_WoW) && !subentry.localeFlags.HasFlag(LocaleFlags.enUS))
                            {
                                continue;
                            }

                            if (entry.Key > 0 && fileNames.ContainsKey(entry.Key))
                            {
                                Console.WriteLine(fileNames[entry.Key] + ";" + entry.Key.ToString("x").PadLeft(16, '0') + ";" + subentry.fileDataID + ";" + BitConverter.ToString(subentry.md5).Replace("-", string.Empty).ToLower());
                            }
                            else
                            {
                                Console.WriteLine("unknown;" + entry.Key.ToString("x").PadLeft(16, '0') + ";" + subentry.fileDataID + ";" + BitConverter.ToString(subentry.md5).Replace("-", string.Empty).ToLower());
                            }
                        }

                    }

                    Environment.Exit(0);
                }
                if (args[0] == "dumproot2")
                {
                    if (args.Length != 2) throw new Exception("Not enough arguments. Need mode, root");
                    cdns = GetCDNs("wow");

                    var hasher = new Jenkins96();
                    UpdateListfile();
                    var hashes = File
                        .ReadLines("listfile.txt")
                        .Select<string, Tuple<ulong, string>>(fileName => new Tuple<ulong, string>(hasher.ComputeHash(fileName), fileName))
                        .ToDictionary(key => key.Item1, value => value.Item2);

                    var root = GetRoot(cdns.entries[0].path + "/", args[1], true);

                    Action<RootEntry> print = delegate (RootEntry entry)
                    {
                        var lookup = "";
                        var fileName = "";

                        if (entry.lookup > 0)
                        {
                            lookup = entry.lookup.ToString("x").PadLeft(16, '0');
                            fileName = hashes.ContainsKey(entry.lookup) ? hashes[entry.lookup] : "";
                        }

                        var md5 = BitConverter.ToString(entry.md5).Replace("-", string.Empty).ToLower();
                        var dataId = entry.fileDataID;
                        Console.WriteLine("{0};{1};{2};{3}", fileName, lookup, dataId, md5);
                    };

                    foreach (var entry in root.entriesFDID)
                    {
                        RootEntry? prioritizedEntry = entry.Value.FirstOrDefault(subentry =>
                            subentry.contentFlags.HasFlag(ContentFlags.LowViolence) == false && (subentry.localeFlags.HasFlag(LocaleFlags.All_WoW) || subentry.localeFlags.HasFlag(LocaleFlags.enUS))
                        );

                        var selectedEntry = (prioritizedEntry.Value.md5 != null) ? prioritizedEntry.Value : entry.Value.First();
                        print(selectedEntry);
                    }

                    Environment.Exit(0);
                }
                if (args[0] == "calchash")
                {
                    var hasher = new Jenkins96();
                    var hash = hasher.ComputeHash(args[1]);
                    Console.WriteLine(hash + " " + hash.ToString("x").PadLeft(16, '0'));
                    Environment.Exit(0);
                }
                if (args[0] == "calchashlistfile")
                {
                    string target = "";

                    if (args.Length == 2 && File.Exists(args[1]))
                    {
                        target = args[1];
                    }
                    else
                    {
                        UpdateListfile();
                        target = "listfile.txt";
                    }

                    var hasher = new Jenkins96();

                    foreach (var line in File.ReadLines(target))
                    {
                        if (string.IsNullOrEmpty(line)) continue;
                        var hash = hasher.ComputeHash(line);
                        Console.WriteLine(line + " = " + hash.ToString("x").PadLeft(16, '0'));
                    }
                    Environment.Exit(0);
                }
                if (args[0] == "dumpinstall")
                {
                    if (args.Length != 3) throw new Exception("Not enough arguments. Need mode, product, install");

                    cdns = GetCDNs(args[1]);
                    install = GetInstall(cdns.entries[0].path + "/", args[2], true);
                    foreach (var entry in install.entries)
                    {
                        Console.WriteLine(entry.name + " (size: " + entry.size + ", md5: " + BitConverter.ToString(entry.contentHash).Replace("-", string.Empty).ToLower() + ", tags: " + string.Join(",", entry.tags) + ")");
                    }
                    Environment.Exit(0);
                }

                if (args[0] == "dumpencoding")
                {
                    if (args.Length != 3) throw new Exception("Not enough arguments. Need mode, product, encoding");

                    cdns = GetCDNs(args[1]);
                    encoding = GetEncoding(cdns.entries[0].path + "/", args[2], 0, true);
                    foreach (var entry in encoding.aEntries)
                    {
                        var table2Entry = encoding.bEntries[entry.key];
                        Console.WriteLine(entry.hash.ToLower() + " " + entry.key.ToLower() + " " + entry.keyCount + " " + entry.size + " " + encoding.stringBlockEntries[table2Entry.stringIndex]);
                    }
                    Console.WriteLine("ENCODINGESPEC " + encoding.encodingESpec);
                    Environment.Exit(0);
                }

                if (args[0] == "extractfilebycontenthash" || args[0] == "extractrawfilebycontenthash")
                {
                    if (args.Length != 6) throw new Exception("Not enough arguments. Need mode, product, buildconfig, cdnconfig, contenthash, outname");

                    cdns = GetCDNs(args[1]);

                    args[4] = args[4].ToLower();

                    buildConfig = GetBuildConfig(Path.Combine(cdn.cacheDir, cdns.entries[0].path), args[2]);
                    if (string.IsNullOrWhiteSpace(buildConfig.buildName)) { Console.WriteLine("Invalid buildConfig!"); }

                    encoding = GetEncoding(Path.Combine(cdn.cacheDir, cdns.entries[0].path), buildConfig.encoding[1]);

                    string target = "";

                    foreach (var entry in encoding.aEntries)
                    {
                        if (entry.hash.ToLower() == args[4])
                        {
                            target = entry.key.ToLower();
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(target))
                    {
                        throw new Exception("File not found in encoding!");
                    }

                    cdnConfig = GetCDNconfig(Path.Combine(cdn.cacheDir, cdns.entries[0].path), args[3]);

                    GetIndexes(Path.Combine(cdn.cacheDir, cdns.entries[0].path), cdnConfig.archives);

                    if (args[0] == "extractrawfilebycontenthash")
                    {
                        var unarchivedName = Path.Combine(cdn.cacheDir, cdns.entries[0].path, "data", target[0] + "" + target[1], target[2] + "" + target[3], target);

                        Directory.CreateDirectory(Path.GetDirectoryName(unarchivedName));

                        File.WriteAllBytes(unarchivedName, RetrieveFileBytes(target, true, cdns.entries[0].path));
                    }
                    else
                    {
                        File.WriteAllBytes(args[5], RetrieveFileBytes(target, false, cdns.entries[0].path));
                    }

                    Environment.Exit(0);
                }
                if (args[0] == "extractfilesbylist")
                {
                    if (args.Length != 5) throw new Exception("Not enough arguments. Need mode, buildconfig, cdnconfig, basedir, list");

                    buildConfig = GetBuildConfig(Path.Combine(cdn.cacheDir, "tpr", "wow"), args[1]);
                    if (string.IsNullOrWhiteSpace(buildConfig.buildName)) { Console.WriteLine("Invalid buildConfig!"); }

                    encoding = GetEncoding(Path.Combine(cdn.cacheDir, "tpr", "wow"), buildConfig.encoding[1]);

                    var basedir = args[3];

                    var lines = File.ReadLines(args[4]);

                    cdnConfig = GetCDNconfig(Path.Combine(cdn.cacheDir, "tpr", "wow"), args[2]);

                    GetIndexes(Path.Combine(cdn.cacheDir, "tpr", "wow"), cdnConfig.archives);

                    foreach (var line in lines)
                    {
                        var splitLine = line.Split(',');
                        var contenthash = splitLine[0];
                        var filename = splitLine[1];

                        if (!Directory.Exists(Path.Combine(basedir, Path.GetDirectoryName(filename))))
                        {
                            Directory.CreateDirectory(Path.Combine(basedir, Path.GetDirectoryName(filename)));
                        }

                        Console.WriteLine(filename);

                        string target = "";

                        foreach (var entry in encoding.aEntries)
                        {
                            if (entry.hash.ToLower() == contenthash.ToLower()) { target = entry.key.ToLower(); Console.WriteLine("Found target: " + target); break; }
                        }

                        if (string.IsNullOrEmpty(target))
                        {
                            Console.WriteLine("File " + filename + " (" + contenthash + ") not found in encoding!");
                            continue;
                        }

                        try
                        {
                            File.WriteAllBytes(Path.Combine(basedir, filename), RetrieveFileBytes(target));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }

                    Environment.Exit(0);
                }
                if (args[0] == "extractfilesbyfnamelist" || args[0] == "extractfilesbyfdidlist")
                {
                    if (args.Length != 5) throw new Exception("Not enough arguments. Need mode, buildconfig, cdnconfig, basedir, list");

                    buildConfig = GetBuildConfig(Path.Combine(cdn.cacheDir, "tpr", "wow"), args[1]);
                    if (string.IsNullOrWhiteSpace(buildConfig.buildName)) { Console.WriteLine("Invalid buildConfig!"); }

                    encoding = GetEncoding(Path.Combine(cdn.cacheDir, "tpr", "wow"), buildConfig.encoding[1]);

                    cdnConfig = GetCDNconfig(Path.Combine(cdn.cacheDir, "tpr", "wow"), args[2]);

                    GetIndexes(Path.Combine(cdn.cacheDir, "tpr", "wow"), cdnConfig.archives);

                    var basedir = args[3];

                    var lines = File.ReadLines(args[4]);

                    var rootHash = "";

                    foreach (var entry in encoding.aEntries)
                    {
                        if (entry.hash.ToLower() == buildConfig.root.ToLower()) { rootHash = entry.key.ToLower(); break; }
                    }

                    var hasher = new Jenkins96();
                    var nameList = new Dictionary<ulong, string>();
                    var fdidList = new Dictionary<uint, string>();

                    if (args[0] == "extractfilesbyfnamelist")
                    {
                        foreach (var line in lines)
                        {
                            var hash = hasher.ComputeHash(line);
                            nameList.Add(hash, line);
                        }
                    }
                    else if (args[0] == "extractfilesbyfdidlist")
                    {
                        foreach (var line in lines)
                        {
                            if (string.IsNullOrEmpty(line))
                                continue;

                            var expl = line.Split(';');
                            if (expl.Length == 1)
                            {
                                fdidList.Add(uint.Parse(expl[0]), expl[0]);
                            }
                            else
                            {
                                fdidList.Add(uint.Parse(expl[0]), expl[1]);
                            }
                        }
                    }

                    Console.WriteLine("Looking up in root..");

                    root = GetRoot(Path.Combine(cdn.cacheDir, "tpr", "wow"), rootHash, true);

                    var encodingList = new Dictionary<string, List<string>>();

                    foreach (var entry in root.entriesFDID)
                    {
                        foreach (var subentry in entry.Value)
                        {
                            if (subentry.contentFlags.HasFlag(ContentFlags.LowViolence)) continue;

                            if (!subentry.localeFlags.HasFlag(LocaleFlags.All_WoW) && !subentry.localeFlags.HasFlag(LocaleFlags.enUS))
                            {
                                continue;
                            }

                            if (args[0] == "extractfilesbyfnamelist")
                            {
                                if (nameList.ContainsKey(subentry.lookup))
                                {
                                    var cleanContentHash = BitConverter.ToString(subentry.md5).Replace("-", string.Empty).ToLower();

                                    if (encodingList.ContainsKey(cleanContentHash))
                                    {
                                        encodingList[cleanContentHash].Add(nameList[subentry.lookup]);
                                    }
                                    else
                                    {
                                        encodingList.Add(cleanContentHash, new List<string>() { nameList[subentry.lookup] });
                                    }
                                }
                            }
                            else if (args[0] == "extractfilesbyfdidlist")
                            {
                                if (fdidList.ContainsKey(subentry.fileDataID))
                                {
                                    var cleanContentHash = BitConverter.ToString(subentry.md5).Replace("-", string.Empty).ToLower();

                                    if (encodingList.ContainsKey(cleanContentHash))
                                    {
                                        encodingList[cleanContentHash].Add(fdidList[subentry.fileDataID]);
                                    }
                                    else
                                    {
                                        encodingList.Add(cleanContentHash, new List<string>() { fdidList[subentry.fileDataID] });
                                    }
                                }
                            }

                            continue;
                        }
                    }

                    var fileList = new Dictionary<string, List<string>>();

                    Console.WriteLine("Looking up in encoding..");
                    foreach (var encodingEntry in encoding.aEntries)
                    {
                        string target = "";

                        if (encodingList.ContainsKey(encodingEntry.hash.ToLower()))
                        {
                            target = encodingEntry.key.ToLower();
                            //Console.WriteLine(target);
                            foreach (var subName in encodingList[encodingEntry.hash.ToLower()])
                            {
                                if (fileList.ContainsKey(target))
                                {
                                    fileList[target].Add(subName);
                                }
                                else
                                {
                                    fileList.Add(target, new List<string>() { subName });
                                }
                            }
                            encodingList.Remove(encodingEntry.hash.ToLower());
                        }
                    }

                    var archivedFileList = new Dictionary<string, Dictionary<string, List<string>>>();
                    var unarchivedFileList = new Dictionary<string, List<string>>();

                    Console.WriteLine("Looking up in indexes..");
                    foreach (var fileEntry in fileList)
                    {
                        if (!indexDictionary.TryGetValue(fileEntry.Key.ToUpper(), out IndexEntry entry))
                        {
                            unarchivedFileList.Add(fileEntry.Key, fileEntry.Value);
                        }

                        var index = cdnConfig.archives[entry.index];
                        if (!archivedFileList.ContainsKey(index))
                        {
                            archivedFileList.Add(index, new Dictionary<string, List<string>>());
                        }

                        archivedFileList[index].Add(fileEntry.Key, fileEntry.Value);
                    }

                    var extractedFiles = 0;
                    var totalFiles = fileList.Count;

                    Console.WriteLine("Extracting " + unarchivedFileList.Count + " unarchived files..");
                    foreach (var fileEntry in unarchivedFileList)
                    {
                        var target = fileEntry.Key;

                        foreach (var filename in fileEntry.Value)
                        {
                            if (!Directory.Exists(Path.Combine(basedir, Path.GetDirectoryName(filename))))
                            {
                                Directory.CreateDirectory(Path.Combine(basedir, Path.GetDirectoryName(filename)));
                            }
                        }

                        var unarchivedName = Path.Combine(cdn.cacheDir, "tpr", "wow", "data", target[0] + "" + target[1], target[2] + "" + target[3], target);
                        if (File.Exists(unarchivedName))
                        {
                            foreach (var filename in fileEntry.Value)
                            {
                                try
                                {
                                    File.WriteAllBytes(Path.Combine(basedir, filename), BLTE.Parse(File.ReadAllBytes(unarchivedName)));
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.Message);
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Unarchived file does not exist " + unarchivedName + ", cannot extract " + string.Join(',', fileEntry.Value));
                        }

                        extractedFiles++;

                        if (extractedFiles % 100 == 0)
                        {
                            Console.WriteLine("[" + DateTime.Now.ToString() + "] Extracted " + extractedFiles + " out of " + totalFiles + " files");
                        }
                    }

                    foreach (var archiveEntry in archivedFileList)
                    {
                        var archiveName = Path.Combine(cdn.cacheDir, "tpr", "wow", "data", archiveEntry.Key[0] + "" + archiveEntry.Key[1], archiveEntry.Key[2] + "" + archiveEntry.Key[3], archiveEntry.Key);
                        Console.WriteLine("[" + DateTime.Now.ToString() + "] Extracting " + archiveEntry.Value.Count + " files from archive " + archiveEntry.Key + "..");

                        using (var stream = new MemoryStream(File.ReadAllBytes(archiveName)))
                        {
                            foreach (var fileEntry in archiveEntry.Value)
                            {
                                var target = fileEntry.Key;

                                foreach (var filename in fileEntry.Value)
                                {
                                    if (!Directory.Exists(Path.Combine(basedir, Path.GetDirectoryName(filename))))
                                    {
                                        Directory.CreateDirectory(Path.Combine(basedir, Path.GetDirectoryName(filename)));
                                    }
                                }

                                if (indexDictionary.TryGetValue(target.ToUpper(), out IndexEntry entry))
                                {
                                    foreach (var filename in fileEntry.Value)
                                    {
                                        if (File.Exists(Path.Combine(basedir, filename)))
                                            continue;

                                        try
                                        {
                                            stream.Seek(entry.offset, SeekOrigin.Begin);

                                            if (entry.offset > stream.Length || entry.offset + entry.size > stream.Length)
                                            {
                                                throw new Exception("File is beyond archive length, incomplete archive!");
                                            }

                                            var archiveBytes = new byte[entry.size];
                                            stream.Read(archiveBytes, 0, (int)entry.size);
                                            File.WriteAllBytes(Path.Combine(basedir, filename), BLTE.Parse(archiveBytes));
                                        }
                                        catch (Exception e)
                                        {
                                            Console.WriteLine(e.Message);
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("!!!!! Unable to find " + fileEntry.Key + " (" + fileEntry.Value[0] + ") in archives!");
                                }

                                extractedFiles++;

                                if (extractedFiles % 1000 == 0)
                                {
                                    Console.WriteLine("[" + DateTime.Now.ToString() + "] Extracted " + extractedFiles + " out of " + totalFiles + " files");
                                }
                            }
                        }
                    }

                    Environment.Exit(0);
                }
                if (args[0] == "forcebuild")
                {
                    if (args.Length == 4)
                    {
                        checkPrograms = new string[] { args[1] };
                        overrideBuildconfig = args[2];
                        overrideCDNconfig = args[3];
                        overrideVersions = true;
                    }
                }
                if (args[0] == "forceprogram")
                {
                    if (args.Length == 2)
                    {
                        checkPrograms = new string[] { args[1] };
                    }
                }
                if (args[0] == "dumpencrypted")
                {
                    if (args.Length != 3) throw new Exception("Not enough arguments. Need mode, product, buildconfig");

                    if (args[1] != "wow")
                    {
                        Console.WriteLine("Only WoW is currently supported due to root/fileDataID usage");
                        return;
                    }

                    cdns = GetCDNs(args[1]);

                    buildConfig = GetBuildConfig(Path.Combine(cdn.cacheDir, cdns.entries[0].path), args[2]);

                    encoding = GetEncoding(Path.Combine(cdn.cacheDir, cdns.entries[0].path), buildConfig.encoding[1], 0, true);

                    var encryptedKeys = new Dictionary<string, string>();
                    var encryptedSizes = new Dictionary<string, ulong>();
                    foreach (var entry in encoding.bEntries)
                    {
                        var stringBlockEntry = encoding.stringBlockEntries[entry.Value.stringIndex];
                        if (stringBlockEntry.Contains("e:"))
                        {
                            encryptedKeys.Add(entry.Key, stringBlockEntry);
                            encryptedSizes.Add(entry.Key, entry.Value.compressedSize);
                        }
                    }

                    string rootKey = "";
                    var encryptedContentHashes = new Dictionary<string, string>();
                    var encryptedContentSizes = new Dictionary<string, ulong>();
                    foreach (var entry in encoding.aEntries)
                    {
                        if (encryptedKeys.ContainsKey(entry.key))
                        {
                            encryptedContentHashes.Add(entry.hash, encryptedKeys[entry.key]);
                            encryptedContentSizes.Add(entry.hash, entry.size);
                        }

                        if (entry.hash == buildConfig.root.ToUpper()) { rootKey = entry.key.ToLower(); }
                    }

                    root = GetRoot(Path.Combine(cdn.cacheDir, cdns.entries[0].path), rootKey, true);

                    foreach (var entry in root.entriesFDID)
                    {
                        foreach (var subentry in entry.Value)
                        {
                            if (encryptedContentHashes.ContainsKey(BitConverter.ToString(subentry.md5).Replace("-", "")))
                            {
                                var stringBlock = encryptedContentHashes[BitConverter.ToString(subentry.md5).Replace("-", "")];
                                var rawStringBlock = stringBlock;
                                var keyList = new List<string>();
                                while (stringBlock.Contains("e:"))
                                {
                                    var keyName = stringBlock.Substring(stringBlock.IndexOf("e:{") + 3, 16);
                                    if (!keyList.Contains(keyName))
                                    {
                                        keyList.Add(keyName);
                                    }
                                    stringBlock = stringBlock.Remove(stringBlock.IndexOf("e:{"), 19);
                                }

                                Console.WriteLine(subentry.fileDataID + " " + string.Join(',', keyList) + " " + rawStringBlock + " " + encryptedContentSizes[BitConverter.ToString(subentry.md5).Replace("-", "")]);
                                break;
                            }
                        }
                    }

                    Environment.Exit(0);
                }
                if (args[0] == "dumpsizes")
                {
                    if (args.Length != 3) throw new Exception("Not enough arguments. Need mode, product, buildconfig");

                    if (args[1] != "wow")
                    {
                        Console.WriteLine("Only WoW is currently supported due to root/fileDataID usage");
                        return;
                    }

                    cdns = GetCDNs(args[1]);

                    buildConfig = GetBuildConfig(Path.Combine(cdn.cacheDir, cdns.entries[0].path), args[2]);

                    encoding = GetEncoding(Path.Combine(cdn.cacheDir, cdns.entries[0].path), buildConfig.encoding[1], 0, true);

                    foreach (var entry in encoding.aEntries)
                    {
                        Console.WriteLine(entry.hash.ToLower() + " " + entry.size);
                    }

                    Environment.Exit(0);
                }

                if (args[0] == "dumprawfile")
                {
                    if (args.Length < 2) throw new Exception("Not enough arguments. Need mode, path, (numbytes)");

                    var file = BLTE.Parse(File.ReadAllBytes(args[1]));

                    if (args.Length == 3)
                    {
                        file = file.Take(int.Parse(args[2])).ToArray();
                    }

                    Console.Write(Encoding.UTF8.GetString(file));
                    Environment.Exit(0);
                }
                if (args[0] == "dumprawfiletofile")
                {
                    if (args.Length != 3) throw new Exception("Not enough arguments. Need mode, path, outfile");
                    File.WriteAllBytes(args[2], BLTE.Parse(File.ReadAllBytes(args[1])));
                    Environment.Exit(0);
                }
                if (args[0] == "dumpindex")
                {
                    if (args.Length < 3) throw new Exception("Not enough arguments. Need mode, product, hash, (folder)");

                    cdns = GetCDNs(args[1]);

                    var folder = "data";

                    if (args.Length == 4)
                    {
                        folder = args[3];
                    }

                    var index = ParseIndex(cdns.entries[0].path + "/", args[2], folder);

                    foreach (var entry in index)
                    {
                        Console.WriteLine(entry.Key + " " + entry.Value.size);
                    }
                    Environment.Exit(0);
                }
            }

            // Load programs
            if (checkPrograms == null)
            {
                checkPrograms = new string[] { "agent", "wow", "wowt", "wowdev", "wow_beta", "wowe1", "wowe2", "wowe3", "wowv", "wowz", "catalogs", "wowdemo", "wow_classic", "wow_classic_beta", "wow_classic_ptr" };
            }

            backupPrograms = new string[] { "agent", "wow", "wowt", "wow_beta", "wowdev", "wowe1", "wowe2", "wowe3", "wowv", "wowz", "wowdemo", "wow_classic", "wow_classic_beta", "wow_classic_ptr" };

            foreach (string program in checkPrograms)
            {
                Console.WriteLine("Using program " + program);

                try
                {
                    versions = GetVersions(program);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error parsing versions: " + e.Message);
                }

                if (versions.entries == null || versions.entries.Count() == 0) { Console.WriteLine("Invalid versions file for " + program + ", skipping!"); continue; }
                Console.WriteLine("Loaded " + versions.entries.Count() + " versions");

                try
                {
                    cdns = GetCDNs(program);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error parsing CDNs: " + e.Message);
                }

                if (cdns.entries == null || cdns.entries.Count() == 0) { Console.WriteLine("Invalid CDNs file for " + program + ", skipping!"); continue; }
                Console.WriteLine("Loaded " + cdns.entries.Count() + " cdns");

                if (!string.IsNullOrEmpty(versions.entries[0].productConfig))
                {
                    productConfig = GetProductConfig(cdns.entries[0].configPath + "/", versions.entries[0].productConfig);
                }

                gameblob = GetGameBlob(program);

                var decryptionKeyName = "";

                if (gameblob.decryptionKeyName != null && gameblob.decryptionKeyName != string.Empty)
                {
                    decryptionKeyName = gameblob.decryptionKeyName;
                }

                if (productConfig.decryptionKeyName != null && productConfig.decryptionKeyName != string.Empty)
                {
                    decryptionKeyName = productConfig.decryptionKeyName;
                }

                if (overrideVersions && !string.IsNullOrEmpty(overrideBuildconfig))
                {
                    buildConfig = GetBuildConfig(cdns.entries[0].path + "/", overrideBuildconfig);
                }
                else
                {
                    buildConfig = GetBuildConfig(cdns.entries[0].path + "/", versions.entries[0].buildConfig);
                }

                // Retrieve all buildconfigs
                for (var i = 0; i < versions.entries.Count(); i++)
                {
                    GetBuildConfig(cdns.entries[0].path + "/", versions.entries[i].buildConfig);
                }

                if (string.IsNullOrWhiteSpace(buildConfig.buildName))
                {
                    Console.WriteLine("Missing buildname in buildConfig for " + program + ", setting build name!");
                    buildConfig.buildName = "UNKNOWN";
                }

                if (overrideVersions && !string.IsNullOrEmpty(overrideCDNconfig))
                {
                    cdnConfig = GetCDNconfig(cdns.entries[0].path + "/", overrideCDNconfig);
                }
                else
                {
                    cdnConfig = GetCDNconfig(cdns.entries[0].path + "/", versions.entries[0].cdnConfig);
                }

                if (cdnConfig.builds != null)
                {
                    cdnBuildConfigs = new BuildConfigFile[cdnConfig.builds.Count()];
                }
                else if (cdnConfig.archives != null)
                {
                    //Console.WriteLine("CDNConfig loaded, " + cdnConfig.archives.Count() + " archives");
                }
                else
                {
                    Console.WriteLine("Invalid cdnConfig for " + program + "!");
                    continue;
                }

                if (!string.IsNullOrEmpty(versions.entries[0].keyRing)) cdn.Get(cdns.entries[0].path + "/config/" + versions.entries[0].keyRing[0] + versions.entries[0].keyRing[1] + "/" + versions.entries[0].keyRing[2] + versions.entries[0].keyRing[3] + "/" + versions.entries[0].keyRing);

                if (!string.IsNullOrEmpty(decryptionKeyName) && cdnConfig.archives == null) // Let us ignore this whole encryption thing if archives are set, surely this will never break anything and it'll back it up perfectly fine.
                {
                    if (!File.Exists(decryptionKeyName + ".ak"))
                    {
                        Console.WriteLine("Decryption key is set and not available on disk, skipping.");
                        cdn.isEncrypted = false;
                        continue;
                    }
                    else
                    {
                        cdn.isEncrypted = true;
                    }
                }
                else
                {
                    cdn.isEncrypted = false;
                }

                if (!backupPrograms.Contains(program))
                {
                    Console.WriteLine("No need to backup, moving on..");
                    continue;
                }

                Console.Write("Downloading patch files..");
                if (!string.IsNullOrEmpty(buildConfig.patch)) patch = GetPatch(cdns.entries[0].path + "/", buildConfig.patch, true);
                if (!string.IsNullOrEmpty(buildConfig.patchConfig)) cdn.Get(cdns.entries[0].path + "/config/" + buildConfig.patchConfig[0] + buildConfig.patchConfig[1] + "/" + buildConfig.patchConfig[2] + buildConfig.patchConfig[3] + "/" + buildConfig.patchConfig);
                Console.Write("..done\n");

                Console.Write("Loading " + cdnConfig.archives.Count() + " indexes..");
                GetIndexes(cdns.entries[0].path + "/", cdnConfig.archives);
                Console.Write("..done\n");

                Console.Write("Downloading " + cdnConfig.archives.Count() + " archives..");
                foreach (var archive in cdnConfig.archives)
                {
                    cdn.Get(cdns.entries[0].path + "/data/" + archive[0] + archive[1] + "/" + archive[2] + archive[3] + "/" + archive, false);
                }
                Console.Write("..done\n");

                Console.Write("Loading encoding..");

                try
                {
                    if (buildConfig.encodingSize == null || buildConfig.encodingSize.Count() < 2)
                    {
                        encoding = GetEncoding(cdns.entries[0].path + "/", buildConfig.encoding[1], 0);
                    }
                    else
                    {
                        encoding = GetEncoding(cdns.entries[0].path + "/", buildConfig.encoding[1], int.Parse(buildConfig.encodingSize[1]));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Fatal error occured during encoding parsing: " + e.Message);
                    continue;
                }

                Dictionary<string, string> hashes = new Dictionary<string, string>();

                string rootKey = "";
                string downloadKey = "";
                string installKey = "";

                if (buildConfig.install.Length == 2)
                {
                    installKey = buildConfig.install[1];
                }

                if (buildConfig.download.Length == 2)
                {
                    downloadKey = buildConfig.download[1];
                }

                foreach (var entry in encoding.aEntries)
                {
                    if (entry.hash == buildConfig.root.ToUpper()) { rootKey = entry.key.ToLower(); }
                    if (downloadKey == "" && entry.hash == buildConfig.download[0].ToUpper()) { downloadKey = entry.key.ToLower(); }
                    if (installKey == "" && entry.hash == buildConfig.install[0].ToUpper()) { installKey = entry.key.ToLower(); }
                    if (!hashes.ContainsKey(entry.key)) { hashes.Add(entry.key, entry.hash); }
                }

                Console.Write("..done\n");

                if (!string.IsNullOrEmpty(cdnConfig.fileIndex))
                {
                    Console.Write("Parsing file index..");
                    fileIndexList = ParseIndex(cdns.entries[0].path + "/", cdnConfig.fileIndex);
                    Console.Write("..done\n");

                }

                if (!string.IsNullOrEmpty(cdnConfig.patchFileIndex))
                {
                    Console.Write("Parsing patch file index..");
                    patchFileIndexList = ParseIndex(cdns.entries[0].path + "/", cdnConfig.patchFileIndex, "patch");
                    Console.Write("..done\n");
                }

                if (program == "wow" || program == "wowt" || program == "wow_beta" || program == "wow_classic") // Only these are supported right now
                {
                    Console.Write("Loading root..");
                    if (rootKey == "") { Console.WriteLine("Unable to find root key in encoding!"); } else { root = GetRoot(cdns.entries[0].path + "/", rootKey); }
                    Console.Write("..done\n");

                    Console.Write("Loading download..");
                    if (downloadKey == "") { Console.WriteLine("Unable to find download key in encoding!"); } else { download = GetDownload(cdns.entries[0].path + "/", downloadKey); }
                    Console.Write("..done\n");

                    Console.Write("Loading install..");
                    Console.Write("..done\n");

                    try
                    {
                        if (installKey == "") { Console.WriteLine("Unable to find install key in encoding!"); } else { install = GetInstall(cdns.entries[0].path + "/", installKey); }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error loading install: " + e.Message);
                    }
                }

                foreach (var entry in indexDictionary)
                {
                    hashes.Remove(entry.Key.ToUpper());
                }

                if (!string.IsNullOrEmpty(cdnConfig.fileIndex))
                {
                    Console.Write("Downloading " + fileIndexList.Count + " unarchived files from file index..");

                    Parallel.ForEach(fileIndexList.Keys, new ParallelOptions { MaxDegreeOfParallelism = 20 }, (entry) =>
                    {
                        cdn.Get(cdns.entries[0].path + "/data/" + entry[0] + entry[1] + "/" + entry[2] + entry[3] + "/" + entry, false);
                    });

                    Console.Write("..done\n");
                }

                if (!string.IsNullOrEmpty(cdnConfig.patchFileIndex))
                {
                    Console.Write("Downloading " + patchFileIndexList.Count + " unarchived patch files from patch file index..");

                    Parallel.ForEach(patchFileIndexList.Keys, new ParallelOptions { MaxDegreeOfParallelism = 20 }, (entry) =>
                    {
                        cdn.Get(cdns.entries[0].path + "/patch/" + entry[0] + entry[1] + "/" + entry[2] + entry[3] + "/" + entry, false);
                    });

                    Console.Write("..done\n");
                }

                Console.Write("Downloading " + hashes.Count() + " unarchived files..");

                foreach (var entry in hashes)
                {
                    cdn.Get(cdns.entries[0].path + "/data/" + entry.Key[0] + entry.Key[1] + "/" + entry.Key[2] + entry.Key[3] + "/" + entry.Key, false);
                }

                Console.Write("..done\n");

                if (cdnConfig.patchArchives != null)
                {
                    Console.Write("Downloading " + cdnConfig.patchArchives.Count() + " patch archives..");
                    for (var i = 0; i < cdnConfig.patchArchives.Count(); i++)
                    {
                        cdn.Get(cdns.entries[0].path + "/patch/" + cdnConfig.patchArchives[i][0] + cdnConfig.patchArchives[i][1] + "/" + cdnConfig.patchArchives[i][2] + cdnConfig.patchArchives[i][3] + "/" + cdnConfig.patchArchives[i], false);
                    }
                    Console.Write("..done\n");

                    Console.Write("Downloading " + cdnConfig.patchArchives.Count() + " patch archive indexes..");
                    GetPatchIndexes(cdns.entries[0].path + "/", cdnConfig.patchArchives);
                    Console.Write("..done\n");

                    if (patch.blocks != null)
                    {
                        var unarchivedPatchKeyList = new List<string>();
                        foreach (var block in patch.blocks)
                        {
                            foreach (var fileBlock in block.files)
                            {
                                foreach (var patch in fileBlock.patches)
                                {
                                    var pKey = BitConverter.ToString(patch.patchEncodingKey).Replace("-", "");
                                    if (!patchIndexDictionary.ContainsKey(pKey))
                                    {
                                        unarchivedPatchKeyList.Add(pKey);
                                    }
                                }
                            }
                        }

                        if (unarchivedPatchKeyList.Count > 0)
                        {
                            Console.Write("Downloading " + unarchivedPatchKeyList.Count + " unarchived patch files..");

                            foreach (var entry in unarchivedPatchKeyList)
                            {
                                cdn.Get(cdns.entries[0].path + "/patch/" + entry[0] + entry[1] + "/" + entry[2] + entry[3] + "/" + entry, false);
                            }

                            Console.Write("..done\n");
                        }
                    }
                }

                GC.Collect();
            }
        }

        private static byte[] RetrieveFileBytes(string target, bool raw = false, string cdndir = "tpr/wow")
        {
            var unarchivedName = Path.Combine(cdn.cacheDir, cdndir, "data", target[0] + "" + target[1], target[2] + "" + target[3], target);

            if (File.Exists(unarchivedName))
            {
                if (!raw)
                {
                    return BLTE.Parse(File.ReadAllBytes(unarchivedName));
                }
                else
                {
                    return File.ReadAllBytes(unarchivedName);
                }
            }

            if (!indexDictionary.TryGetValue(target.ToUpper(), out IndexEntry entry))
            {
                throw new Exception("Unable to find file in archives. File is not available!?");
            }
            else
            {
                var index = cdnConfig.archives[entry.index];

                var archiveName = Path.Combine(cdn.cacheDir, cdndir, "data", index[0] + "" + index[1], index[2] + "" + index[3], index);
                if (!File.Exists(archiveName))
                {
                    throw new FileNotFoundException("Unable to find archive " + index + " on disk!");
                }

                using (BinaryReader bin = new BinaryReader(File.Open(archiveName, FileMode.Open, FileAccess.Read)))
                {
                    bin.BaseStream.Position = entry.offset;
                    try
                    {
                        if (!raw)
                        {
                            return BLTE.Parse(bin.ReadBytes((int)entry.size));
                        }
                        else
                        {
                            return bin.ReadBytes((int)entry.size);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }

            return new byte[0];
        }

        private static CDNConfigFile GetCDNconfig(string url, string hash)
        {
            string content;
            var cdnConfig = new CDNConfigFile();

            try
            {
                content = Encoding.UTF8.GetString(cdn.Get(url + "/config/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash));
            }
            catch (Exception e)
            {
                Console.WriteLine("Error retrieving CDN config: " + e.Message);
                return cdnConfig;
            }

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

            return cdnConfig;
        }

        private static VersionsFile GetVersions(string program)
        {
            string content;
            var versions = new VersionsFile();

            using (HttpResponseMessage response = cdn.client.GetAsync(new Uri(baseUrl + program + "/" + "versions")).Result)
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
                    Console.WriteLine("Error during retrieving HTTP versions: Received bad HTTP code " + response.StatusCode);
                    return versions;
                }
            }

            /*
            try
            {
                var client = new Client(Region.EU);
                var request = client.Request("v1/products/" + program + "/versions");
                content = request.ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error during retrieving Ribbit versions: " + e.Message + ", trying HTTP..");
                try
                {
                    using (HttpResponseMessage response = cdn.client.GetAsync(new Uri(baseUrl + program + "/" + "versions")).Result)
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
                            Console.WriteLine("Error during retrieving HTTP versions: Received bad HTTP code " + response.StatusCode);
                            return versions;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error retrieving versions: " + ex.Message);
                    return versions;
                }
                return versions;
            }
            */
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

            return versions;
        }

        private static CdnsFile GetCDNs(string program)
        {
            string content;

            var cdns = new CdnsFile();

            using (HttpResponseMessage response = cdn.client.GetAsync(new Uri(baseUrl + program + "/" + "cdns")).Result)
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
                    Console.WriteLine("Error during retrieving HTTP cdns: Received bad HTTP code " + response.StatusCode);
                    return cdns;
                }
            }
            /*
            try
            {
                var client = new Client(Region.US);
                var request = client.Request("v1/products/" + program + "/cdns");
                content = request.ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error during retrieving Ribbit cdns: " + e.Message + ", trying HTTP..");
                try
                {
                    using (HttpResponseMessage response = cdn.client.GetAsync(new Uri(baseUrl + program + "/" + "cdns")).Result)
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
                            Console.WriteLine("Error during retrieving HTTP cdns: Received bad HTTP code " + response.StatusCode);
                            return cdns;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error retrieving CDNs file: " + ex.Message);
                    return cdns;
                }
            }
            */
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

            return cdns;
        }

        private static GameBlobFile GetGameBlob(string program)
        {
            string content;

            var gblob = new GameBlobFile();

            try
            {
                using (HttpResponseMessage response = cdn.client.GetAsync(new Uri(baseUrl + program + "/" + "blob/game")).Result)
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

        private static GameBlobFile GetProductConfig(string url, string hash)
        {
            string content;

            var gblob = new GameBlobFile();

            try
            {
                content = Encoding.UTF8.GetString(cdn.Get(url + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash));
            }
            catch (Exception e)
            {
                Console.WriteLine("Error retrieving product config: " + e.Message);
                return gblob;
            }

            if (string.IsNullOrEmpty(content))
            {
                Console.WriteLine("Error reading product config!");
                return gblob;
            }

            dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(content);
            if (json.all.config.decryption_key_name != null)
            {
                gblob.decryptionKeyName = json.all.config.decryption_key_name.Value;
            }
            return gblob;
        }

        private static BuildConfigFile GetBuildConfig(string url, string hash)
        {
            string content;

            var buildConfig = new BuildConfigFile();

            try
            {
                content = Encoding.UTF8.GetString(cdn.Get(url + "/config/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash));
            }
            catch (Exception e)
            {
                Console.WriteLine("Error retrieving build config: " + e.Message);
                return buildConfig;
            }

            if (string.IsNullOrEmpty(content) || !content.StartsWith("# Build"))
            {
                Console.WriteLine("Error reading build config!");
                return buildConfig;
            }

            var lines = content.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < lines.Count(); i++)
            {
                if (lines[i].StartsWith("#") || lines[i].Length == 0) { continue; }
                var cols = lines[i].Split(new string[] { " = " }, StringSplitOptions.RemoveEmptyEntries);
                switch (cols[0])
                {
                    case "root":
                        buildConfig.root = cols[1];
                        break;
                    case "download":
                        buildConfig.download = cols[1].Split(' ');
                        break;
                    case "install":
                        buildConfig.install = cols[1].Split(' ');
                        break;
                    case "encoding":
                        buildConfig.encoding = cols[1].Split(' ');
                        break;
                    case "encoding-size":
                        var encodingSize = cols[1].Split(' ');
                        buildConfig.encodingSize = encodingSize;
                        break;
                    case "size":
                        buildConfig.size = cols[1].Split(' ');
                        break;
                    case "size-size":
                        buildConfig.sizeSize = cols[1].Split(' ');
                        break;
                    case "build-name":
                        buildConfig.buildName = cols[1];
                        break;
                    case "build-playbuild-installer":
                        buildConfig.buildPlaybuildInstaller = cols[1];
                        break;
                    case "build-product":
                        buildConfig.buildProduct = cols[1];
                        break;
                    case "build-uid":
                        buildConfig.buildUid = cols[1];
                        break;
                    case "patch":
                        buildConfig.patch = cols[1];
                        break;
                    case "patch-size":
                        buildConfig.patchSize = cols[1];
                        break;
                    case "patch-config":
                        buildConfig.patchConfig = cols[1];
                        break;
                    case "build-branch": // Overwatch
                        buildConfig.buildBranch = cols[1];
                        break;
                    case "build-num": // Agent
                    case "build-number": // Overwatch
                    case "build-version": // Catalog
                        buildConfig.buildNumber = cols[1];
                        break;
                    case "build-attributes": // Agent
                        buildConfig.buildAttributes = cols[1];
                        break;
                    case "build-comments": // D3
                        buildConfig.buildComments = cols[1];
                        break;
                    case "build-creator": // D3
                        buildConfig.buildCreator = cols[1];
                        break;
                    case "build-fixed-hash": // S2
                        buildConfig.buildFixedHash = cols[1];
                        break;
                    case "build-replay-hash": // S2
                        buildConfig.buildReplayHash = cols[1];
                        break;
                    case "build-t1-manifest-version":
                        buildConfig.buildManifestVersion = cols[1];
                        break;
                    case "install-size":
                        buildConfig.installSize = cols[1].Split(' ');
                        break;
                    case "download-size":
                        buildConfig.downloadSize = cols[1].Split(' ');
                        break;
                    case "build-partial-priority":
                    case "partial-priority":
                        buildConfig.partialPriority = cols[1];
                        break;
                    case "partial-priority-size":
                        buildConfig.partialPrioritySize = cols[1];
                        break;
                    default:
                        Console.WriteLine("!!!!!!!! Unknown buildconfig variable '" + cols[0] + "'");
                        break;
                }
            }

            return buildConfig;
        }

        private static Dictionary<string, IndexEntry> ParseIndex(string url, string hash, string folder = "data")
        {
            byte[] indexContent = cdn.Get(url + folder + "/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash + ".index");

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
                    footer.numElements = bin.ReadUInt32(true);
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
                        var headerHash = BitConverter.ToString(bin.ReadBytes(footer.keySizeInBytes)).Replace("-", "");
                        var entry = new IndexEntry();

                        if (footer.sizeBytes == 4)
                        {
                            entry.size = bin.ReadUInt32(true);
                        }
                        else
                        {
                            throw new NotImplementedException("Index size reading other than 4 is not implemented!");
                        }

                        if (footer.offsetBytes == 4)
                        {
                            // Archive index
                            entry.offset = bin.ReadUInt32(true);
                        }
                        else if (footer.offsetBytes == 6)
                        {
                            // Group index
                            throw new NotImplementedException("Group index reading is not implemented!");
                        }
                        else
                        {
                            // File index
                        }

                        if (entry.size != 0)
                        {
                            returnDict.Add(headerHash, entry);
                        }
                    }

                    bin.ReadBytes(blockPadding);
                }
            }

            return returnDict;
        }

        private static List<string> ParsePatchFileIndex(string url, string hash)
        {
            byte[] indexContent = cdn.Get(url + "/patch/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash + ".index");

            var list = new List<string>();

            using (BinaryReader bin = new BinaryReader(new MemoryStream(indexContent)))
            {
                int indexEntries = indexContent.Length / 4096;

                for (var b = 0; b < indexEntries; b++)
                {
                    for (var bi = 0; bi < 170; bi++)
                    {
                        var headerHash = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");

                        var size = bin.ReadUInt32(true);

                        list.Add(headerHash);
                    }
                    bin.ReadBytes(16);
                }
            }

            return list;
        }

        private static void GetIndexes(string url, string[] archives)
        {
            Parallel.ForEach(archives, (archive, state, i) =>
            {
                byte[] indexContent = cdn.Get(url + "/data/" + archives[i][0] + archives[i][1] + "/" + archives[i][2] + archives[i][3] + "/" + archives[i] + ".index");

                using (BinaryReader bin = new BinaryReader(new MemoryStream(indexContent)))
                {
                    int indexEntries = indexContent.Length / 4096;

                    for (var b = 0; b < indexEntries; b++)
                    {
                        for (var bi = 0; bi < 170; bi++)
                        {
                            var headerHash = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");

                            var entry = new IndexEntry()
                            {
                                index = (short)i,
                                size = bin.ReadUInt32(true),
                                offset = bin.ReadUInt32(true)
                            };

                            cacheLock.EnterUpgradeableReadLock();
                            try
                            {
                                if (!indexDictionary.ContainsKey(headerHash))
                                {
                                    cacheLock.EnterWriteLock();
                                    try
                                    {
                                        indexDictionary.Add(headerHash, entry);
                                    }
                                    finally
                                    {
                                        cacheLock.ExitWriteLock();
                                    }
                                }
                            }
                            finally
                            {
                                cacheLock.ExitUpgradeableReadLock();
                            }
                        }
                        bin.ReadBytes(16);
                    }
                }
            });
        }
        private static void GetPatchIndexes(string url, string[] archives)
        {
            Parallel.ForEach(archives, (archive, state, i) =>
            {
                byte[] indexContent = cdn.Get(url + "/patch/" + archives[i][0] + archives[i][1] + "/" + archives[i][2] + archives[i][3] + "/" + archives[i] + ".index");

                using (BinaryReader bin = new BinaryReader(new MemoryStream(indexContent)))
                {
                    int indexEntries = indexContent.Length / 4096;

                    for (var b = 0; b < indexEntries; b++)
                    {
                        for (var bi = 0; bi < 170; bi++)
                        {
                            var headerHash = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");

                            var entry = new IndexEntry()
                            {
                                index = (short)i,
                                size = bin.ReadUInt32(true),
                                offset = bin.ReadUInt32(true)
                            };

                            cacheLock.EnterUpgradeableReadLock();
                            try
                            {
                                if (!patchIndexDictionary.ContainsKey(headerHash))
                                {
                                    cacheLock.EnterWriteLock();
                                    try
                                    {
                                        patchIndexDictionary.Add(headerHash, entry);
                                    }
                                    finally
                                    {
                                        cacheLock.ExitWriteLock();
                                    }
                                }
                            }
                            finally
                            {
                                cacheLock.ExitUpgradeableReadLock();
                            }
                        }
                        bin.ReadBytes(16);
                    }
                }
            });
        }

        private static RootFile GetRoot(string url, string hash, bool parseIt = false)
        {
            var root = new RootFile
            {
                entriesLookup = new MultiDictionary<ulong, RootEntry>(),
                entriesFDID = new MultiDictionary<uint, RootEntry>()
            };

            byte[] content = cdn.Get(url + "/data/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash);
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

        private static DownloadFile GetDownload(string url, string hash, bool parseIt = false)
        {
            var download = new DownloadFile();

            byte[] content = cdn.Get(url + "/data/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash);

            if (!parseIt) return download;

            using (BinaryReader bin = new BinaryReader(new MemoryStream(BLTE.Parse(content))))
            {
                if (Encoding.UTF8.GetString(bin.ReadBytes(2)) != "DL") { throw new Exception("Error while parsing download file. Did BLTE header size change?"); }
                download.unk = bin.ReadBytes(3); // Unk
                download.numEntries = bin.ReadUInt32(true);
                download.numTags = bin.ReadUInt16(true);

                download.entries = new DownloadEntry[download.numEntries];
                for (int i = 0; i < download.numEntries; i++)
                {
                    download.entries[i].hash = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");
                    bin.ReadBytes(10);
                }
            }

            return download;
        }

        private static InstallFile GetInstall(string url, string hash, bool parseIt = false)
        {
            var install = new InstallFile();

            byte[] content = cdn.Get(url + "/data/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash);

            if (!parseIt) return install;

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

        private static EncodingFile GetEncoding(string url, string hash, int encodingSize = 0, bool parseTableB = false, bool checkStuff = false)
        {
            var encoding = new EncodingFile();

            byte[] content;

            content = cdn.Get(url + "/data/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash);

            if (encodingSize != 0 && encodingSize != content.Length)
            {
                content = cdn.Get(url + "/data/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash, true);

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

        private static PatchFile GetPatch(string url, string hash, bool parseIt = false)
        {
            var patchFile = new PatchFile();

            byte[] content = cdn.Get(url + "/patch/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash);

            if (!parseIt) return patchFile;

            using (BinaryReader bin = new BinaryReader(new MemoryStream(content)))
            {
                if (Encoding.UTF8.GetString(bin.ReadBytes(2)) != "PA") { throw new Exception("Error while parsing patch file!"); }

                patchFile.version = bin.ReadByte();
                patchFile.fileKeySize = bin.ReadByte();
                patchFile.sizeB = bin.ReadByte();
                patchFile.patchKeySize = bin.ReadByte();
                patchFile.blockSizeBits = bin.ReadByte();
                patchFile.blockCount = bin.ReadUInt16(true);
                patchFile.flags = bin.ReadByte();
                patchFile.encodingContentKey = bin.ReadBytes(16);
                patchFile.encodingEncodingKey = bin.ReadBytes(16);
                patchFile.decodedSize = bin.ReadUInt32(true);
                patchFile.encodedSize = bin.ReadUInt32(true);
                patchFile.especLength = bin.ReadByte();
                patchFile.encodingSpec = new string(bin.ReadChars(patchFile.especLength));

                patchFile.blocks = new PatchBlock[patchFile.blockCount];
                for (var i = 0; i < patchFile.blockCount; i++)
                {
                    patchFile.blocks[i].lastFileContentKey = bin.ReadBytes(patchFile.fileKeySize);
                    patchFile.blocks[i].blockMD5 = bin.ReadBytes(16);
                    patchFile.blocks[i].blockOffset = bin.ReadUInt32(true);

                    var prevPos = bin.BaseStream.Position;

                    var files = new List<BlockFile>();

                    bin.BaseStream.Position = patchFile.blocks[i].blockOffset;
                    while (bin.BaseStream.Position <= patchFile.blocks[i].blockOffset + 0x10000)
                    {
                        var file = new BlockFile();

                        file.numPatches = bin.ReadByte();
                        if (file.numPatches == 0) break;
                        file.targetFileContentKey = bin.ReadBytes(patchFile.fileKeySize);
                        file.decodedSize = bin.ReadUInt40(true);

                        var filePatches = new List<FilePatch>();

                        for (var j = 0; j < file.numPatches; j++)
                        {
                            var filePatch = new FilePatch();
                            filePatch.sourceFileEncodingKey = bin.ReadBytes(patchFile.fileKeySize);
                            filePatch.decodedSize = bin.ReadUInt40(true);
                            filePatch.patchEncodingKey = bin.ReadBytes(patchFile.patchKeySize);
                            filePatch.patchSize = bin.ReadUInt32(true);
                            filePatch.patchIndex = bin.ReadByte();
                            filePatches.Add(filePatch);
                        }

                        file.patches = filePatches.ToArray();

                        files.Add(file);
                    }

                    patchFile.blocks[i].files = files.ToArray();
                    bin.BaseStream.Position = prevPos;
                }
            }

            return patchFile;
        }
        private static void UpdateListfile()
        {
            if (!File.Exists("listfile.txt") || DateTime.Now.AddHours(-1) > File.GetLastWriteTime("listfile.txt"))
            {
                using (var client = new System.Net.WebClient())
                using (var stream = new MemoryStream())
                {
                    client.Headers[System.Net.HttpRequestHeader.AcceptEncoding] = "gzip";
                    using (var responseStream = new System.IO.Compression.GZipStream(client.OpenRead("https://wow.tools/casc/listfile/download"), System.IO.Compression.CompressionMode.Decompress))
                    {
                        responseStream.CopyTo(stream);
                        File.WriteAllBytes("listfile.txt", stream.ToArray());
                    }
                }
            }
        }
    }
}
