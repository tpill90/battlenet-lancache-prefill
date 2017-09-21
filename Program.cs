using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace BuildBackup
{
    class Program
    {
        private static readonly Uri baseUrl = new Uri("http://us.patch.battle.net:1119/");

        private static string cacheDir;

        private static string[] checkPrograms;
        private static string[] backupPrograms;

        private static VersionsFile versions;
        private static CdnsFile cdns;
        private static GameBlobFile gameblob;

        private static BuildConfigFile buildConfig;
        private static BuildConfigFile[] cdnBuildConfigs;
        private static CDNConfigFile cdnConfig;
        private static ArchiveIndex[] indexes;
        private static EncodingFile encoding;
        private static InstallFile install;
        private static DownloadFile download;
        private static RootFile root;

        private static bool overrideVersions;
        private static string overrideBuildconfig;
        private static string overrideCDNconfig;

        private static HttpClient httpClient;

        private static Salsa20 salsa = new Salsa20();

        private static Salsa20 SalsaInstance => salsa;

        private static bool isEncrypted = false;

        static void Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                cacheDir = "H:/";
            }
            else
            {
                cacheDir = "/var/www/bnet.marlam.in/";
            }

            httpClient = new HttpClient();

            // Check if cache/backup directory exists
            if (!Directory.Exists(cacheDir)) { Directory.CreateDirectory(cacheDir); }

            if (args.Length > 0)
            {
                if (args[0] == "missingfiles")
                {
                    if (args.Length != 3) throw new Exception("Not enough arguments. Need mode, buildconfig, cdnconfig");

                    buildConfig = GetBuildConfig("wow", Path.Combine(cacheDir, "tpr", "wow"), args[1]);
                    if (string.IsNullOrWhiteSpace(buildConfig.buildName)) { Console.WriteLine("Invalid buildConfig!"); }

                    cdnConfig = GetCDNconfig("wow", Path.Combine(cacheDir, "tpr", "wow"), args[2]);
                    if (cdnConfig.archives == null) { Console.WriteLine("Invalid cdnConfig"); }

                    encoding = GetEncoding(Path.Combine(cacheDir, "tpr", "wow"), buildConfig.encoding[1]);

                    Dictionary<string, string> hashes = new Dictionary<string, string>();

                    foreach (var entry in encoding.aEntries)
                    {
                        if (entry.hash == buildConfig.root.ToUpper()) { root = GetRoot(Path.Combine(cacheDir, "tpr", "wow"), entry.hash.ToLower()); }
                        if (!hashes.ContainsKey(entry.key)) { hashes.Add(entry.key, entry.hash); }
                    }

                    indexes = GetIndexes(Path.Combine(cacheDir, "tpr", "wow"), cdnConfig.archives);

                    foreach (var index in indexes)
                    {
                        // If respective archive does not exist, add to separate list



                        // Remove from list as usual
                        foreach (var entry in index.archiveIndexEntries)
                        {
                            hashes.Remove(entry.headerHash);
                        }
                    }

                    // Run through root to see which file hashes belong to which missing file and put those in a list
                    // Run through listfile to see if files are known
                    Environment.Exit(0);
                }
                if (args[0] == "dumpinfo")
                {
                    if (args.Length != 4) throw new Exception("Not enough arguments. Need mode, product, buildconfig, cdnconfig");

                    cdns = GetCDNs(args[1]);

                    buildConfig = GetBuildConfig(args[1], Path.Combine(cacheDir, cdns.entries[0].path), args[2]);
                    if (string.IsNullOrWhiteSpace(buildConfig.buildName)) { Console.WriteLine("Invalid buildConfig!"); }

                    cdnConfig = GetCDNconfig(args[1], Path.Combine(cacheDir, cdns.entries[0].path), args[3]);
                    if (cdnConfig.archives == null) { Console.WriteLine("Invalid cdnConfig"); }

                    encoding = GetEncoding(Path.Combine(cacheDir, cdns.entries[0].path), buildConfig.encoding[1]);

                    string rootKey = "";
                    string downloadKey = "";
                    string installKey = "";

                    Dictionary<string, string> hashes = new Dictionary<string, string>();

                    foreach (var entry in encoding.aEntries)
                    {
                        if (entry.hash == buildConfig.root.ToUpper()) { rootKey = entry.key; Console.WriteLine("root = " + entry.key.ToLower()); }
                        if (entry.hash == buildConfig.download[0].ToUpper()) { downloadKey = entry.key; Console.WriteLine("download = " + entry.key.ToLower()); }
                        if (entry.hash == buildConfig.install[0].ToUpper()) { installKey = entry.key; Console.WriteLine("install = " + entry.key.ToLower()); }
                        if (!hashes.ContainsKey(entry.key)) { hashes.Add(entry.key, entry.hash); }
                    }

                    indexes = GetIndexes(Path.Combine(cacheDir, cdns.entries[0].path), cdnConfig.archives);

                    foreach (var index in indexes)
                    {
                        //Console.WriteLine("Checking " + index.name + " " + index.archiveIndexEntries.Count() + " entries");
                        foreach (var entry in index.archiveIndexEntries)
                        {
                            hashes.Remove(entry.headerHash);
                            //Console.WriteLine("Removing " + entry.headerHash.ToLower() + " from list");
                        }
                    }

                    int h = 1;
                    var tot = hashes.Count;

                    foreach (var entry in hashes)
                    {
                        //Console.WriteLine("[" + h + "/" + tot + "] Downloading " + entry.Key);
                        Console.WriteLine("unarchived = " + entry.Key.ToLower());
                        h++;
                    }

                    Environment.Exit(1);
                }
                if (args[0] == "dumproot")
                {
                    if (args.Length != 2) throw new Exception("Not enough arguments. Need mode, root");
                    cdns = GetCDNs("wow");

                    var fileNames = new Dictionary<ulong, string>();

                    var hasher = new Jenkins96();
                    foreach (var line in File.ReadLines("listfile.txt"))
                    {
                        fileNames.Add(hasher.ComputeHash(line), line);
                    }

                    var root = GetRoot("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", args[1]);

                    foreach (var entry in root.entries)
                    {
                        foreach (var subentry in entry.Value)
                        {
                            if (subentry.contentFlags.HasFlag(ContentFlags.LowViolence)) continue;

                            if (!subentry.localeFlags.HasFlag(LocaleFlags.All_WoW) && !subentry.localeFlags.HasFlag(LocaleFlags.enUS))
                            {
                                continue;
                            }

                            if (fileNames.ContainsKey(entry.Key))
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

                    var fileNames = new Dictionary<ulong, string>();

                    var hasher = new Jenkins96();
                    foreach (var line in File.ReadLines("listfile.txt"))
                    {
                        fileNames.Add(hasher.ComputeHash(line), line);
                    }

                    var root = GetRoot("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", args[1]);

                    foreach (var entry in root.entries)
                    {
                        foreach (var subentry in entry.Value)
                        {
                            if (subentry.contentFlags.HasFlag(ContentFlags.LowViolence)) continue;

                            if (!subentry.localeFlags.HasFlag(LocaleFlags.All_WoW) && !subentry.localeFlags.HasFlag(LocaleFlags.enUS))
                            {
                                continue;
                            }

                            if (fileNames.ContainsKey(entry.Key))
                            {
                                Console.WriteLine(fileNames[entry.Key] + ";" + entry.Key.ToString("x").PadLeft(16, '0') + ";" + subentry.fileDataID + ";" + BitConverter.ToString(subentry.md5).Replace("-", string.Empty).ToLower());
                            }
                            else
                            {
                                Console.WriteLine(";" + entry.Key.ToString("x").PadLeft(16, '0') + ";" + subentry.fileDataID + ";" + BitConverter.ToString(subentry.md5).Replace("-", string.Empty).ToLower());
                            }
                        }

                    }

                    Environment.Exit(0);
                }
                if (args[0] == "diffroot")
                {
                    cdns = GetCDNs("wow");

                    var fileNames = new Dictionary<ulong, string>();

                    var hasher = new Jenkins96();
                    foreach (var line in File.ReadLines("listfile.txt"))
                    {
                        fileNames.Add(hasher.ComputeHash(line), line);
                    }

                    var root1 = GetRoot("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", args[1]);
                    var root2 = GetRoot("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", args[2]);

                    var unkFilenames = new List<ulong>();

                    foreach (var entry in root2.entries)
                    {
                        if (!root1.entries.ContainsKey(entry.Key))
                        {
                            // Added
                            if (fileNames.ContainsKey(entry.Key))
                            {
                                Console.WriteLine("[ADDED] <b>" + fileNames[entry.Key] + "</b> (lookup: " + entry.Key.ToString("x").PadLeft(16, '0') + ", content md5: " + BitConverter.ToString(entry.Value[0].md5).Replace("-", string.Empty).ToLower() + ", FileData ID: " + entry.Value[0].fileDataID + ")");
                            }
                            else
                            {
                                Console.WriteLine("[ADDED] <b>Unknown filename: " + entry.Key.ToString("x").PadLeft(16, '0') + "</b> (content md5: " + BitConverter.ToString(entry.Value[0].md5).Replace("-", string.Empty).ToLower() + ", FileData ID: " + entry.Value[0].fileDataID + ")");
                                unkFilenames.Add(entry.Key);
                            }
                        }
                    }

                    foreach (var entry in root1.entries)
                    {
                        if (!root2.entries.ContainsKey(entry.Key))
                        {
                            // Removed
                            if (fileNames.ContainsKey(entry.Key))
                            {
                                Console.WriteLine("[REMOVED] <b>" + fileNames[entry.Key] + "</b> (lookup: " + entry.Key.ToString("x").PadLeft(16, '0') + ", content md5: " + BitConverter.ToString(entry.Value[0].md5).Replace("-", string.Empty).ToLower() + ", FileData ID: " + entry.Value[0].fileDataID + ")");
                            }
                            else
                            {
                                Console.WriteLine("[REMOVED] <b>Unknown filename: " + entry.Key.ToString("x").PadLeft(16, '0') + "</b> (content md5: " + BitConverter.ToString(entry.Value[0].md5).Replace("-", string.Empty).ToLower() + ", FileData ID: " + entry.Value[0].fileDataID + ")");
                                unkFilenames.Add(entry.Key);
                            }
                        }
                        else
                        {
                            var r1md5 = BitConverter.ToString(entry.Value[0].md5).Replace("-", string.Empty).ToLower();
                            var r2md5 = BitConverter.ToString(root2.entries[entry.Key][0].md5).Replace("-", string.Empty).ToLower();
                            if (r1md5 != r2md5)
                            {
                                if (fileNames.ContainsKey(entry.Key))
                                {
                                    Console.WriteLine("[MODIFIED] <b>" + fileNames[entry.Key] + "</b> (lookup: " + entry.Key.ToString("x").PadLeft(16, '0') + ", FileData ID: " + entry.Value[0].fileDataID + ")");
                                }
                                else
                                {
                                    Console.WriteLine("[MODIFIED] <b>Unknown filename: " + entry.Key.ToString("x").PadLeft(16, '0') + "</b> (content md5: " + BitConverter.ToString(entry.Value[0].md5).Replace("-", string.Empty).ToLower() + ", FileData ID: " + entry.Value[0].fileDataID + ")");
                                    unkFilenames.Add(entry.Key);
                                }
                            }
                        }
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
                    
                    if(args.Length == 2 && File.Exists(args[1]))
                    {
                        target = args[1];
                    }
                    else
                    {
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
                    install = GetInstall("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", args[2]);
                    foreach (var entry in install.entries)
                    {
                        Console.WriteLine(entry.name + " (size: " + entry.size + ", md5: " + BitConverter.ToString(entry.contentHash).Replace("-", string.Empty).ToLower() + ", tags: " + string.Join(",", entry.tags) + ")");
                    }
                    Environment.Exit(0);
                }
                if (args[0] == "extractfilebycontenthash" || args[0] == "extractrawfilebycontenthash")
                {
                    if (args.Length != 6) throw new Exception("Not enough arguments. Need mode, product, buildconfig, cdnconfig, contenthash, outname");

                    var done = false;

                    cdns = GetCDNs(args[1]);

                    args[4] = args[4].ToLower();

                    buildConfig = GetBuildConfig(args[1], Path.Combine(cacheDir, cdns.entries[0].path), args[2]);
                    if (string.IsNullOrWhiteSpace(buildConfig.buildName)) { Console.WriteLine("Invalid buildConfig!"); }

                    encoding = GetEncoding(Path.Combine(cacheDir, cdns.entries[0].path), buildConfig.encoding[1]);

                    string target = "";

                    foreach (var entry in encoding.aEntries)
                    {
                        if (entry.hash.ToLower() == args[4]) { target = entry.key.ToLower(); }
                    }

                    if (string.IsNullOrEmpty(target))
                    {
                        throw new Exception("File not found in encoding!");
                    }

                    var unarchivedName = Path.Combine(cacheDir, cdns.entries[0].path, "data", target[0] + "" + target[1], target[2] + "" + target[3], target);
                    if (File.Exists(unarchivedName))
                    {
                        Console.WriteLine("File " + args[4] + " found as unarchived " + target + "!");
                        File.WriteAllBytes(args[5], ParseBLTEfile(File.ReadAllBytes(unarchivedName)));
                        done = true;
                    }

                    if (!done)
                    {
                        cdnConfig = GetCDNconfig(args[1], Path.Combine(cacheDir, cdns.entries[0].path), args[3]);

                        indexes = GetIndexes(Path.Combine(cacheDir, cdns.entries[0].path), cdnConfig.archives);

                        foreach (var index in indexes)
                        {
                            foreach (var entry in index.archiveIndexEntries)
                            {
                                if (entry.headerHash.ToLower() == target.ToLower())
                                {
                                    var archiveName = Path.Combine(cacheDir, cdns.entries[0].path, "data", index.name[0] + "" + index.name[1], index.name[2] + "" + index.name[3], index.name);
                                    if (!File.Exists(archiveName))
                                    {
                                        throw new FileNotFoundException("Unable to find archive " + index.name + " on disk!");
                                    }

                                    using (BinaryReader bin = new BinaryReader(File.Open(archiveName, FileMode.Open, FileAccess.Read)))
                                    {
                                        bin.BaseStream.Position = entry.offset;
                                        Console.WriteLine("File " + args[4] + " found in web archive as " + target + "!");
                                        if (args[0] == "extractrawfilebycontenthash")
                                        {
                                            Console.WriteLine("Going to write " + args[4] + " to " + unarchivedName);
                                            Directory.CreateDirectory(Path.GetDirectoryName(unarchivedName));
                                            File.WriteAllBytes(unarchivedName, bin.ReadBytes((int)entry.size));
                                        }
                                        else
                                        {
                                            File.WriteAllBytes(args[5], ParseBLTEfile(bin.ReadBytes((int)entry.size)));
                                        }
                                        done = true;
                                    }
                                }
                            }
                        }
                    }

                    if (!done)
                    {
                        // If not found here, file is unarchived. TODO!
                        throw new Exception("Unable to find file in archives. File is not available!?");
                    }

                    Environment.Exit(0);
                }
                if (args[0] == "extractfilesbylist")
                {
                    if (args.Length != 5) throw new Exception("Not enough arguments. Need mode, buildconfig, cdnconfig, basedir, list");

                    buildConfig = GetBuildConfig("wow", Path.Combine(cacheDir, "tpr", "wow"), args[1]);
                    if (string.IsNullOrWhiteSpace(buildConfig.buildName)) { Console.WriteLine("Invalid buildConfig!"); }

                    encoding = GetEncoding(Path.Combine(cacheDir, "tpr", "wow"), buildConfig.encoding[1]);
                    Console.WriteLine(encoding.aEntries.Count());

                    var basedir = args[3];

                    var lines = File.ReadLines(args[4]);

                    cdnConfig = GetCDNconfig("wow", Path.Combine(cacheDir, "tpr", "wow"), args[2]);

                    indexes = GetIndexes(Path.Combine(cacheDir, "tpr", "wow"), cdnConfig.archives);

                    foreach (var line in lines)
                    {
                        var done = false;

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

                        var unarchivedName = Path.Combine(cacheDir, "tpr", "wow", "data", target[0] + "" + target[1], target[2] + "" + target[3], target);
                        if (File.Exists(unarchivedName))
                        {
                            File.WriteAllBytes(Path.Combine(basedir, filename), ParseBLTEfile(File.ReadAllBytes(unarchivedName)));
                            done = true;
                        }

                        if (!done)
                        {
                            foreach (var index in indexes)
                            {
                                foreach (var entry in index.archiveIndexEntries)
                                {
                                    if (entry.headerHash.ToLower() == target.ToLower())
                                    {
                                        var archiveName = Path.Combine(cacheDir, "tpr", "wow", "data", index.name[0] + "" + index.name[1], index.name[2] + "" + index.name[3], index.name);
                                        if (!File.Exists(archiveName))
                                        {
                                            throw new FileNotFoundException("Unable to find archive " + index.name + " on disk!");
                                        }

                                        using (BinaryReader bin = new BinaryReader(File.Open(archiveName, FileMode.Open, FileAccess.Read)))
                                        {
                                            bin.BaseStream.Position = entry.offset;
                                            File.WriteAllBytes(Path.Combine(basedir, filename), ParseBLTEfile(bin.ReadBytes((int)entry.size)));
                                            done = true;
                                            break;
                                        }
                                    }
                                }
                                if (done) break;
                            }
                        }

                        if (!done)
                        {
                            throw new Exception("Unable to find file in archives. File is not available!?");
                        }
                    }

                    Environment.Exit(0);
                }
                if (args[0] == "extractfilesbyfnamelist")
                {
                    if (args.Length != 5) throw new Exception("Not enough arguments. Need mode, buildconfig, cdnconfig, basedir, list");

                    buildConfig = GetBuildConfig("wow", Path.Combine(cacheDir, "tpr", "wow"), args[1]);
                    if (string.IsNullOrWhiteSpace(buildConfig.buildName)) { Console.WriteLine("Invalid buildConfig!"); }

                    encoding = GetEncoding(Path.Combine(cacheDir, "tpr", "wow"), buildConfig.encoding[1]);

                    cdnConfig = GetCDNconfig("wow", Path.Combine(cacheDir, "tpr", "wow"), args[2]);

                    indexes = GetIndexes(Path.Combine(cacheDir, "tpr", "wow"), cdnConfig.archives);

                    var basedir = args[3];

                    var lines = File.ReadLines(args[4]);

                    var rootHash = "";

                    foreach (var entry in encoding.aEntries)
                    {
                        if (entry.hash.ToLower() == buildConfig.root.ToLower()) { rootHash = entry.key.ToLower(); break; }
                    }

                    var hasher = new Jenkins96();

                    var rootList = new Dictionary<ulong, string>();
                    foreach (var line in lines)
                    {
                        var hash = hasher.ComputeHash(line);
                        rootList.Add(hash, line);
                    }

                    Console.WriteLine("Looking up in root..");

                    root = GetRoot(Path.Combine(cacheDir, "tpr", "wow"), rootHash);

                    var encodingList = new Dictionary<string, List<string>>();


                    foreach (var entry in root.entries)
                    {
                        foreach (var subentry in entry.Value)
                        {
                            if (subentry.contentFlags.HasFlag(ContentFlags.LowViolence)) continue;

                            if (!subentry.localeFlags.HasFlag(LocaleFlags.All_WoW) && !subentry.localeFlags.HasFlag(LocaleFlags.enUS))
                            {
                                continue;
                            }

                            if (rootList.ContainsKey(entry.Key))
                            {
                                var cleanContentHash = BitConverter.ToString(subentry.md5).Replace("-", string.Empty).ToLower();

                                if (encodingList.ContainsKey(cleanContentHash))
                                {
                                    encodingList[cleanContentHash].Add(rootList[entry.Key]);
                                }
                                else
                                {
                                    encodingList.Add(cleanContentHash, new List<string>() { rootList[entry.Key] });
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
                            Console.WriteLine(target);
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

                    foreach (var fileEntry in fileList)
                    {
                        var done = false;

                        var target = fileEntry.Key;

                        foreach (var filename in fileEntry.Value)
                        {
                            if (!Directory.Exists(Path.Combine(basedir, Path.GetDirectoryName(filename))))
                            {
                                Directory.CreateDirectory(Path.Combine(basedir, Path.GetDirectoryName(filename)));
                            }
                        }

                        var unarchivedName = Path.Combine(cacheDir, "tpr", "wow", "data", target[0] + "" + target[1], target[2] + "" + target[3], target);
                        if (File.Exists(unarchivedName))
                        {
                            foreach (var filename in fileEntry.Value)
                            {
                                Console.WriteLine(filename);
                                try
                                {
                                    File.WriteAllBytes(Path.Combine(basedir, filename), ParseBLTEfile(File.ReadAllBytes(unarchivedName)));
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.Message);
                                }
                            }
                            done = true;
                        }

                        if (!done)
                        {
                            foreach (var index in indexes)
                            {
                                foreach (var entry in index.archiveIndexEntries)
                                {
                                    if (entry.headerHash.ToLower() == target.ToLower())
                                    {
                                        var archiveName = Path.Combine(cacheDir, "tpr", "wow", "data", index.name[0] + "" + index.name[1], index.name[2] + "" + index.name[3], index.name);
                                        if (!File.Exists(archiveName))
                                        {
                                            throw new FileNotFoundException("Unable to find archive " + index.name + " on disk!");
                                        }

                                        using (BinaryReader bin = new BinaryReader(File.Open(archiveName, FileMode.Open, FileAccess.Read)))
                                        {
                                            foreach (var filename in fileEntry.Value)
                                            {
                                                Console.WriteLine(filename);
                                                bin.BaseStream.Position = entry.offset;
                                                File.WriteAllBytes(Path.Combine(basedir, filename), ParseBLTEfile(bin.ReadBytes((int)entry.size)));
                                            }
                                            done = true;
                                            break;
                                        }
                                    }
                                }
                                if (done) break;
                            }
                        }

                        if (!done)
                        {
                            throw new Exception("Unable to find file in archives. File is not available!?");
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

                    buildConfig = GetBuildConfig(args[1], Path.Combine(cacheDir, cdns.entries[0].path), args[2]);

                    encoding = GetEncoding(Path.Combine(cacheDir, cdns.entries[0].path), buildConfig.encoding[1], 0, true);

                    var encryptedKeys = new Dictionary<string, string>();
                    foreach(var entry in encoding.bEntries)
                    {
                        var stringBlockEntry = encoding.stringBlockEntries[entry.stringIndex];
                        if (stringBlockEntry.Contains("e:"))
                        {
                            encryptedKeys.Add(entry.key, stringBlockEntry);
                        }
                    }

                    string rootKey = "";
                    var encryptedContentHashes = new Dictionary<string, string>();
                    foreach(var entry in encoding.aEntries)
                    {
                        if (encryptedKeys.ContainsKey(entry.key))
                        {
                            encryptedContentHashes.Add(entry.hash, encryptedKeys[entry.key]);
                        }

                        if (entry.hash == buildConfig.root.ToUpper()) { rootKey = entry.key.ToLower(); }
                    }

                    root = GetRoot(Path.Combine(cacheDir, cdns.entries[0].path), rootKey);

                    foreach(var entry in root.entries)
                    {
                        foreach (var subentry in entry.Value)
                        {
                            if (encryptedContentHashes.ContainsKey(BitConverter.ToString(subentry.md5).Replace("-", "")))
                            {
                                var stringBlock = encryptedContentHashes[BitConverter.ToString(subentry.md5).Replace("-", "")];
                                var encryptionKey = stringBlock.Substring(stringBlock.IndexOf("e:{") + 3, 16);
                                Console.WriteLine(subentry.fileDataID + " " + encryptionKey);
                                break;
                            }
                        }
                    }

                    Environment.Exit(0);
                }
                if (args[0] == "dumprawfile")
                {
                    if (args.Length != 2) throw new Exception("Not enough arguments. Need mode, path");
                    Console.Write(Encoding.UTF8.GetString(ParseBLTEfile(File.ReadAllBytes(args[1]))));
                    Environment.Exit(0);
                }
            }

            if (File.Exists("lockfile"))
            {
                Console.WriteLine("Lockfile detected, exiting.");
                Environment.Exit(0);
            }

            File.Create("lockfile");

            // Load programs
            if (checkPrograms == null)
            {
                checkPrograms = new string[] { "agent", "bna", "bnt", "clnt", "d3", "d3cn", "d3t", "demo", "hero", "herot", "hsb", "hst", "pro", "proc", "prot", "prodev", "sc2", "s2", "s2t", "s2b", "test", "storm", "war3", "wow", "wowt", "wow_beta", "s1", "s1t", "s1a", "catalogs", "w3", "w3t" };
            }
            //checkPrograms = new string[] { "wow" };
            backupPrograms = new string[] { "agent", "bna", "pro", "prot", "proc", "wow", "wowt", "wow_beta", "s1", "s1t", "catalogs", "w3", "s1a", "w3t" };

            foreach (string program in checkPrograms)
            {
                Console.WriteLine("Using program " + program);

                versions = GetVersions(program);
                if (versions.entries == null || versions.entries.Count() == 0) { Console.WriteLine("Invalid versions file for " + program + ", skipping!"); continue; }
                Console.WriteLine("Loaded " + versions.entries.Count() + " versions");

                cdns = GetCDNs(program);
                if (cdns.entries == null || cdns.entries.Count() == 0) { Console.WriteLine("Invalid CDNs file for " + program + ", skipping!"); continue; }
                Console.WriteLine("Loaded " + cdns.entries.Count() + " cdns");

                if (!string.IsNullOrEmpty(versions.entries[0].productConfig))
                {
                    Console.WriteLine("Productconfig detected, backing up.");
                    GetCDNFile("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].configPath + "/" + versions.entries[0].productConfig[0] + versions.entries[0].productConfig[1] + "/" + versions.entries[0].productConfig[2] + versions.entries[0].productConfig[3] + "/" + versions.entries[0].productConfig);
                }

                gameblob = GetGameBlob(program);

                if (gameblob.decryptionKeyName != null && gameblob.decryptionKeyName != string.Empty)
                {
                    if (!File.Exists(gameblob.decryptionKeyName + ".ak"))
                    {
                        Console.WriteLine("Decryption key is set and not available on disk, skipping.");
                        isEncrypted = false;
                        continue;
                    }
                    else
                    {
                        isEncrypted = true;
                    }
                }
                else
                {
                    isEncrypted = false;
                }

                if (overrideVersions && !string.IsNullOrEmpty(overrideBuildconfig))
                {
                    buildConfig = GetBuildConfig(program, "http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", overrideBuildconfig);
                }
                else
                {
                    buildConfig = GetBuildConfig(program, "http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", versions.entries[0].buildConfig);
                }

                // Retrieve all buildconfigs
                for(var i = 0; i < versions.entries.Count(); i++)
                {
                    GetBuildConfig(program, "http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", versions.entries[i].buildConfig);
                }

                if (string.IsNullOrWhiteSpace(buildConfig.buildName)) { Console.WriteLine("Invalid buildConfig for " + program + ", setting build name!"); buildConfig.buildName = "UNKNOWN"; }
                Console.WriteLine("BuildConfig for " + buildConfig.buildName + " loaded");


                if (overrideVersions && !string.IsNullOrEmpty(overrideCDNconfig))
                {
                    cdnConfig = GetCDNconfig(program, "http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", overrideCDNconfig);
                }
                else
                {
                    cdnConfig = GetCDNconfig(program, "http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", versions.entries[0].cdnConfig);
                }

                if (cdnConfig.archives == null) { Console.WriteLine("Invalid cdnConfig for " + program + ", skipping!"); continue; }

                if (cdnConfig.builds != null)
                {
                    Console.WriteLine("CDNConfig loaded, " + cdnConfig.builds.Count() + " builds, " + cdnConfig.archives.Count() + " archives");
                    cdnBuildConfigs = new BuildConfigFile[cdnConfig.builds.Count()];
                }
                else
                {
                    Console.WriteLine("CDNConfig loaded, " + cdnConfig.archives.Count() + " archives");
                }

                if (!string.IsNullOrEmpty(versions.entries[0].keyRing)) GetCDNFile("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/" + "config/" + versions.entries[0].keyRing[0] + versions.entries[0].keyRing[1] + "/" + versions.entries[0].keyRing[2] + versions.entries[0].keyRing[3] + "/" + versions.entries[0].keyRing);

                if (!backupPrograms.Contains(program))
                {
                    Console.WriteLine("No need to backup, moving on..");
                    continue;
                }

                Console.Write("Downloading patch files..");
                if (!string.IsNullOrEmpty(buildConfig.patch)) GetCDNFile("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/" + "patch/" + buildConfig.patch[0] + buildConfig.patch[1] + "/" + buildConfig.patch[2] + buildConfig.patch[3] + "/" + buildConfig.patch);
                if (!string.IsNullOrEmpty(buildConfig.patchConfig)) GetCDNFile("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/" + "config/" + buildConfig.patchConfig[0] + buildConfig.patchConfig[1] + "/" + buildConfig.patchConfig[2] + buildConfig.patchConfig[3] + "/" + buildConfig.patchConfig);
                Console.Write("..done\n");

                Console.Write("Loading " + cdnConfig.archives.Count() + " indexes..");
                indexes = GetIndexes("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", cdnConfig.archives);
                Console.Write("..done\n");

                Console.Write("Downloading " + cdnConfig.archives.Count() + " archives..");
                GetArchives("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", cdnConfig.archives);
                Console.Write("..done\n");

                Console.Write("Loading encoding..");

                if (buildConfig.encodingSize == null || buildConfig.encodingSize.Count() < 2)
                {
                    encoding = GetEncoding("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", buildConfig.encoding[1], 0);
                }
                else
                {
                    encoding = GetEncoding("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", buildConfig.encoding[1], int.Parse(buildConfig.encodingSize[1]));
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

                if (program == "wow" || program == "wowt" || program == "wow_beta") // Only these are supported right now
                {
                    Console.Write("Loading root..");
                    if (rootKey == "") { Console.WriteLine("Unable to find root key in encoding!"); } else { root = GetRoot("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", rootKey); }
                    Console.Write("..done\n");

                    Console.Write("Loading download..");
                    if (downloadKey == "") { Console.WriteLine("Unable to find download key in encoding!"); } else { download = GetDownload("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", downloadKey); }
                    Console.Write("..done\n");

                    Console.Write("Loading install..");
                    if (installKey == "") { Console.WriteLine("Unable to find install key in encoding!"); } else { install = GetInstall("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", installKey); }
                    Console.Write("..done\n");
                }

                foreach (var index in indexes)
                {
                    foreach (var entry in index.archiveIndexEntries)
                    {
                        hashes.Remove(entry.headerHash);
                    }
                }

                if (cdnConfig.patchArchives != null)
                {
                    var totalPatchArchives = cdnConfig.patchArchives.Count();
                    for (var i = 0; i < cdnConfig.patchArchives.Count(); i++)
                    {
                        GetCDNFile("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/" + "patch/" + cdnConfig.patchArchives[i][0] + cdnConfig.patchArchives[i][1] + "/" + cdnConfig.patchArchives[i][2] + cdnConfig.patchArchives[i][3] + "/" + cdnConfig.patchArchives[i], false);
                        GetCDNFile("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/" + "patch/" + cdnConfig.patchArchives[i][0] + cdnConfig.patchArchives[i][1] + "/" + cdnConfig.patchArchives[i][2] + cdnConfig.patchArchives[i][3] + "/" + cdnConfig.patchArchives[i] + ".index", false);
                    }
                }

                Console.Write("Downloading " + hashes.Count() + " unarchived files..");

                int h = 1;
                var tot = hashes.Count;

                foreach (var entry in hashes)
                {
                    GetCDNFile("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/" + "data/" + entry.Key[0] + entry.Key[1] + "/" + entry.Key[2] + entry.Key[3] + "/" + entry.Key, false);
                    h++;
                }

                Console.Write("..done\n");

                GC.Collect();
            }

            File.Delete("lockfile");
        }

        private static CDNConfigFile GetCDNconfig(string program, string url, string hash)
        {
            string content;
            var cdnConfig = new CDNConfigFile();

            if (url.StartsWith("http"))
            {
                try
                {
                    content = Encoding.UTF8.GetString(GetCDNFile(url + "/config/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error retrieving CDN config: " + e.Message);
                    return cdnConfig;
                }
            }
            else
            {
                content = File.ReadAllText(Path.Combine(url, "config", "" + hash[0] + hash[1], "" + hash[2] + hash[3], hash));
            }


            var cdnConfigLines = content.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < cdnConfigLines.Count(); i++)
            {
                if (cdnConfigLines[i].StartsWith("# CDN") || cdnConfigLines[i].Length == 0) { continue; }
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
                    default:
                        throw new Exception("Unknown CDNConfig variable '" + cols[0] + "'");
                }
            }

            return cdnConfig;
        }

        private static VersionsFile GetVersions(string program)
        {
            string content;
            var versions = new VersionsFile();

            try
            {
                using (HttpResponseMessage response = httpClient.GetAsync(new Uri(baseUrl + program + "/" + "versions")).Result)
                {
                    using (HttpContent res = response.Content)
                    {
                        content = res.ReadAsStringAsync().Result;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error retrieving versions: " + e.Message);
                return versions;
            }

            var lines = content.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

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
                                throw new Exception("Unknown versions variable '" + friendlyName + "'");
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

            try
            {
                using (HttpResponseMessage response = httpClient.GetAsync(new Uri(baseUrl + program + "/" + "cdns")).Result)
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
                Console.WriteLine("Error retrieving CDNs file: " + e.Message);
                return cdns;
            }

            var lines = content.Split(new string[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

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
                                throw new Exception("Unknown cdns variable '" + friendlyName + "'");
                        }
                    }
                }
            }

            foreach (var cdn in cdns.entries)
            {
                if (cdn.name == "eu")
                {
                    //override cdn to always use eu if present
                    var over = new CdnsFile();
                    over.entries = new CdnsEntry[1];
                    over.entries[0] = cdn;
                    return over;
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
                using (HttpResponseMessage response = httpClient.GetAsync(new Uri(baseUrl + program + "/" + "blob/game")).Result)
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
            dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(content);
            if (json.all.config.decryption_key_name != null)
            {
                gblob.decryptionKeyName = json.all.config.decryption_key_name.Value;
            }
            return gblob;
        }

        private static BuildConfigFile GetBuildConfig(string program, string url, string hash)
        {
            string content;

            var buildConfig = new BuildConfigFile();

            if (url.StartsWith("http"))
            {
                try
                {
                    content = Encoding.UTF8.GetString(GetCDNFile(url + "/config/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error retrieving build config: " + e.Message);
                    return buildConfig;
                }
            }
            else
            {
                content = File.ReadAllText(Path.Combine(url, "config", "" + hash[0] + hash[1], "" + hash[2] + hash[3], hash));
            }

            if (string.IsNullOrEmpty(content) || !content.StartsWith("# Build"))
            {
                Console.WriteLine("Error reading build config!");
                return buildConfig;
            }

            var lines = content.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < lines.Count(); i++)
            {
                if (lines[i].StartsWith("# Build") || lines[i].Length == 0) { continue; }
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
                        throw new Exception("Unknown BuildConfig variable '" + cols[0] + "'");
                }
            }

            return buildConfig;
        }

        private static ArchiveIndex[] GetIndexes(string url, string[] archives)
        {
            var indexes = new ArchiveIndex[archives.Count()];
            for (int i = 0; i < archives.Count(); i++)
            {
                indexes[i].name = archives[i];

                byte[] indexContent;
                if (url.StartsWith("http"))
                {
                    indexContent = GetCDNFile(url + "data/" + archives[i][0] + archives[i][1] + "/" + archives[i][2] + archives[i][3] + "/" + archives[i] + ".index");
                }
                else
                {
                    indexContent = File.ReadAllBytes(Path.Combine(url, "data", "" + archives[i][0] + archives[i][1], "" + archives[i][2] + archives[i][3], archives[i] + ".index"));
                }

                using (BinaryReader bin = new BinaryReader(new MemoryStream(indexContent)))
                {
                    int indexEntries = indexContent.Length / 4096;

                    var entries = new List<ArchiveIndexEntry>();

                    for (int b = 0; b < indexEntries; b++)
                    {
                        for (int bi = 0; bi < 170; bi++)
                        {
                            var entry = new ArchiveIndexEntry()
                            {
                                headerHash = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", ""),
                                size = bin.ReadUInt32(true),
                                offset = bin.ReadUInt32(true)
                            };
                            //Console.WriteLine(entry.headerHash + " " + entry.size + " " + entry.offset);
                            entries.Add(entry);
                        }
                        bin.ReadBytes(16);
                    }

                    indexes[i].archiveIndexEntries = entries.ToArray();
                }

            }
            return indexes;
        }

        private static void GetArchives(string url, string[] archives)
        {
            var indexes = new ArchiveIndex[archives.Count()];
            for (int i = 0; i < archives.Count(); i++)
            {
                indexes[i].name = archives[i];
                string name = url + "data/" + archives[i][0] + archives[i][1] + "/" + archives[i][2] + archives[i][3] + "/" + archives[i];
                string cleanname = name.Replace("http://" + cdns.entries[0].hosts[0], "");

                if (!File.Exists(cacheDir + cleanname)) // Check if already downloaded
                {
                    Console.WriteLine("Downloading archive " + cleanname);
                    using (HttpResponseMessage response = httpClient.GetAsync(new Uri(name)).Result)
                    {
                        using (MemoryStream mstream = new MemoryStream())
                        using (HttpContent res = response.Content)
                        {
                            res.CopyToAsync(mstream);
                            if (isEncrypted)
                            {
                                File.WriteAllBytes(cacheDir + cleanname, DecryptFile(cleanname.Substring(cleanname.Length - 32), mstream.ToArray()));
                            }
                            else
                            {
                                File.WriteAllBytes(cacheDir + cleanname, mstream.ToArray());
                            }
                        }
                    }
                }
                //else
                //{
                //    var MyClient = WebRequest.Create(name) as HttpWebRequest;
                //    MyClient.Method = WebRequestMethods.Http.Get;
                //    var response = MyClient.GetResponse() as HttpWebResponse;
                //    if (response.Headers["Content-Length"] != new FileInfo(cacheDir + cleanname).Length.ToString())
                //    {
                //        Console.WriteLine("!!! Archive " + cleanname + " is incomplete or has been deleted from CDN. " + response.Headers["Content-Length"] + " vs " + new FileInfo(cacheDir + cleanname).Length.ToString() + ". Attempting redownload!");
                //        using (var webClient = new System.Net.WebClient())
                //        {
                //            //byte[] file;

                //            try
                //            {
                //                webClient.DownloadFile(new Uri(name), cacheDir + cleanname);
                //                // file = webClient.DownloadData(new Uri(name));
                //                // if (file != null) File.WriteAllBytes(cacheDir + cleanname, file);
                //            }
                //            catch (WebException e)
                //            {
                //                Console.WriteLine(e.Message);
                //            }
                //        }
                //    }
                //    MyClient.Abort();

                //}
            }
        }

        private static RootFile GetRoot(string url, string hash)
        {
            var root = new RootFile();
            root.entries = new MultiDictionary<ulong, RootEntry>();

            byte[] content;

            if (url.StartsWith("http:"))
            {
                content = GetCDNFile(url + "data/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash);
            }
            else
            {
                content = File.ReadAllBytes(Path.Combine(url, "data", "" + hash[0] + hash[1], "" + hash[2] + hash[3], hash));
            }

            using (var ms = new MemoryStream(ParseBLTEfile(content)))
            using (var bin = new BinaryReader(ms))
            {
                while (bin.BaseStream.Position < bin.BaseStream.Length)
                {
                    var count = bin.ReadUInt32();
                    var contentFlags = (ContentFlags)bin.ReadUInt32();
                    var localeFlags = (LocaleFlags)bin.ReadUInt32();

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

                    for (var i = 0; i < count; ++i)
                    {
                        entries[i].md5 = bin.ReadBytes(16);
                        entries[i].lookup = bin.ReadUInt64();
                        root.entries.Add(entries[i].lookup, entries[i]);
                    }
                }
            }

            return root;
        }

        private static DownloadFile GetDownload(string url, string hash)
        {
            var download = new DownloadFile();

            byte[] content;
            content = GetCDNFile(url + "data/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash);
            byte[] parsedContent = ParseBLTEfile(content);

            using (BinaryReader bin = new BinaryReader(new MemoryStream(parsedContent)))
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

        private static InstallFile GetInstall(string url, string hash)
        {
            var install = new InstallFile();

            byte[] content = GetCDNFile(url + "data/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash);

            using (BinaryReader bin = new BinaryReader(new MemoryStream(ParseBLTEfile(content))))
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

            if (url.StartsWith("http:"))
            {
                content = GetCDNFile(url + "data/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash);

                if (encodingSize != content.Length)
                {
                    content = GetCDNFile(url + "data/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash, true);

                    if (encodingSize != content.Length && encodingSize != 0)
                    {
                        throw new Exception("File corrupt/not fully downloaded! Remove " + "data / " + hash[0] + hash[1] + " / " + hash[2] + hash[3] + " / " + hash + " from cache.");
                    }
                }
            }
            else
            {
                content = File.ReadAllBytes(Path.Combine(url, "data", "" + hash[0] + hash[1], "" + hash[2] + hash[3], hash));
            }

            byte[] parsedContent = ParseBLTEfile(content);
            using (BinaryReader bin = new BinaryReader(new MemoryStream(parsedContent)))
            {
                if (Encoding.UTF8.GetString(bin.ReadBytes(2)) != "EN") { throw new Exception("Error while parsing encoding file. Did BLTE header size change?"); }
                encoding.unk1 = bin.ReadByte();
                encoding.checksumSizeA = bin.ReadByte();
                encoding.checksumSizeB = bin.ReadByte();
                encoding.flagsA = bin.ReadUInt16();
                encoding.flagsB = bin.ReadUInt16();
                encoding.numEntriesA = bin.ReadUInt32(true);
                encoding.numEntriesB = bin.ReadUInt32(true);
                encoding.stringBlockSize = bin.ReadUInt40(true);

                var headerLength = bin.BaseStream.Position;

                var stringBlockEntries = new List<string>();

                while((bin.BaseStream.Position - headerLength) != (long)encoding.stringBlockSize)
                {
                    stringBlockEntries.Add(bin.ReadCString());
                }

                encoding.stringBlockEntries = stringBlockEntries.ToArray();

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

                List<EncodingFileDescEntry> b_entries = new List<EncodingFileDescEntry>();

                while (bin.BaseStream.Position < tableBstart + 4096 * encoding.numEntriesB)
                {
                    var remaining = 4096 - (bin.BaseStream.Position - tableBstart) % 4096;

                    if (remaining < 25)
                    {
                        bin.BaseStream.Position += remaining;
                        continue;
                    }

                    EncodingFileDescEntry entry = new EncodingFileDescEntry()
                    {
                        key = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", ""),
                        stringIndex = bin.ReadUInt32(true),
                        compressedSize = bin.ReadUInt40(true)
                    };

                    if(entry.stringIndex == uint.MaxValue) break;

                    b_entries.Add(entry);
                }

                encoding.bEntries = b_entries.ToArray();
            }

            return encoding;
        }

        private static byte[] ParseBLTEfile(byte[] content)
        {
            MemoryStream result = new MemoryStream();

            using (BinaryReader bin = new BinaryReader(new MemoryStream(content)))
            {
                if (bin.ReadUInt32() != 0x45544c42) { throw new Exception("Not a BLTE file"); }

                var blteSize = bin.ReadUInt32(true);

                BLTEChunkInfo[] chunkInfos;

                if (blteSize == 0)
                {
                    chunkInfos = new BLTEChunkInfo[1];
                    chunkInfos[0].isFullChunk = false;
                    chunkInfos[0].inFileSize = Convert.ToInt32(bin.BaseStream.Length - bin.BaseStream.Position);
                    chunkInfos[0].actualSize = Convert.ToInt32(bin.BaseStream.Length - bin.BaseStream.Position);
                    chunkInfos[0].checkSum = new byte[16]; ;
                }
                else
                {

                    var bytes = bin.ReadBytes(4);

                    var chunkCount = bytes[1] << 16 | bytes[2] << 8 | bytes[3] << 0;

                    //var unk = bin.ReadByte();

                    ////Code by TOM_RUS 
                    //byte v1 = bin.ReadByte();
                    //byte v2 = bin.ReadByte();
                    //byte v3 = bin.ReadByte();
                    //var chunkCount = v1 << 16 | v2 << 8 | v3 << 0; // 3-byte
                    ////Retrieved from https://github.com/WoW-Tools/CASCExplorer/blob/cli/CascLib/BLTEHandler.cs#L76

                    var supposedHeaderSize = 24 * chunkCount + 12;

                    if (supposedHeaderSize != blteSize)
                    {
                        throw new Exception("Invalid header size!");
                    }

                    if (supposedHeaderSize > bin.BaseStream.Length)
                    {
                        throw new Exception("Not enough data");
                    }

                    chunkInfos = new BLTEChunkInfo[chunkCount];

                    for (int i = 0; i < chunkCount; i++)
                    {
                        chunkInfos[i].isFullChunk = true;
                        chunkInfos[i].inFileSize = bin.ReadInt32(true);
                        chunkInfos[i].actualSize = bin.ReadInt32(true);
                        chunkInfos[i].checkSum = new byte[16];
                        chunkInfos[i].checkSum = bin.ReadBytes(16);
                    }
                }

                for (var index = 0; index < chunkInfos.Count(); index++)
                {
                    var chunk = chunkInfos[index];

                    MemoryStream chunkResult = new MemoryStream();

                    if (chunk.inFileSize > bin.BaseStream.Length)
                    {
                        throw new Exception("Trying to read more than is available!");
                    }

                    var chunkBuffer = bin.ReadBytes(chunk.inFileSize);

                    var hasher = MD5.Create();
                    var md5sum = hasher.ComputeHash(chunkBuffer);

                    if (chunk.isFullChunk && BitConverter.ToString(md5sum) != BitConverter.ToString(chunk.checkSum))
                    {
                        throw new Exception("MD5 checksum mismatch on BLTE chunk! Sum is " + BitConverter.ToString(md5sum).Replace("-", "") + " but is supposed to be " + BitConverter.ToString(chunk.checkSum).Replace("-", ""));
                    }

                    HandleDataBlock(chunkBuffer, index, chunk, chunkResult);

                    var chunkres = chunkResult.ToArray();
                    if (chunk.isFullChunk && chunkres.Length != chunk.actualSize)
                    {
                        throw new Exception("Decoded result is wrong size!");
                    }

                    result.Write(chunkres, 0, chunkres.Length);
                }

                foreach (var chunk in chunkInfos)
                {
                    if (chunk.inFileSize > bin.BaseStream.Length)
                    {
                        throw new Exception("Trying to read more than is available!");
                    }
                    else
                    {
                        bin.BaseStream.Position += chunk.inFileSize;
                    }
                }
            }

            return result.ToArray();
        }

        private static void HandleDataBlock(byte[] chunkBuffer, int index, BLTEChunkInfo chunk, MemoryStream chunkResult)
        {
            using (BinaryReader chunkreader = new BinaryReader(new MemoryStream(chunkBuffer)))
            {
                var mode = chunkreader.ReadChar();
                switch (mode)
                {
                    case 'N': // none
                        chunkResult.Write(chunkreader.ReadBytes(chunk.actualSize), 0, chunk.actualSize); //read actual size because we already read the N from chunkreader
                        break;
                    case 'Z': // zlib
                        using (MemoryStream stream = new MemoryStream(chunkreader.ReadBytes(chunk.inFileSize - 1), 2, chunk.inFileSize - 3))
                        {
                            var ds = new DeflateStream(stream, CompressionMode.Decompress);
                            ds.CopyTo(chunkResult);
                        }
                        break;
                    case 'E': // encrypted
                        byte[] decrypted = Decrypt(chunkBuffer, index);
                        Console.WriteLine("File is encrypted with key " + ReturnEncryptionKeyName(chunkreader.ReadBytes(chunk.inFileSize)));

                        // Override inFileSize with decrypted length because it now differs from original encrypted chunk.inFileSize which breaks decompression
                        chunk.inFileSize = decrypted.Length;

                        HandleDataBlock(decrypted, index, chunk, chunkResult);
                        break;
                    case 'F': // frame
                    default:
                        throw new Exception("Unsupported mode!");
                }
            }
        }

        private static string ReturnEncryptionKeyName(byte[] data)
        {
            byte keyNameSize = data[0];

            if (keyNameSize == 0 || keyNameSize != 8)
            {
                Console.WriteLine(keyNameSize.ToString());
                throw new Exception("keyNameSize == 0 || keyNameSize != 8");
            }

            byte[] keyNameBytes = new byte[keyNameSize];
            Array.Copy(data, 1, keyNameBytes, 0, keyNameSize);

            Array.Reverse(keyNameBytes);

            return BitConverter.ToString(keyNameBytes).Replace("-", "");
        }
        private static byte[] Decrypt(byte[] data, int index)
        {
            byte keyNameSize = data[1];

            if (keyNameSize == 0 || keyNameSize != 8)
                throw new Exception("keyNameSize == 0 || keyNameSize != 8");

            byte[] keyNameBytes = new byte[keyNameSize];
            Array.Copy(data, 2, keyNameBytes, 0, keyNameSize);

            ulong keyName = BitConverter.ToUInt64(keyNameBytes, 0);

            byte IVSize = data[keyNameSize + 2];

            if (IVSize != 4 || IVSize > 0x10)
                throw new Exception("IVSize != 4 || IVSize > 0x10");

            byte[] IVpart = new byte[IVSize];
            Array.Copy(data, keyNameSize + 3, IVpart, 0, IVSize);

            if (data.Length < IVSize + keyNameSize + 4)
                throw new Exception("data.Length < IVSize + keyNameSize + 4");

            int dataOffset = keyNameSize + IVSize + 3;

            byte encType = data[dataOffset];

            if (encType != 'S' && encType != 'A') // 'S' or 'A'
                throw new Exception("encType != ENCRYPTION_SALSA20 && encType != ENCRYPTION_ARC4");

            dataOffset++;

            // expand to 8 bytes
            byte[] IV = new byte[8];
            Array.Copy(IVpart, IV, IVpart.Length);

            // magic
            for (int shift = 0, i = 0; i < sizeof(int); shift += 8, i++)
            {
                IV[i] ^= (byte)((index >> shift) & 0xFF);
            }

            byte[] key = KeyService.GetKey(keyName);

            if (key == null)
                throw new Exception("Unknown keyname " + keyName.ToString("X16"));

            if (encType == 'S')
            {
                ICryptoTransform decryptor = KeyService.SalsaInstance.CreateDecryptor(key, IV);

                return decryptor.TransformFinalBlock(data, dataOffset, data.Length - dataOffset);
            }
            else
            {
                // ARC4 ?
                throw new Exception("encType ENCRYPTION_ARC4 not implemented");
            }
        }

        static byte[] DecryptFile(string name, byte[] data)
        {
            byte[] key = new byte[16];

            using (BinaryReader reader = new BinaryReader(new FileStream(gameblob.decryptionKeyName + ".ak", FileMode.Open)))
            {
                key = reader.ReadBytes(16);
            }

            byte[] IV = name.ToByteArray();

            Array.Copy(IV, 8, IV, 0, 8);
            Array.Resize(ref IV, 8);

            ICryptoTransform decryptor = SalsaInstance.CreateDecryptor(key, IV);

            return decryptor.TransformFinalBlock(data, 0, data.Length);
        }

        public static byte[] GetCDNFile(string url, bool returnstream = true, bool redownload = false)
        {
            url = url.ToLower();

            string cleanname = url.Replace("http://" + cdns.entries[0].hosts[0], "");

            if (redownload || !File.Exists(cacheDir + cleanname))
            {
                try
                {
                    if (!Directory.Exists(cacheDir + cleanname)) { Directory.CreateDirectory(Path.GetDirectoryName(cacheDir + cleanname)); }
                    using (HttpResponseMessage response = httpClient.GetAsync(url).Result)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            using (MemoryStream mstream = new MemoryStream())
                            using (HttpContent res = response.Content)
                            {
                                res.CopyToAsync(mstream);
                                
                                if (isEncrypted)
                                {
                                    var cleaned = Path.GetFileNameWithoutExtension(cleanname);
                                    var decrypted = DecryptFile(cleaned, mstream.ToArray());

                                    File.WriteAllBytes(cacheDir + cleanname, decrypted);
                                    return decrypted;
                                }
                                else
                                {
                                    File.WriteAllBytes(cacheDir + cleanname, mstream.ToArray());
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("Error retrieving file: HTTP status code " + response.StatusCode);
                        }

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            if (returnstream)
            {
                return File.ReadAllBytes(cacheDir + cleanname);
            }
            else
            {
                return new byte[0];
            }
        }
    }
}
