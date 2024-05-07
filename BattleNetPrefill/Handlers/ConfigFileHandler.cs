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
            var cdnConfigLines = content.Split("\n", StringSplitOptions.RemoveEmptyEntries);

            foreach (var i in cdnConfigLines)
            {
                if (i.StartsWith('#') || i.Length == 0)
                {
                    continue;
                }
                var cols = i.Split(" = ", StringSplitOptions.RemoveEmptyEntries);
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
                    case "patch-archives":
                        if (cols.Length > 1)
                        {
                            cdnConfig.patchArchives = cols[1].Split(' ').Select(e => e.ToMD5()).ToArray();
                        }
                        break;
                    case "file-index":
                        cdnConfig.fileIndex = cols[1].ToMD5();
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
                    // We don't use these fields anywhere, so we'll skip over them
                    case "archive-group":
                    case "archives-index-size":
                    case "builds":
                    case "file-index-size":
                    case "patch-archive-group":
                        break;
                    default:
                        AnsiConsole.Console.LogMarkupError($"!!!!!!!! Unknown CdnConfig variable '{cols[0]}'");
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
            string content = await _cdnRequestManager.MakePatchRequestAsync(tactProduct, PatchRequest.versions);

            var lines = content.Replace("\0", "")
                               .Split("\n", StringSplitOptions.RemoveEmptyEntries)
                               .Where(t => t[0] != '#')
                               .ToList();

            if (!lines.Any())
            {
                throw new Exception("Version file has no entries!");
            }

            var versionEntries = new VersionsEntry[lines.Count - 1];

            var cols = lines[0].Split('|');

            for (var c = 0; c < cols.Length; c++)
            {
                var friendlyName = cols[c].Split('!').ElementAt(0);

                for (var i = 1; i < lines.Count; i++)
                {
                    var row = lines[i].Split('|');

                    switch (friendlyName)
                    {
                        case "BuildConfig":
                            versionEntries[i - 1].buildConfig = row[c].ToMD5();
                            break;
                        case "CDNConfig":
                            versionEntries[i - 1].cdnConfig = row[c].ToMD5();
                            break;
                        case "Keyring":
                        case "KeyRing":
                            var keyRing = row[c];
                            if (!String.IsNullOrEmpty(keyRing))
                            {
                                versionEntries[i - 1].keyRing = keyRing.ToMD5();
                            }
                            break;
                        case "VersionName":
                        case "VersionsName":
                            versionEntries[i - 1].versionsName = row[c].Trim('\r');
                            break;
                        // We don't use any of these fields
                        case "Region":
                        case "BuildId":
                        case "ProductConfig":
                            break;
                        default:
                            AnsiConsole.WriteLine($"!!!!!!!! Unknown versions variable '{friendlyName}'");
                            break;
                    }
                }
            }

            VersionsEntry targetVersion = versionEntries[0];
            QueueKeyRingFile(targetVersion);

            return targetVersion;
        }

        private void QueueKeyRingFile(VersionsEntry targetVersion)
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
