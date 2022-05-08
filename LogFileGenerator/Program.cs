using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using BattleNetPrefill;
using BattleNetPrefill.Handlers;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Utils.Debug;
using BattleNetPrefill.Web;
using Spectre.Console;
using static BattleNetPrefill.Utils.SpectreColors;

namespace LogFileGenerator
{
    public static class Program
    {
        private static readonly ConfigFileHandler ConfigFileHandler = new ConfigFileHandler(new CdnRequestManager(Config.BattleNetPatchUri));

        private static string RootInstallDir = @"E:\BattleNet";
        //private static readonly string BnetInstallerPath = $"{Path.GetTempPath()}BnetInstaller.exe";
        //TODO Open a pull request to add CoD install tag support, and switch back once this is merged into the actual repo
        private static readonly string BnetInstallerPath = @"C:\Users\Tim\Dropbox\Programming\ThirdParty Repos\Battle.Net-Installer\BNetInstaller\bin\Release\net6.0\BNetInstaller.exe";

        private static readonly List<TactProduct> ManualInstallProducts = new List<TactProduct>
        {
            TactProduct.Hearthstone, TactProduct.CodBOCW
        };

        public static void Main()
        {
            EnsureBnetInstallerIsDownloaded();

            var products = TactProduct.AllEnumValues;
            foreach (var product in products)
            {
                if (IsLogFileUpToDate(product))
                {
                    AnsiConsole.MarkupLine($"{Green(product.DisplayName)} already up to date!  Skipping..");
                    continue;
                }
                AnsiConsole.MarkupLine($"{Yellow(product.DisplayName)} logs are out of date!  Generating latest logs..");

                ClearLancacheLogs();

                if (ManualInstallProducts.Contains(product))
                {
                    ManuallyInstallProduct(product);
                }
                else
                {
                    InstallProduct(product);
                }
                
                CopyLogsToHost(product);
            }

            DeleteGameFiles();
        }

        private static bool IsLogFileUpToDate(TactProduct product)
        {
            var currentLogVersion = NginxLogParser.GetLatestLogVersionForProduct(Config.LogFileBasePath, product);

            // Finding the latest version of the game according to blizzard
            VersionsEntry cdnVersion = ConfigFileHandler.GetLatestVersionEntryAsync(product).Result;

            return cdnVersion.versionsName == currentLogVersion;
        }

        private static void ClearLancacheLogs()
        {
            var info = new ProcessStartInfo("ssh")
            {
                Arguments = "-t tim@192.168.1.222 pwsh -f ./scripts/Clear-SteamCacheLogs.ps1",
                UseShellExecute = false
            };
            var process = Process.Start(info);
            process.WaitForExit();
        }

        private static void EnsureBnetInstallerIsDownloaded()
        {
            var downloadUrl = "https://github.com/barncastle/Battle.Net-Installer/releases/download/v1.6/BNetInstaller.exe";

            if (!File.Exists(BnetInstallerPath))
            {
                using (var client = new WebClient())
                {
                    client.DownloadFile(downloadUrl, BnetInstallerPath);
                }
            }
        }

        private static void InstallProduct(TactProduct product)
        {
            AnsiConsole.MarkupLine($"Installing {Yellow(product.DisplayName)}....");

            var installPath = $@"{RootInstallDir}\{product.ProductCode}";
            if (!Directory.Exists(installPath))
            {
                Directory.CreateDirectory(installPath);
            }
            //TODO remove this hardcoded path
            var info = new ProcessStartInfo($@"C:\Users\Tim\Dropbox\Apps\Gaming\BNetInstaller.exe", @$"--prod {product.ProductCode} --lang enus --dir {installPath}")
            {
            };
            var process = Process.Start(info);
            process.WaitForExit();


            AnsiConsole.MarkupLine($"Installing done!");
        }

        private static void ManuallyInstallProduct(TactProduct product)
        {
            AnsiConsole.MarkupLine($"{Yellow(product.DisplayName)} requires a manual install.  Install then press Enter when finished to continue....");
            Console.ReadLine();
        }

        private static void CopyLogsToHost(TactProduct product)
        {
            AnsiConsole.WriteLine($"Copying logs to host...");

            var logFileFolder = $@"{Config.LogFileBasePath}\{product.DisplayName.Replace(":", "")}";
            // Deleting original logs
            foreach (var file in Directory.GetFiles(logFileFolder))
            {
                File.Delete(file);
            }
            
            VersionsEntry cdnVersion = ConfigFileHandler.GetLatestVersionEntryAsync(product).Result;
            var logFilePath = $@"{logFileFolder}\{cdnVersion.versionsName}.log";
            // Copying the logs down
            var info = new ProcessStartInfo("scp", $@"tim@192.168.1.222:/mnt/nvme0n1/lancache/cache/logs/access.log ""{logFilePath}""")
            {
                UseShellExecute = false
            };
            var process = Process.Start(info);
            process.WaitForExit();

            //TODO cleanup crappy log entries before zipping.  Remove things like Steam logs, or bnt004

            // Creating a zip file
            var zipPath = @$"{logFileFolder}\{cdnVersion.versionsName}.zip";
            using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            var entry = archive.CreateEntryFromFile(logFilePath, Path.GetFileName(logFilePath), CompressionLevel.Optimal);
        }

        private static void DeleteGameFiles()
        {
            AnsiConsole.WriteLine($"Removing installed game files...");

            foreach (var dir in Directory.GetDirectories(RootInstallDir))
            {
                AnsiConsole.Markup($"   Deleting {Yellow(dir)}...\n");
                Directory.Delete(dir, true);
            }
        }
    }
}
