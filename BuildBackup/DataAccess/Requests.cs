using System;
using System.IO;
using System.Linq;
using System.Text;
using BuildBackup.Structs;

namespace BuildBackup.DataAccess
{
    public static class Requests
    {
        public static BuildConfigFile GetBuildConfig(VersionsEntry versionsEntry, CDN cdn)
        {
            string content;

            var buildConfig = new BuildConfigFile();

            try
            {
                content = Encoding.UTF8.GetString(cdn.Get(RootFolder.config, versionsEntry.buildConfig));
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

            return buildConfig;
        }
    }
}