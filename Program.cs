using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
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
        private static BuildConfigFile buildConfig;
        private static BuildConfigFile[] cdnBuildConfigs;
        private static CDNConfigFile cdnConfig;
        private static ArchiveIndex[] indexes;
        private static EncodingFile encoding;
        private static InstallFile install;
        private static DownloadFile download;
        private static RootFile root;

        static void Main(string[] args)
        {
            cacheDir = ConfigurationManager.AppSettings["cachedir"];

            // Check if cache/backup directory exists
            if (!Directory.Exists(cacheDir)) { Directory.CreateDirectory(cacheDir); }

            if (args.Length > 0)
            {
                if(args[0] == "missingfiles")
                {
                    if (args.Length != 3) throw new Exception("Not enough arguments. Need mode, buildconfig, cdnconfig");

                    buildConfig = GetBuildConfig("wow", Path.Combine(cacheDir, "tpr", "wow"), args[1]);
                    if (string.IsNullOrWhiteSpace(buildConfig.buildName)) { Console.WriteLine("Invalid buildConfig!"); }

                    cdnConfig = GetCDNconfig("wow", Path.Combine(cacheDir, "tpr", "wow"), args[2]);
                    if (cdnConfig.archives == null) { Console.WriteLine("Invalid cdnConfig"); }

                    encoding = GetEncoding(Path.Combine(cacheDir, "tpr", "wow"), buildConfig.encoding[1]);

                    Dictionary<string, string> hashes = new Dictionary<string, string>();

                    foreach (var entry in encoding.entries)
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
                    Environment.Exit(1);
                }
                if(args[0] == "dumpinfo")
                {
                    if (args.Length != 3) throw new Exception("Not enough arguments. Need mode, buildconfig, cdnconfig");
                    buildConfig = GetBuildConfig("wow", Path.Combine(cacheDir, "tpr", "wow"), args[1]);
                    if (string.IsNullOrWhiteSpace(buildConfig.buildName)) { Console.WriteLine("Invalid buildConfig!"); }

                    cdnConfig = GetCDNconfig("wow", Path.Combine(cacheDir, "tpr", "wow"), args[2]);
                    if (cdnConfig.archives == null) { Console.WriteLine("Invalid cdnConfig"); }

                    encoding = GetEncoding(Path.Combine(cacheDir, "tpr", "wow"), buildConfig.encoding[1]);

                    string rootKey = "";
                    string downloadKey = "";
                    string installKey = "";

                    Dictionary<string, string> hashes = new Dictionary<string, string>();

                    foreach (var entry in encoding.entries)
                    {
                        if (entry.hash == buildConfig.root.ToUpper()) { rootKey = entry.key; Console.WriteLine("root = " + entry.key.ToLower()); }
                        if (entry.hash == buildConfig.download.ToUpper()) { downloadKey = entry.key; Console.WriteLine("download = " + entry.key.ToLower()); }
                        if (entry.hash == buildConfig.install.ToUpper()) { installKey = entry.key; Console.WriteLine("install = " + entry.key.ToLower()); }
                        if (!hashes.ContainsKey(entry.key)) { hashes.Add(entry.key, entry.hash); }
                    }

                    indexes = GetIndexes(Path.Combine(cacheDir, "tpr", "wow"), cdnConfig.archives);

                    foreach (var index in indexes)
                    {
                        Console.WriteLine("Checking " + index.name + " " + index.archiveIndexEntries.Count() + " entries");
                        foreach (var entry in index.archiveIndexEntries)
                        {
                            hashes.Remove(entry.headerHash);
                            Console.WriteLine("Removing " + entry.headerHash.ToLower() + " from list");
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
                if(args[0] == "dumproot")
                {
                    if (args.Length != 3) throw new Exception("Not enough arguments. Need mode, buildconfig, cdnconfig");
                    buildConfig = GetBuildConfig("wow", Path.Combine(cacheDir, "tpr", "wow"), args[1]);
                    if (string.IsNullOrWhiteSpace(buildConfig.buildName)) { Console.WriteLine("Invalid buildConfig!"); }

                    encoding = GetEncoding(Path.Combine(cacheDir, "tpr", "wow"), buildConfig.encoding[1]);

                    string rootKey = "";

                    foreach (var entry in encoding.entries)
                    {
                        if (entry.hash == buildConfig.root.ToUpper()) { rootKey = entry.key; }
                    }

                    cdns = GetCDNs("wow");

                    var root = GetRoot("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", rootKey);
                    foreach(var entry in root.entries)
                    {
                        Console.WriteLine(entry.Key + " => " + BitConverter.ToString(entry.Value[0].md5).Replace("-", "").ToLower());
                    }

                    Environment.Exit(1);
                }

                if(args[0] == "diffroot")
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

                    foreach (var entry in root2.entries)
                    {
                        if (!root1.entries.ContainsKey(entry.Key))
                        {
                            // Added
                            if (fileNames.ContainsKey(entry.Key))
                            {
                                Console.WriteLine("[ADDED] " + fileNames[entry.Key]);
                            }
                            else
                            {
                                Console.WriteLine("[ADDED] Unknown filename: " + entry.Key.ToString("x").PadLeft(16, '0'));
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
                                Console.WriteLine("[REMOVED] " + fileNames[entry.Key]);
                            }
                            else
                            {
                                Console.WriteLine("[REMOVED] Unknown filename: " + entry.Key.ToString("x").PadLeft(16, '0'));
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
                                    Console.WriteLine("[MODIFIED] " + fileNames[entry.Key]);
                                }
                                else
                                {
                                    Console.WriteLine("[MODIFIED] Unknown filename: " + entry.Key.ToString("x").PadLeft(16, '0'));
                                }
                            }
                        }
                    }
                    Environment.Exit(1);
                }
            }

            // Load programs
            //checkPrograms = ConfigurationManager.AppSettings["checkprograms"].Split(',');
            checkPrograms = new string[] { "wowt" };
            backupPrograms = ConfigurationManager.AppSettings["backupprograms"].Split(',');

            foreach (string program in checkPrograms)
            {
                Console.WriteLine("Using program " + program);

                versions = GetVersions(program);
                if (versions.entries == null || versions.entries.Count() == 0) { Console.WriteLine("Invalid versions file for " + program + ", skipping!"); continue; }
                Console.WriteLine("Loaded " + versions.entries.Count() + " versions");

                cdns = GetCDNs(program);
                if (cdns.entries == null || cdns.entries.Count() == 0) { Console.WriteLine("Invalid CDNs file for " + program + ", skipping!"); continue; }
                Console.WriteLine("Loaded " + cdns.entries.Count() + " cdns");

                buildConfig = GetBuildConfig(program, "http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", versions.entries[0].buildConfig);
                if (string.IsNullOrWhiteSpace(buildConfig.buildName)) { Console.WriteLine("Invalid buildConfig for " + program + ", skipping!"); continue; }
                Console.WriteLine("BuildConfig for " + buildConfig.buildName + " loaded");

                cdnConfig = GetCDNconfig(program, "http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", versions.entries[0].cdnConfig);
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

                foreach (var entry in encoding.entries)
                {
                    if (entry.hash == buildConfig.root.ToUpper()) { rootKey = entry.key.ToLower(); }
                    if (entry.hash == buildConfig.download.ToUpper()) { downloadKey = entry.key.ToLower(); }
                    if (entry.hash == buildConfig.install.ToUpper()) { installKey = entry.key.ToLower(); }
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
                        Console.WriteLine("[" + (i + 1) + "/" + totalPatchArchives + "] Downloading patch archive " + cdnConfig.patchArchives[i]);
                        GetCDNFile("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/" + "patch/" + cdnConfig.patchArchives[i][0] + cdnConfig.patchArchives[i][1] + "/" + cdnConfig.patchArchives[i][2] + cdnConfig.patchArchives[i][3] + "/" + cdnConfig.patchArchives[i], false);
                        GetCDNFile("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/" + "patch/" + cdnConfig.patchArchives[i][0] + cdnConfig.patchArchives[i][1] + "/" + cdnConfig.patchArchives[i][2] + cdnConfig.patchArchives[i][3] + "/" + cdnConfig.patchArchives[i] + ".index", false);
                    }
                }

                Console.WriteLine("Downloading " + hashes.Count() + " unarchived files..");

                int h = 1;
                var tot = hashes.Count;

                foreach (var entry in hashes)
                {
                    Console.WriteLine("[" + h + "/" + tot + "] Downloading " + entry.Key.ToLower());
                    GetCDNFile("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/" + "data/" + entry.Key[0] + entry.Key[1] + "/" + entry.Key[2] + entry.Key[3] + "/" + entry.Key, false);
                    h++;
                }

                Console.WriteLine("Done downloading unarchived files.");

                GC.Collect();
            }
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

            using (var webClient = new WebClient())
            {
                try
                {
                    content = webClient.DownloadString(new Uri(baseUrl + program + "/" + "versions"));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error retrieving versions: " + e.Message);
                    return versions;
                }
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
                                throw new Exception("Unknown BuildConfig variable '" + friendlyName + "'");
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

            using (var webClient = new System.Net.WebClient())
            {
                try
                {
                    content = webClient.DownloadString(new Uri(baseUrl + program + "/" + "cdns"));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error downloading CDNs file" + e.Message);
                    return cdns;
                }
            }

            var lines = content.Split(new string[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Count() > 0)
            {
                cdns.entries = new CdnsEntry[lines.Count() - 1];
                for (var i = 0; i < lines.Count(); i++)
                {
                    if (lines[i].StartsWith("Name!")) { continue; }
                    var cols = lines[i].Split('|');
                    cdns.entries[i - 1].name = cols[0];
                    cdns.entries[i - 1].path = cols[1];
                    var hosts = cols[2].Split(' ');
                    cdns.entries[i - 1].hosts = new string[hosts.Count()];
                    for (var h = 0; h < hosts.Count(); h++)
                    {
                        cdns.entries[i - 1].hosts[h] = hosts[h];
                    }
                }
            }

            return cdns;
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
                        buildConfig.download = cols[1];
                        break;
                    case "install":
                        buildConfig.install = cols[1];
                        break;
                    case "encoding":
                        var encoding = cols[1].Split(' ');
                        buildConfig.encoding = encoding;
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
                        buildConfig.installSize = cols[1];
                        break;
                    case "download-size":
                        buildConfig.downloadSize = cols[1];
                        break;
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

                using (var webClient = new System.Net.WebClient())
                {
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
                    using (var webClient = new System.Net.WebClient())
                    {
                        webClient.DownloadFileAsync(new Uri(name), cacheDir + cleanname);

                        Console.Write("\n");
                        webClient.DownloadProgressChanged += (s, e) =>
                        {
                            Console.Write("\r [" + (i + 1) + "/" + archives.Count() + "] " + e.ProgressPercentage + "% for archive " + archives[i]);
                        };

                        while (webClient.IsBusy)
                        {
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

            using (var webClient = new WebClient())
            {
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
            }

            return root;
        }

        private static DownloadFile GetDownload(string url, string hash)
        {
            var download = new DownloadFile();

            using (var webClient = new WebClient())
            {
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
                    install.tags[i].files = bin.ReadBytes(bytesPerTag);
                }

                install.entries = new InstallFileEntry[install.numEntries];

                for (var i = 0; i < install.numEntries; i++)
                {
                    install.entries[i].name = bin.ReadCString();
                    install.entries[i].contentHash = bin.ReadBytes(install.hashSize);
                    install.entries[i].size = bin.ReadUInt32(true);
                }
            }

            return install;
        }

        private static EncodingFile GetEncoding(string url, string hash, int encodingSize = 0)
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
                encoding.unk2 = bin.ReadByte();

                encoding.stringBlockSize = bin.ReadInt32(true);

                bin.ReadBytes(encoding.stringBlockSize);

                encoding.headers = new EncodingHeaderEntry[encoding.numEntriesA];

                for (int i = 0; i < encoding.numEntriesA; i++)
                {
                    encoding.headers[i].firstHash = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");
                    encoding.headers[i].checksum = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");
                }

                long chunkStart = bin.BaseStream.Position;

                encoding.entries = new EncodingFileEntry[encoding.numEntriesA];
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

                        for (int key = 0; key < entry.keyCount - 1; key++)
                        {
                            bin.ReadBytes(16);
                        }

                        entries.Add(entry);
                    }

                    long remaining = 4096 - ((bin.BaseStream.Position - chunkStart) % 4096);
                    if (remaining > 0) { bin.BaseStream.Position += remaining; }
                }

                encoding.entries = entries.ToArray();
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

                foreach (var chunk in chunkInfos)
                {
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
                        // throw new Exception("MD5 checksum mismatch on BLTE chunk! Sum is " + BitConverter.ToString(md5sum).Replace("-", "") + " but is supposed to be " + BitConverter.ToString(chunk.checkSum).Replace("-", ""));
                    }

                    using (BinaryReader chunkreader = new BinaryReader(new MemoryStream(chunkBuffer)))
                    {
                        var mode = chunkreader.ReadChar();
                        switch (mode)
                        {
                            case 'N': // none
                                chunkResult.Write(chunkreader.ReadBytes(chunk.actualSize), 0, chunk.actualSize); //read actual size because we already read the N from chunkreader
                                break;
                            case 'Z': // zlib, todo
                                using (MemoryStream stream = new MemoryStream(chunkreader.ReadBytes(chunk.inFileSize - 1), 2, chunk.inFileSize - 3))
                                {
                                    var ds = new DeflateStream(stream, CompressionMode.Decompress);
                                    ds.CopyTo(chunkResult);
                                }
                                break;
                            case 'F': // frame
                            case 'E': // encrypted
                                Console.WriteLine("Encrypted file!");
                                break;
                            default:
                                throw new Exception("Unsupported mode!");
                        }
                    }

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

        public static byte[] GetCDNFile(string url, bool returnstream = true, bool redownload = false)
        {
            url = url.ToLower();

            string cleanname = url.Replace("http://" + cdns.entries[0].hosts[0], "");

            if (redownload || !File.Exists(cacheDir + cleanname))
            {
                using (var webClient = new WebClient())
                {
                    try
                    {
                        if (!Directory.Exists(cacheDir + cleanname)) { Directory.CreateDirectory(Path.GetDirectoryName(cacheDir + cleanname)); }
                        webClient.DownloadFile(url, cacheDir + cleanname);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
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
