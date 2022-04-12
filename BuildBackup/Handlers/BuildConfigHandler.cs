using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BuildBackup.Structs;
using Shared;

namespace BuildBackup.Handlers
{
    public static class BuildConfigHandler
    {
        public static BuildConfigFile GetBuildConfig(VersionsEntry versionsEntry, CDN cdn)
        {
            var timer = Stopwatch.StartNew();

            var buildConfig = new BuildConfigFile();

            string content = Encoding.UTF8.GetString(cdn.Get(RootFolder.config, versionsEntry.buildConfig));
            
            if (string.IsNullOrEmpty(content) || !content.StartsWith("# Build"))
            {
                Console.WriteLine("Error reading build config!");
                return buildConfig;
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
                        buildConfig.root = cols[1].FromHexString().ToMD5();
                        break;
                    case "download":
                        buildConfig.download = cols[1].Split(' ').Select(e => e.FromHexString().ToMD5()).ToArray();
                        break;
                    case "install":
                        buildConfig.install = cols[1].Split(' ').Select(e => e.FromHexString().ToMD5()).ToArray();
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
                    case "patch-index":
                        buildConfig.patchIndex = cols[1].Split(' ');
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
                    case "vfs-root":
                        buildConfig.vfsRoot = cols[1].Split(' ');
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
                    default:
                        Console.WriteLine("!!!!!!!! Unknown buildconfig variable '" + cols[0] + "'");
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(buildConfig.buildName))
            {
                Console.WriteLine($"Missing buildname in buildConfig, setting build name!");
                buildConfig.buildName = "UNKNOWN";
            }

            // Making a request to load "size" + "vfsRoot" files.  Not used by anything in our application, however it is called for some reason
            // by the Actual Battle.Net client
            cdn.QueueRequest(RootFolder.data, buildConfig.size[1], writeToDevNull: true);
            if (buildConfig.vfsRoot != null)
            {
                cdn.QueueRequest(RootFolder.data, buildConfig.vfsRoot[1], 0, buildConfig.vfsRootSize[1] - 1, true);
            }

            Console.Write("Parsed BuildConfig ...".PadRight(Config.PadRight));
            Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}".PadLeft(Config.Padding));

            return buildConfig;
        }
    }
}