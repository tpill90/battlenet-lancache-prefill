using System;
using System.Collections.Generic;
using System.Configuration;
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

        private static versionsFile versions;
        private static cdnsFile cdns;
        private static buildConfigFile buildConfig;
        private static buildConfigFile[] cdnBuildConfigs;
        private static cdnConfigFile cdnConfig;
        private static archiveIndex[] indexes;
        private static encodingFile encoding;
        private static installFile install;
        private static downloadFile download;

        static void Main(string[] args)
        {
            cacheDir = ConfigurationManager.AppSettings["cachedir"];

            // Check if cache/backup directory exists
            if (!Directory.Exists(cacheDir)) { Directory.CreateDirectory(cacheDir); }

            // Load programs
            checkPrograms = ConfigurationManager.AppSettings["checkprograms"].Split(',');
            backupPrograms = ConfigurationManager.AppSettings["backupprograms"].Split(',');

            foreach (string program in checkPrograms)
            {
                Console.WriteLine("Using program " + program);

                versions = getVersions(program);
                if (versions.entries == null || versions.entries.Count() == 0) { Console.WriteLine("Invalid versions file for " + program + ", skipping!"); continue; }
                Console.WriteLine("Loaded " + versions.entries.Count() + " versions");

                cdns = getCDNs(program);
                if(cdns.entries == null || cdns.entries.Count() == 0){ Console.WriteLine("Invalid CDNs file for " + program + ", skipping!"); continue; }
                Console.WriteLine("Loaded " + cdns.entries.Count() + " cdns");

                buildConfig = getBuildConfig(program, "http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", versions.entries[0].buildConfig);
                if (string.IsNullOrWhiteSpace(buildConfig.buildName)) { Console.WriteLine("Invalid buildConfig for " + program + ", skipping!"); continue; }
                Console.WriteLine("BuildConfig for " + buildConfig.buildName + " loaded");

                cdnConfig = getCDNconfig(program, "http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", versions.entries[0].cdnConfig);
                if(cdnConfig.archives == null) { Console.WriteLine("Invalid cdnConfig for " + program + ", skipping!"); continue; }

                if (cdnConfig.builds != null)
                {
                    Console.WriteLine("CDNConfig loaded, " + cdnConfig.builds.Count() + " builds, " + cdnConfig.archives.Count() + " archives");
                    cdnBuildConfigs = new buildConfigFile[cdnConfig.builds.Count()];
                }
                else
                {
                    Console.WriteLine("CDNConfig loaded, " + cdnConfig.archives.Count() + " archives");
                }

                if (!backupPrograms.Contains(program))
                {
                    Console.WriteLine("No need to backup, moving on..");
                    continue;
                }

                var allBuilds = false; // Whether or not to grab other builds mentioned in cdnconfig, adds a few min to execution if it has to DL everything fresh.

                Dictionary<string, string> hashes = new Dictionary<string, string>();

                if (allBuilds == true && cdnConfig.builds != null)
                {
                    for (var i = 0; i < cdnConfig.builds.Count(); i++)
                    {
                        cdnBuildConfigs[i] = getBuildConfig(program, "http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", cdnConfig.builds[i]);

                        Console.WriteLine("Retrieved additional build config in cdn config: " + cdnBuildConfigs[i].buildName);

                        Console.WriteLine("Loading encoding " + cdnBuildConfigs[i].encoding[1]);
                        var subBuildEncoding = getEncoding("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", cdnBuildConfigs[i].encoding[1], int.Parse(cdnBuildConfigs[i].encodingSize[1])); //Use of first encoding is unknown

                        string subBuildRootKey = null;
                        string subBuildDownloadKey = null;
                        string subBuildInstallKey = null;

                        foreach (var entry in subBuildEncoding.entries)
                        {
                            if (!hashes.ContainsKey(entry.key)) { hashes.Add(entry.key, entry.hash); }

                            if (entry.hash == cdnBuildConfigs[i].root.ToUpper()) { subBuildRootKey = entry.key; }
                            if (entry.hash == cdnBuildConfigs[i].download.ToUpper()) { subBuildDownloadKey = entry.key; }
                            if (entry.hash == cdnBuildConfigs[i].install.ToUpper()) { subBuildInstallKey = entry.key; }
                        }

                        if (subBuildRootKey != null && program != "pro") // Overwatch has it in archives
                        {
                            Console.WriteLine("Downloading root " + subBuildRootKey + " (in buildconfig: " + cdnBuildConfigs[i].root.ToUpper() + ")");
                            downloadCDNFile("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/" + "data/" + subBuildRootKey[0] + subBuildRootKey[1] + "/" + subBuildRootKey[2] + subBuildRootKey[3] + "/" + subBuildRootKey);
                        }

                        if (subBuildDownloadKey != null)
                        {
                            Console.WriteLine("Downloading download " + subBuildDownloadKey);
                            downloadCDNFile("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/" + "data/" + subBuildDownloadKey[0] + subBuildDownloadKey[1] + "/" + subBuildDownloadKey[2] + subBuildDownloadKey[3] + "/" + subBuildDownloadKey);
                        }

                        if (subBuildInstallKey != null)
                        {
                            Console.WriteLine("Downloading install " + subBuildInstallKey);
                            downloadCDNFile("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/" + "data/" + subBuildInstallKey[0] + subBuildInstallKey[1] + "/" + subBuildInstallKey[2] + subBuildInstallKey[3] + "/" + subBuildInstallKey);
                        }

                        if (cdnBuildConfigs[i].patchConfig != null)
                        {
                            if (cdnBuildConfigs[i].patchConfig.Contains(" ")) { throw new Exception("Patch config has multiple entries"); }
                            Console.WriteLine("Downloading patch config " + cdnBuildConfigs[i].patchConfig);
                            downloadCDNFile("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/" + "config/" + cdnBuildConfigs[i].patchConfig[0] + cdnBuildConfigs[i].patchConfig[1] + "/" + cdnBuildConfigs[i].patchConfig[2] + cdnBuildConfigs[i].patchConfig[3] + "/" + cdnBuildConfigs[i].patchConfig);
                        }

                        if (cdnBuildConfigs[i].patch != null)
                        {
                            if (cdnBuildConfigs[i].patch.Contains(" ")) { throw new Exception("Patch has multiple entries"); }
                            Console.WriteLine("Downloading patch " + cdnBuildConfigs[i].patch);
                            downloadCDNFile("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/" + "patch/" + cdnBuildConfigs[i].patch[0] + cdnBuildConfigs[i].patch[1] + "/" + cdnBuildConfigs[i].patch[2] + cdnBuildConfigs[i].patch[3] + "/" + cdnBuildConfigs[i].patch);
                        }
                    }
                }

                //Get all stuff from additional builds

                if (cdnConfig.patchArchives != null)
                {
                    for (var i = 0; i < cdnConfig.patchArchives.Count(); i++)
                    {
                        Console.WriteLine("Downloading patch archive " + cdnConfig.patchArchives[i]);
                        downloadCDNFile("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/" + "patch/" + cdnConfig.patchArchives[i][0] + cdnConfig.patchArchives[i][1] + "/" + cdnConfig.patchArchives[i][2] + cdnConfig.patchArchives[i][3] + "/" + cdnConfig.patchArchives[i]);
                        downloadCDNFile("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/" + "patch/" + cdnConfig.patchArchives[i][0] + cdnConfig.patchArchives[i][1] + "/" + cdnConfig.patchArchives[i][2] + cdnConfig.patchArchives[i][3] + "/" + cdnConfig.patchArchives[i] + ".index");
                    }
                }

                Console.Write("Loading " + cdnConfig.archives.Count() + " indexes..");
                indexes = getIndexes("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", cdnConfig.archives);
                Console.Write("..done\n");
                Console.Write("Downloading " + cdnConfig.archives.Count() + " archives..");
                getArchives("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", cdnConfig.archives);
                Console.Write("..done\n");

                Console.Write("Loading encoding..");
                encoding = getEncoding("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", buildConfig.encoding[1], int.Parse(buildConfig.encodingSize[1])); //Use of first encoding is unknown

                string rootKey = "";
                string downloadKey = "";
                string installKey = "";

                foreach (var entry in encoding.entries)
                {
                    if (entry.hash == buildConfig.root.ToUpper()) { rootKey = entry.key; }
                    if (entry.hash == buildConfig.download.ToUpper()) { downloadKey = entry.key; }
                    if (entry.hash == buildConfig.install.ToUpper()) { installKey = entry.key; }
                    if (!hashes.ContainsKey(entry.key)) { hashes.Add(entry.key, entry.hash); }
                }

                Console.Write("..done\n");


                if (program != "pro" && program != "agent" && program != "Prot" && program != "bnt" && program != "bna") // These aren't supported right now
                {
                    Console.Write("Loading root..");
                    if (rootKey == "") { Console.WriteLine("Unable to find root key in encoding!"); } else { getRoot("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", rootKey); }
                    Console.Write("..done\n");

                    Console.Write("Loading download..");
                    if (downloadKey == "") { Console.WriteLine("Unable to find download key in encoding!"); } else { download = getDownload("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", downloadKey); }
                    Console.Write("..done\n");

                    Console.Write("Loading install..");
                    if (installKey == "") { Console.WriteLine("Unable to find install key in encoding!"); } else { getInstall("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/", installKey); }
                    Console.Write("..done\n");
                }


                foreach (var index in indexes)
                {
                    foreach (var entry in index.archiveIndexEntries)
                    {
                        hashes.Remove(entry.headerHash);
                    }
                }

                Console.WriteLine("Downloading " + hashes.Count() + " unarchived files..");

                foreach (var entry in hashes)
                {
                    Console.WriteLine("Downloading " + entry.Key);
                    downloadCDNFile("http://" + cdns.entries[0].hosts[0] + "/" + cdns.entries[0].path + "/" + "data/" + entry.Key[0] + entry.Key[1] + "/" + entry.Key[2] + entry.Key[3] + "/" + entry.Key);
                }

                Console.WriteLine("Done downloading unarchived files.");

                GC.Collect();
            }

            Console.ReadLine();
        }

        private static cdnConfigFile getCDNconfig(string program, string url, string hash)
        {
            string content;
            var cdnConfig = new cdnConfigFile();

            try
            {
                content = Encoding.UTF8.GetString(downloadAndReturnCDNFile(url + "/config/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash));
            }
            catch (Exception e)
            {
                Console.WriteLine("Error retrieving CDN config: " + e.Message);
                return cdnConfig;
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

        private static versionsFile getVersions(string program)
        {
            string content;
            var versions = new versionsFile();

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

            versions.entries = new versionsEntry[lines.Count() - 1];

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
            
            if(versions.entries.Count() > 0)
            {
                //TODO For now this'll have to do
                var dirname = Path.Combine(cacheDir, "ngdp", program, program + "_" + versions.entries[0].buildId + "_" + versions.entries[0].versionsName);
                if (!Directory.Exists(dirname)) { Directory.CreateDirectory(dirname); }
                File.WriteAllText(Path.Combine(dirname, "versions"), content);
            }

            return versions;
        }

        private static cdnsFile getCDNs(string program)
        {
            string content;

            var cdns = new cdnsFile();

            using (var webClient = new System.Net.WebClient())
            {
                try
                {
                    content = webClient.DownloadString(new Uri(baseUrl + program + "/" + "cdns"));
                }catch(Exception e)
                {
                    Console.WriteLine("Error downloading CDNs file");
                    return cdns;
                }
            }

            var lines = content.Split(new string[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);


            cdns.entries = new cdnsEntry[lines.Count() - 1];
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

            //TODO For now thisll have to do
            var dirname = Path.Combine(cacheDir, "ngdp", program, program + "_" + versions.entries[0].buildId + "_" + versions.entries[0].versionsName);
            if (!Directory.Exists(dirname)) { Directory.CreateDirectory(dirname); }
            File.WriteAllText(Path.Combine(dirname, "cdns"), content);

            return cdns;
        }

        private static buildConfigFile getBuildConfig(string program, string url, string hash)
        {
            string content;

            var buildConfig = new buildConfigFile();

            try
            {
                content = Encoding.UTF8.GetString(downloadAndReturnCDNFile(url + "/config/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash));
            }
            catch (Exception e)
            {
                Console.WriteLine("Error retrieving build config: " + e.Message);
                return buildConfig;
            }
            
            if(string.IsNullOrEmpty(content) || !content.StartsWith("# Build")) {
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

        private static archiveIndex[] getIndexes(string url, string[] archives)
        {
            var indexes = new archiveIndex[archives.Count()];
            for (int i = 0; i < archives.Count(); i++)
            {
                indexes[i].name = archives[i];

                using (var webClient = new System.Net.WebClient())
                {
                    byte[] indexContent;
                    indexContent = downloadAndReturnCDNFile(url + "data/" + archives[i][0] + archives[i][1] + "/" + archives[i][2] + archives[i][3] + "/" + archives[i] + ".index");

                    using (BinaryReader bin = new BinaryReader(new MemoryStream(indexContent)))
                    {
                        int indexEntries = indexContent.Length / 4096;

                        var entries = new List<archiveIndexEntry>();

                        for (int b = 0; b < indexEntries; b++)
                        {
                            for (int bi = 0; bi < 170; bi++)
                            {
                                var entry = new archiveIndexEntry();
                                entry.headerHash = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");
                                entry.size = bin.ReadUInt32(true);
                                entry.offset = bin.ReadUInt32(true);
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

        private static void getArchives(string url, string[] archives)
        {
            var indexes = new archiveIndex[archives.Count()];
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
                            Console.Write("\r" + e.ProgressPercentage + "% for archive " + archives[i]);
                        };

                        while (webClient.IsBusy)
                        {
                        }

                    }
                }
                else
                {
                    var MyClient = WebRequest.Create(name) as HttpWebRequest;
                    MyClient.Method = WebRequestMethods.Http.Get;
                    var response = MyClient.GetResponse() as HttpWebResponse;
                    if (response.Headers["Content-Length"] != new FileInfo(cacheDir + cleanname).Length.ToString())
                    {
                        Console.WriteLine("!!! Archive " + cleanname + " is incomplete or has been deleted from CDN. " + response.Headers["Content-Length"] + " vs " + new FileInfo(cacheDir + cleanname).Length.ToString() + ". Attempting redownload!");
                        using (var webClient = new System.Net.WebClient())
                        {
                            //byte[] file;

                            try
                            {
                                webClient.DownloadFile(new Uri(name), cacheDir + cleanname);
                                // file = webClient.DownloadData(new Uri(name));
                                // if (file != null) File.WriteAllBytes(cacheDir + cleanname, file);
                            }
                            catch (WebException e)
                            {
                                Console.WriteLine(e.Message);
                            }
                        }
                    }
                    MyClient.Abort();

                }
            }
        }

        private static void getRoot(string url, string hash)
        {
            using (var webClient = new System.Net.WebClient())
            {
                byte[] content;
                //Console.WriteLine(url + "data/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash);
                content = downloadAndReturnCDNFile(url + "data/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash);

                //File.WriteAllBytes("root_encoded", content);

                //byte[] parsedContent = parseBLTEfile(content);

                //File.WriteAllBytes("root_decoded", parsedContent);

            }
        }

        private static downloadFile getDownload(string url, string hash)
        {
            var download = new downloadFile();

            using (var webClient = new System.Net.WebClient())
            {
                byte[] content;
                content = downloadAndReturnCDNFile(url + "data/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash);
                byte[] parsedContent = parseBLTEfile(content);

                using (BinaryReader bin = new BinaryReader(new MemoryStream(parsedContent)))
                {
                    if (Encoding.UTF8.GetString(bin.ReadBytes(2)) != "DL") { throw new Exception("Error while parsing download file. Did BLTE header size change?"); }
                    download.unk = bin.ReadBytes(3); // Unk
                    download.numEntries = bin.ReadUInt32(true);
                    download.numTags = bin.ReadUInt16(true);

                    download.entries = new downloadEntry[download.numEntries];
                    for (int i = 0; i < download.numEntries; i++)
                    {
                        download.entries[i].hash = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");
                        bin.ReadBytes(10);
                    }
                }
            }

            return download;
        }

        private static installFile getInstall(string url, string hash)
        {
            var install = new installFile();

            byte[] content;

            content = downloadAndReturnCDNFile(url + "data/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash);

            byte[] parsedContent = parseBLTEfile(content);

            using (BinaryReader bin = new BinaryReader(new MemoryStream(parsedContent)))
            {

                if (Encoding.UTF8.GetString(bin.ReadBytes(2)) != "IN") { throw new Exception("Error while parsing install file. Did BLTE header size change?"); }
                //install.unk = bin.ReadUInt32();
                // install.numEntries = bin.ReadUInt32();
            }

            return install;
        }

        private static encodingFile getEncoding(string url, string hash, int encodingSize)
        {
            var encoding = new encodingFile();

            byte[] content;
            content = downloadAndReturnCDNFile(url + "data/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash);

            if (encodingSize != content.Length)
            {
                
                content = downloadAndReturnCDNFile(url + "data/" + hash[0] + hash[1] + "/" + hash[2] + hash[3] + "/" + hash, true);

                if (encodingSize != content.Length)
                {
                    throw new Exception("File corrupt/not fully downloaded! Remove " + "data / " + hash[0] + hash[1] + " / " + hash[2] + hash[3] + " / " + hash + " from cache.");
                }
            }



            Console.WriteLine("[TEMP] Parsing BLTE file..");
            byte[] parsedContent = parseBLTEfile(content);
            Console.WriteLine("[TEMP] Parsed BLTE file!");

            Console.WriteLine("[TEMP] Parsing encoding..");
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

                encoding.headers = new encodingHeaderEntry[encoding.numEntriesA];

                for (int i = 0; i < encoding.numEntriesA; i++)
                {
                    encoding.headers[i].firstHash = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");
                    encoding.headers[i].checksum = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");
                }

                long chunkStart = bin.BaseStream.Position;

                encoding.entries = new encodingFileEntry[encoding.numEntriesA];
                List<encodingFileEntry> entries = new List<encodingFileEntry>();

                for (int i = 0; i < encoding.numEntriesA; i++)
                {
                    ushort keysCount;
                    while ((keysCount = bin.ReadUInt16()) != 0)
                    {
                        encodingFileEntry entry = new encodingFileEntry();
                        entry.keyCount = keysCount;
                        entry.size = bin.ReadUInt32(true);
                        entry.hash = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");
                        entry.key = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");

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
            Console.WriteLine("[TEMP] Done parsing encoding");

            return encoding;
        }

        private static byte[] parseBLTEfile(byte[] content)
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
            }

            return result.ToArray();
        }

        public static void downloadCDNFile(string url)
        {
            url = url.ToLower();

            string cleanname = url.Replace("http://" + cdns.entries[0].hosts[0], "");

            if (!File.Exists(cacheDir + cleanname))
            {
                using (var webClient = new System.Net.WebClient())
                {
                    try
                    {
                        if (!Directory.Exists(cacheDir + cleanname)) { Directory.CreateDirectory(Path.GetDirectoryName(cacheDir + cleanname)); }
                        webClient.DownloadFile(url, cacheDir + cleanname);
                    }
                    catch (WebException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }

        public static byte[] downloadAndReturnCDNFile(string url, bool redownload = false)
        {
            url = url.ToLower();

            string cleanname = url.Replace("http://" + cdns.entries[0].hosts[0], "");

            if (redownload || !File.Exists(cacheDir + cleanname))
            {
                using (var webClient = new System.Net.WebClient())
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

            return File.ReadAllBytes(cacheDir + cleanname);
        }
    }
}