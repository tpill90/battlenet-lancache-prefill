using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleNetPrefill.Extensions;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Structs.Enums;
using BattleNetPrefill.Web;
using Spectre.Console;

namespace BattleNetPrefill.Handlers
{
    public class ConfigFileHandler
    {
        private readonly CdnRequestManager _cdnRequestManager;

        public ConfigFileHandler(CdnRequestManager cdnRequestManager)
        {
            _cdnRequestManager = cdnRequestManager;
        }

        public async Task<CDNConfigFile> GetCdnConfigAsync(VersionsEntry targetVersion)
        {
            var cdnConfig = new CDNConfigFile();

            var content = Encoding.UTF8.GetString(await _cdnRequestManager.GetRequestAsBytesAsync(RootFolder.config, targetVersion.cdnConfig));
            var cdnConfigLines = content.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < cdnConfigLines.Length; i++)
            {
                if (cdnConfigLines[i].StartsWith("#") || cdnConfigLines[i].Length == 0) { continue; }
                var cols = cdnConfigLines[i].Split(new string[] { " = " }, StringSplitOptions.RemoveEmptyEntries);
                switch (cols[0])
                {
                    case "archives":
                        var archives = cols[1].Split(' ');
                        cdnConfig.archives = archives.Select(e => new Archive
                        {
                            hashId = e,
                            hashIdMd5 = e.ToMD5()
                        }).ToArray();
                        break;
                    case "archives-index-size":
                        var archiveLengths = cols[1].Split(' ').Select(e => Int32.Parse(e)).ToList();
                        for (int j = 0; j < archiveLengths.Count; j++)
                        {
                            cdnConfig.archives[j].archiveIndexSize = archiveLengths[j];
                        }
                        break;
                    case "archive-group":
                        cdnConfig.archiveGroup = cols[1];
                        break;
                    case "patch-archives":
                        if (cols.Length > 1)
                        {
                            cdnConfig.patchArchives = cols[1].Split(' ').Select(e => e.ToMD5()).ToArray();
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
                        cdnConfig.fileIndex = cols[1].ToMD5();
                        break;
                    case "file-index-size":
                        cdnConfig.fileIndexSize = cols[1];
                        break;
                    case "patch-file-index":
                        cdnConfig.patchFileIndex = cols[1].ToMD5();
                        break;
                    case "patch-file-index-size":
                        cdnConfig.patchFileIndexSize = Int32.Parse(cols[1]);
                        break;
                    case "patch-archives-index-size":
                        cdnConfig.patchArchivesIndexSize = cols[1].Split(' ').Select(e => Int32.Parse(e)).ToArray();
                        break;
                    default:
                        if (cols != null)
                        {
                            AnsiConsole.WriteLine("!!!!!!!! Unknown CdnConfig variable '" + cols[0] + "'");
                        }

                        break;
                }
            }

            if (cdnConfig.archives == null)
            {
                throw new Exception("Invalid CDNConfig");
            }
            
            return cdnConfig;
        }
        
        public async Task<VersionsEntry> GetLatestVersionEntryAsync(TactProduct tactProduct)
        {
            var versions = new VersionsFile();
            string content = await _cdnRequestManager.MakePatchRequestAsync(tactProduct, PatchRequest.versions);
            
            var lines = content.Replace("\0", "")
                               .Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            var lineList = new List<string>();

            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i][0] != '#')
                {
                    lineList.Add(lines[i]);
                }
            }

            lines = lineList.ToArray();

            if (lines.Any())
            {
                versions.entries = new VersionsEntry[lines.Length - 1];

                var cols = lines[0].Split('|');

                for (var c = 0; c < cols.Length; c++)
                {
                    var friendlyName = cols[c].Split('!').ElementAt(0);

                    for (var i = 1; i < lines.Length; i++)
                    {
                        var row = lines[i].Split('|');

                        switch (friendlyName)
                        {
                            case "Region":
                                versions.entries[i - 1].region = row[c];
                                break;
                            case "BuildConfig":
                                versions.entries[i - 1].buildConfig = row[c].ToMD5();
                                break;
                            case "CDNConfig":
                                versions.entries[i - 1].cdnConfig = row[c].ToMD5();
                                break;
                            case "Keyring":
                            case "KeyRing":
                                var keyRing = row[c];
                                if (!String.IsNullOrEmpty(keyRing))
                                {
                                    versions.entries[i - 1].keyRing = keyRing.ToMD5();
                                }
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
                                AnsiConsole.WriteLine("!!!!!!!! Unknown versions variable '" + friendlyName + "'");
                                break;
                        }
                    }
                }
            }

            var targetVersion = versions.entries[0];
            QueueKeyRingFile(targetVersion);

            return targetVersion;
        }

        public void QueueKeyRingFile(VersionsEntry targetVersion)
        {
            // Making a request to load this "Key Ring" file.  Not used by anything in our application, however it is called for some reason
            // by the Actual Battle.Net client
            if (targetVersion.keyRing != null)
            {
                _cdnRequestManager.QueueRequest(RootFolder.config, targetVersion.keyRing.Value);
            }
        }
    }
}
