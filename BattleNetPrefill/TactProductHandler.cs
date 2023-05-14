namespace BattleNetPrefill
{
    public sealed class TactProductHandler
    {
        private readonly TactProduct _product;
        private readonly IAnsiConsole _ansiConsole;

        /// <summary>
        /// Creates a new TactProductHandler for the specified product.
        /// </summary>
        /// <param name="product">The targeted game that should be downloaded</param>
        /// <param name="ansiConsole"></param>
        /// <param name="debugConfig"></param>
        public TactProductHandler(TactProduct product, IAnsiConsole ansiConsole)
        {
            _product = product;
            _ansiConsole = ansiConsole;
        }

        /// <summary>
        /// Downloads a specified game, in the same manner that Battle.net does.  Should be used to pre-fill a LanCache with game data from Blizzard's CDN.
        /// </summary>
        /// <param name="skipDiskCache">If set to true, then no cache files will be written to disk.  Every run will re-request the required files</param>
        /// <param name="forcePrefill">By default, a product will be skipped if it has already prefilled the latest product version.
        ///                             Setting this to true will force a prefill regardless of previous runs.</param>
        public async Task<ComparisonResult> ProcessProductAsync(bool skipDiskCache = false, bool forcePrefill = false)
        {
            var metadataTimer = Stopwatch.StartNew();

            // Initializing classes, now that we have our CDN info loaded
            using var cdnRequestManager = new CdnRequestManager(AppConfig.BattleNetPatchUri, _ansiConsole, skipDiskCache);
            var downloadFileHandler = new DownloadFileHandler(cdnRequestManager);
            var configFileHandler = new ConfigFileHandler(cdnRequestManager);
            var installFileHandler = new InstallFileHandler(cdnRequestManager);
            var archiveIndexHandler = new ArchiveIndexHandler(cdnRequestManager, _product);
            var patchLoader = new PatchLoader(cdnRequestManager);
            await cdnRequestManager.InitializeAsync(_product);

            _ansiConsole.LogMarkup($"Starting {Cyan(_product.DisplayName)}");

            // Finding the latest version of the game
            VersionsEntry? targetVersion = await configFileHandler.GetLatestVersionEntryAsync(_product);

            // Skip prefilling if we've already prefilled the latest version 
            if (!forcePrefill && IsProductUpToDate(targetVersion.Value))
            {
                _ansiConsole.MarkupLine($"   {Green("Up to date!")}");
                return null;
            }
            _ansiConsole.Write("\n");

            await _ansiConsole.StatusSpinner().StartAsync("Start", async ctx =>
            {
                // Getting other configuration files for this version, that detail where we can download the required files from.
                ctx.Status("Getting latest config files...");
                BuildConfigFile buildConfig = await BuildConfigParser.GetBuildConfigAsync(targetVersion.Value, cdnRequestManager, _product);
                CDNConfigFile cdnConfig = await configFileHandler.GetCdnConfigAsync(targetVersion.Value);

                ctx.Status("Building Archive Indexes...");
                await Task.WhenAll(archiveIndexHandler.BuildArchiveIndexesAsync(cdnConfig),
                                    downloadFileHandler.ParseDownloadFileAsync(buildConfig));

                // Start processing to determine which files need to be downloaded
                ctx.Status("Determining files to download...");
                await installFileHandler.HandleInstallFileAsync(buildConfig, archiveIndexHandler, cdnConfig);
                await downloadFileHandler.HandleDownloadFileAsync(archiveIndexHandler, cdnConfig, _product);
                await patchLoader.HandlePatchesAsync(buildConfig, _product, cdnConfig);
            });

            _ansiConsole.LogMarkupLine("Retrieved product metadata", metadataTimer);

            // Actually start the download of any deferred requests
            var downloadSuccess = await cdnRequestManager.DownloadQueuedRequestsAsync();
            if (downloadSuccess)
            {
                MarkDownloadAsSuccessful(targetVersion.Value);
            }

            if (!AppConfig.CompareAgainstRealRequests)
            {
                return null;
            }

            var comparisonUtil = new ComparisonUtil();
            return await comparisonUtil.CompareAgainstRealRequestsAsync(cdnRequestManager.allRequestsMade.ToList(), _product);
        }

        /// <summary>
        /// Checks to see if the previously prefilled version is up to date with the latest version on the CDN
        /// </summary>
        private bool IsProductUpToDate(VersionsEntry latestVersion)
        {
            // Checking to see if a file has been previously prefilled
            var versionFilePath = $"{AppConfig.CacheDir}/prefilledVersion-{_product.ProductCode}.txt";
            if (!File.Exists(versionFilePath))
            {
                return false;
            }

            // Checking to see if the game version previously prefilled is up to date with the latest version on the CDN.
            var lastPrefilledVersion = File.ReadAllText(versionFilePath);
            return latestVersion.versionsName == lastPrefilledVersion;
        }

        private void MarkDownloadAsSuccessful(VersionsEntry latestVersion)
        {
            var versionFilePath = $"{AppConfig.CacheDir}/prefilledVersion-{_product.ProductCode}.txt";
            File.WriteAllText(versionFilePath, latestVersion.versionsName);
        }

        #region Select Apps

        //TODO don't like the static
        //TODO don't like the ansiConsole being passed in
        public static void SetAppsAsSelected(List<TuiAppInfo> tuiAppModels, IAnsiConsole ansiConsole)
        {
            List<string> selectedAppIds = tuiAppModels.Where(e => e.IsSelected)
                                                    .Select(e => e.AppId)
                                                    .ToList();
            File.WriteAllText(AppConfig.UserSelectedAppsPath, JsonSerializer.Serialize(selectedAppIds, SerializationContext.Default.ListString));

            ansiConsole.LogMarkupLine($"Selected {Magenta(selectedAppIds.Count)} apps to prefill!");
        }

        //TODO don't like the static
        public static List<TactProduct> LoadPreviouslySelectedApps()
        {
            if (!File.Exists(AppConfig.UserSelectedAppsPath))
            {
                return new List<TactProduct>();
            }

            return JsonSerializer.Deserialize(File.ReadAllText(AppConfig.UserSelectedAppsPath), SerializationContext.Default.ListString)
                                 .Select(e => TactProduct.Parse(e))
                                 .ToList();
        }

        #endregion
    }
}
