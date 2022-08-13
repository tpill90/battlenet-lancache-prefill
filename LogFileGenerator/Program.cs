using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
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
        private static readonly ConfigFileHandler ConfigFileHandler = new ConfigFileHandler(new CdnRequestManager(AppConfig.BattleNetPatchUri));

        private static string RootInstallDir = @"E:\BattleNet";
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
                AnsiConsole.MarkupLine($"{LightYellow(product.DisplayName)} logs are out of date!  Generating latest logs..");

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
            var currentLogVersion = NginxLogParser.GetLatestLogVersionForProduct(AppConfig.LogFileBasePath, product);

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
                var httpClient = new HttpClient();
                var responseStream = httpClient.GetStreamAsync(downloadUrl).Result;
                using var fileStream = new FileStream(BnetInstallerPath, FileMode.Create);
                responseStream.CopyTo(fileStream);
            }
        }

        private static void InstallProduct(TactProduct product)
        {
            AnsiConsole.MarkupLine($"Installing {LightYellow(product.DisplayName)}....");

            var installPath = $@"{RootInstallDir}\{product.ProductCode}";
            if (!Directory.Exists(installPath))
            {
                Directory.CreateDirectory(installPath);
            }

            var process = Process.Start(new ProcessStartInfo(BnetInstallerPath , @$"--prod {product.ProductCode} --lang enus --dir {installPath}"));
            process.WaitForExit();
            
            AnsiConsole.MarkupLine("Installing done!");
        }

        private static void ManuallyInstallProduct(TactProduct product)
        {
            AnsiConsole.MarkupLine($"{LightYellow(product.DisplayName)} requires a manual install.  Install then press Enter when finished to continue....");
            Console.ReadLine();
        }

        private static void CopyLogsToHost(TactProduct product)
        {
            AnsiConsole.WriteLine("Copying logs to host...");

            var logFileFolder = $@"{AppConfig.LogFileBasePath}\{product.DisplayName.Replace(":", "")}";
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
            
            FilterLogs(logFilePath);

            // Creating a zip file
            var zipPath = @$"{logFileFolder}\{cdnVersion.versionsName}.zip";
            using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            archive.CreateEntryFromFile(logFilePath, Path.GetFileName(logFilePath), CompressionLevel.Optimal);
        }

        /// <summary>
        /// Filters out junk results from the logs, so that they don't pollute the result set with false negatives
        /// </summary>
        /// <param name="logFilePath"></param>
        private static void FilterLogs(string logFilePath)
        {
            var linesToKeep = new List<string>();
            foreach (var line in File.ReadLines(logFilePath))
            {
                // Only interested in GET requests from Battle.Net.  Filtering out any other requests from other clients like Steam
                if (!(line.Contains("GET") && line.Contains("[blizzard]")))
                {
                    continue;
                }
                // These requests seem to be made by the Battle.net client itself, cause false positives in our log comparison logic
                if (line.Contains("bnt002") || line.Contains("bnt004"))
                {
                    continue;
                }
                linesToKeep.Add(line);
            }
            File.WriteAllLines(logFilePath, linesToKeep);
        }

        private static void DeleteGameFiles()
        {
            AnsiConsole.WriteLine("Removing installed game files...");

            foreach (var dir in Directory.GetDirectories(RootInstallDir))
            {
                AnsiConsole.Markup($"   Deleting {LightYellow(dir)}...\n");
                Directory.Delete(dir, true);
            }

            foreach (var file in Directory.GetFiles(RootInstallDir))
            {
                AnsiConsole.Markup($"   Deleting {LightYellow(file)}...\n");
                File.Delete(file);
            }
        }
    }
}
