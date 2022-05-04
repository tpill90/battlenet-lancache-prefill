using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Utils;
using BattleNetPrefill.Web;
using Spectre.Console;

namespace BattleNetPrefill.Parsers
{
    public static class BuildConfigParser
    {
        public static async Task<BuildConfigFile> GetBuildConfigAsync(VersionsEntry versionsEntry, CdnRequestManager cdnRequestManager, TactProduct targetProduct)
        {
            var buildConfig = new BuildConfigFile();
            
            string content = Encoding.UTF8.GetString(await cdnRequestManager.GetRequestAsBytesAsync(RootFolder.config, versionsEntry.buildConfig));
            
            if (string.IsNullOrEmpty(content) || !content.StartsWith("# Build"))
            {
                throw new Exception("Error reading build config!");
            }

            var lines = content.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < lines.Count(); i++)
            {
                if (lines[i].StartsWith("#") || lines[i].Length == 0)
                {
                    continue;
                }

                var cols = lines[i].Split(new string[] { " = " }, StringSplitOptions.RemoveEmptyEntries);
                switch (cols[0])
                {
                    case "root":
                        buildConfig.root = cols[1].ToMD5();
                        break;
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
                    case "build-playbuild-installer":
                        buildConfig.buildPlaybuildInstaller = cols[1];
                        break;
                    case "build-product":
                        buildConfig.buildProduct = cols[1];
                        break;
                    case "build-token":
                        buildConfig.buildToken = cols[1];
                        break;
                    case "build-uid":
                        buildConfig.buildUid = cols[1];
                        break;
                    case "patch":
                        buildConfig.patch = cols[1].ToMD5();
                        break;
                    case "patch-size":
                        buildConfig.patchSize = cols[1];
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
                        buildConfig.installSize = cols[1].Split(' ').Select(e => Int32.Parse(e)).ToArray();
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
                    case "vfs-root":
                        buildConfig.vfsRoot = cols[1].Split(' ').Select(e => e.ToMD5()).ToArray();
                        break;
                    case "vfs-root-size":
                        buildConfig.vfsRootSize = cols[1].Split(' ').Select(e => Int32.Parse(e)).ToArray();
                        break;
                    case "build-base-content-release-manifest-file-id":
                        buildConfig.buildBaseContentReleaseManifestFileId = cols[1];
                        break;
                    case "build-base-content-release-manifest-hash":
                        buildConfig.buildBaseContentReleaseManifestHash = cols[1];
                        break;
                    case "build-content-release-manifest-file-id":
                        buildConfig.buildContentReleaseManifestFileId = cols[1];
                        break;
                    case "build-content-release-manifest-hash":
                        buildConfig.buildContentReleaseManifestHash = cols[1];
                        break;
                    case "build-release-name":
                        buildConfig.buildReleaseName = cols[1];
                        break;
                    case string a when Regex.IsMatch(a, "vfs-(\\d*)$"):
                        buildConfig.vfs.Add(cols[0], cols[1]);
                        break;
                    case string a when Regex.IsMatch(a, "vfs-(\\d*)-size$"):
                        buildConfig.vfsSize.Add(cols[0], cols[1]);
                        break;
                    case "build-changelist":
                    case "build-data-branch":
                    case "build-data-revision":
                    case "build-source-revision":
                    case "build-source-branch":
                    case "build-status":
                    case "build-stream":
                    case "build-has-data":
                    case "build-target-platform":
                        // Purposefully doing nothing with these.  Don't care about these values.
                        break;
                    default:
                        AnsiConsole.WriteLine($"!!!!!!!! Unknown buildconfig variable '{cols[0]}'");
                        buildConfig.UnknownKeyPairs.Add(cols[0], cols[1]);
                        break;
                }
            }
            
            if (targetProduct != TactProduct.Diablo3)
            {
                // This data isn't used by our application.  Some TactProducts will make this call, so we do it anyway to match what Battle.Net does
                cdnRequestManager.QueueRequest(RootFolder.data, buildConfig.size[1], 0, buildConfig.sizeSize[1] - 1);
            }
            
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