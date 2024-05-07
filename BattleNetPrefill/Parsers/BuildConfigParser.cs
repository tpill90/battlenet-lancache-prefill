namespace BattleNetPrefill.Parsers
{
    public static class BuildConfigParser
    {
        public static async Task<BuildConfigFile> GetBuildConfigAsync(VersionsEntry versionsEntry, CdnRequestManager cdnRequestManager)
        {
            var buildConfig = new BuildConfigFile();

            string content = Encoding.UTF8.GetString(await cdnRequestManager.GetRequestAsBytesAsync(RootFolder.config, versionsEntry.buildConfig));

            if (string.IsNullOrEmpty(content) || !content.StartsWith("# Build"))
            {
                throw new Exception("Error reading build config!");
            }

            var lines = content.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            foreach (var i in lines)
            {
                if (i.StartsWith('#') || i.Length == 0)
                {
                    continue;
                }

                var cols = i.Split(" = ", StringSplitOptions.RemoveEmptyEntries);
                switch (cols[0])
                {
                    case "download":
                        buildConfig.download = cols[1].Split(' ').Select(e => e.ToMD5()).ToArray();
                        break;
                    case "install":
                        buildConfig.install = cols[1].Split(' ').Select(e => e.ToMD5()).ToArray();
                        break;
                    case "encoding":
                        buildConfig.encoding = cols[1].Split(' ').Select(e => e.ToMD5()).ToArray();
                        break;
                    case "encoding-size":
                        buildConfig.encodingSize = cols[1].Split(' ').Select(e => Int32.Parse(e)).ToArray();
                        break;
                    case "size":
                        buildConfig.size = cols[1].Split(' ').Select(e => e.ToMD5()).ToArray();
                        break;
                    case "size-size":
                        buildConfig.sizeSize = cols[1].Split(' ').Select(e => Int32.Parse(e)).ToArray();
                        break;
                    case "build-name":
                        buildConfig.buildName = cols[1];
                        if (String.IsNullOrWhiteSpace(buildConfig.buildName))
                        {
                            buildConfig.buildName = "UNKNOWN";
                        }
                        break;
                    case "patch":
                        buildConfig.patch = cols[1].ToMD5();
                        break;
                    case "patch-config":
                        buildConfig.patchConfig = cols[1].ToMD5();
                        break;
                    case "patch-index":
                        buildConfig.patchIndex = cols[1].Split(' ').Select(e => e.ToMD5()).ToArray();
                        break;
                    case "patch-index-size":
                        buildConfig.patchIndexSize = cols[1].Split(' ').Select(e => Int32.Parse(e)).ToArray();
                        break;
                    case "install-size":
                        buildConfig.installSize = cols[1].Split(' ').Select(e => Int32.Parse(e)).ToArray();
                        break;
                    case "build-partial-priority":
                    case "partial-priority":
                    case "partial-priority-size":
                        // Purposefully doing nothing with these.  Don't care about these values.
                        break;
                    case "vfs-root":
                        buildConfig.vfsRoot = cols[1].Split(' ').Select(e => e.ToMD5()).ToArray();
                        break;
                    case "vfs-root-size":
                        buildConfig.vfsRootSize = cols[1].Split(' ').Select(e => Int32.Parse(e)).ToArray();
                        break;
                    case "build-branch":
                    case "build-num":
                    case "build-number":
                    case "build-version":
                    case "build-attributes":
                    case "build-comments":
                    case "build-creator":
                    case "build-fixed-hash":
                    case "build-replay-hash":
                    case "build-t1-manifest-version":
                    case "build-playbuild-installer":
                    case "build-product":
                    case "build-token":
                    case "build-uid":
                    case "build-base-content-release-manifest-file-id":
                    case "build-base-content-release-manifest-hash":
                    case "build-content-release-manifest-file-id":
                    case "build-content-release-manifest-hash":
                    case "build-release-name":
                    case string a when Regex.IsMatch(a, "vfs-(\\d*)$"):
                    case string b when Regex.IsMatch(b, "vfs-(\\d*)-size$"):
                    case "build-changelist":
                    case "build-data-branch":
                    case "build-data-revision":
                    case "build-source-revision":
                    case "build-source-branch":
                    case "build-status":
                    case "build-stream":
                    case "build-has-data":
                    case "build-target-platform":
                    case "build-type":
                    case "build-timestamp":
                    case "download-size":
                    case "root":
                    case "patch-size":
                        // We don't use these fields anywhere, so we're purposefully doing nothing with these.
                        break;
                    default:
                        AnsiConsole.Console.LogMarkupVerbose($"!!!!!!!! Unknown buildconfig variable '{cols[0]}'");
                        buildConfig.UnknownKeyPairs.Add(cols[0], cols[1]);
                        break;
                }
            }

            // This data isn't used by our application.  Some TactProducts will make this call, so we do it anyway to match what Battle.Net does
            cdnRequestManager.QueueRequest(RootFolder.data, buildConfig.size[1], 0, buildConfig.sizeSize[1] - 1);

            // This can sometimes be skipped over, as it isn't always required to parse the encoding table.
            // Requesting it anyway since almost every product will download it in the real Battle.net client.
            cdnRequestManager.QueueRequest(RootFolder.data, buildConfig.encoding[1], 0, buildConfig.encodingSize[1] - 1);

            if (buildConfig.vfsRoot != null)
            {
                // Making a request to load "vfsRoot" files.  Not used by anything in our application,
                // however it is called for some reason by the Actual Battle.Net client
                var endBytes = Math.Max(4095, buildConfig.vfsRootSize[1] - 1);
                cdnRequestManager.QueueRequest(RootFolder.data, buildConfig.vfsRoot[1], 0, endBytes);
            }
            return buildConfig;
        }
    }
}