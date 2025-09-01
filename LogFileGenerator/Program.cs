﻿namespace LogFileGenerator
{
    //TODO BnetInstaller doesn't seem to work anymore.  Check out https://github.com/redact7/bnetinstaller or update https://github.com/barncastle/Battle.Net-Installer
    public static class Program
    {
        private static readonly CdnRequestManager CdnRequestManager = new CdnRequestManager(AnsiConsole.Console);
        private static readonly ConfigFileHandler ConfigFileHandler = new ConfigFileHandler(CdnRequestManager);

        private static string RootInstallDir = @"C:\BattleNet";
        private static readonly string BnetInstallerPath = Path.Combine(Path.GetTempPath(), "BattleNet-Installer.exe");

        // These need to be manually installed via Battle.Net because they have multiple options for their install
        private static readonly List<TactProduct> ManualInstallProducts = new List<TactProduct>
        {
            TactProduct.Starcraft1, TactProduct.Starcraft2, TactProduct.Overwatch2, TactProduct.WorldOfWarcraft, TactProduct.WowClassic, TactProduct.WowClassicEra
        };

        // These are the products that we'll make sure are up to date
        private static List<TactProduct> ProductsToCheck = new List<TactProduct>
        {
            TactProduct.Starcraft1, TactProduct.Starcraft2, TactProduct.Overwatch2, TactProduct.WorldOfWarcraft, TactProduct.WowClassic, TactProduct.WowClassicEra
        };

        public static void Main()
        {
            // Battle.Net is required to be running for this to work.
            if (!Process.GetProcessesByName("Battle.net").Any())
            {
                AnsiConsole.MarkupLine("Starting Battle.Net");
                Process.Start(@"C:\Program Files (x86)\Battle.net\Battle.net Launcher.exe");
                Thread.Sleep(10_000);
            }

            EnsureBnetInstallerIsDownloaded();
            if (!Directory.Exists(RootInstallDir))
            {
                Directory.CreateDirectory(RootInstallDir);
            }

            foreach (var product in ProductsToCheck)
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

                // Deleting start menu entries
                foreach (var dir in Directory.EnumerateDirectories(@"C:\ProgramData\Microsoft\Windows\Start Menu\Programs", product.DisplayNameSanitized))
                {
                    Directory.Delete(dir, true);
                }
            }
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
                Arguments = "-t tim@192.168.1.222 bash ./scripts/lancache/Empty-LancacheLogs.sh",
                UseShellExecute = false
            };
            var process = Process.Start(info);
            process.WaitForExit();
        }

        private static void EnsureBnetInstallerIsDownloaded()
        {
            var downloadUrl = "https://github.com/barncastle/Battle.Net-Installer/releases/download/v1.6/BNetInstaller.exe";

            if (File.Exists(BnetInstallerPath))
            {
                return;
            }

            using var httpClient = new HttpClient();
            using var responseStream = httpClient.GetStreamAsync(downloadUrl).Result;
            using var fileStream = new FileStream(BnetInstallerPath, FileMode.Create);
            responseStream.CopyTo(fileStream);
        }

        private static void InstallProduct(TactProduct product)
        {
            AnsiConsole.MarkupLine($"Installing {LightYellow(product.DisplayName)}....");

            var installPath = $@"{RootInstallDir}\{product.ProductCode}";
            if (!Directory.Exists(installPath))
            {
                Directory.CreateDirectory(installPath);
            }

            var process = Process.Start(new ProcessStartInfo(BnetInstallerPath, @$"--prod {product.ProductCode} --lang enus --dir {installPath}"));
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
            var info = new ProcessStartInfo("scp", $@"tim@192.168.1.222:/mnt/temp/lancache/logs/access.log ""{logFilePath}""")
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
    }
}
