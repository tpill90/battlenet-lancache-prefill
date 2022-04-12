using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BuildBackup.Structs;
using Colors = Shared.Colors;

namespace BuildBackup
{
    public class ConfigFileHandler
    {
        private CDN cdn;

        public ConfigFileHandler(CDN cdn)
        {
            this.cdn = cdn;
        }

        public CDNConfigFile GetCDNconfig(VersionsEntry targetVersion)
        {
            var timer = Stopwatch.StartNew();

            var cdnConfig = new CDNConfigFile();

            var content = Encoding.UTF8.GetString(cdn.Get(RootFolder.config, targetVersion.cdnConfig));
            var cdnConfigLines = content.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < cdnConfigLines.Count(); i++)
            {
                if (cdnConfigLines[i].StartsWith("#") || cdnConfigLines[i].Length == 0) { continue; }
                var cols = cdnConfigLines[i].Split(new string[] { " = " }, StringSplitOptions.RemoveEmptyEntries);
                switch (cols[0])
                {
                    case "archives":
                        var archives = cols[1].Split(' ');
                        cdnConfig.archives = archives.Select(e => new Archive { hashId = e }).ToArray();
                        break;
                    case "archives-index-size":
                        cdnConfig.archivesIndexSize = cols[1].Split(' ').Select(e => Int32.Parse(e)).ToArray();
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
                    case "patch-archives-index-size":
                        cdnConfig.patchArchivesIndexSize = cols[1].Split(' ').Select(e => Int32.Parse(e)).ToArray();
                        break;
                    default:
                        if (cols != null)
                        {
                            Console.WriteLine("!!!!!!!! Unknown CdnConfig variable '" + cols[0] + "'");
                        }

                        break;
                }
            }

            if (cdnConfig.archives == null)
            {
                throw new Exception("Invalid CDNconfig");
            }
            
            Console.Write($"CDNConfig loaded, {Colors.Magenta(cdnConfig.archives.Count())} archives.".PadRight(Config.PadRight));
            Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}".PadLeft(Config.Padding));
            return cdnConfig;
        }

        //TODO comment
        public VersionsEntry GetLatestVersionEntry(TactProduct tactProduct)
        {
            var timer = Stopwatch.StartNew();

            string content = cdn.MakePatchRequest(tactProduct, "versions");
            var versions = new VersionsFile();

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

            var targetVersion = versions.entries[0];

            

            Console.Write("GetLatestVersion loaded...".PadRight(Config.PadRight));
            Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}".PadLeft(Config.Padding));
            return targetVersion;
        }

        public void QueueKeyRingFile(VersionsEntry targetVersion)
        {
            // Making a request to load this "Key Ring" file.  Not used by anything in our application, however it is called for some reason
            // by the Actual Battle.Net client
            if (!string.IsNullOrEmpty(targetVersion.keyRing))
            {
                cdn.QueueRequest(RootFolder.config, targetVersion.keyRing);
            }
        }
    }
}
