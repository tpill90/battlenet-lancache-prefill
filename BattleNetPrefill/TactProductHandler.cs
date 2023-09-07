namespace BattleNetPrefill
{
    public sealed class TactProductHandler
    {
        private readonly IAnsiConsole _ansiConsole;
        private readonly bool _skipDiskCache;
        private readonly bool _forcePrefill;

        private readonly PrefillSummaryResult _prefillSummaryResult = new PrefillSummaryResult();

        public TactProductHandler(IAnsiConsole ansiConsole, bool skipDiskCache = false, bool forcePrefill = false)
        {
            _ansiConsole = ansiConsole;
            _skipDiskCache = skipDiskCache;
            _forcePrefill = forcePrefill;
        }

        public async Task ProcessMultipleProductsAsync(List<TactProduct> productsToProcess)
        {
            var timer = Stopwatch.StartNew();

            var distinctProducts = productsToProcess.Distinct().ToList();
            _ansiConsole.LogMarkupLine($"Prefilling {LightYellow(productsToProcess.Count)} products \n");
            foreach (var productCode in distinctProducts)
            {
                try
                {
                    await ProcessProductAsync(productCode);
                }
                catch (Exception e)
                {
                    // Need to catch any exceptions that might happen during a single download, so that the other apps won't be affected
                    _ansiConsole.LogMarkupLine(Red($"Unexpected download error : {e.Message}  Skipping app..."));
                    _ansiConsole.MarkupLine("");
                    _prefillSummaryResult.FailedApps++;
                }
            }

            _ansiConsole.LogMarkupLine($"Prefill complete! Prefilled {Magenta(distinctProducts.Count)} apps", timer);
            _prefillSummaryResult.RenderSummaryTable(_ansiConsole);
        }

        /// <summary>
        /// Downloads a specified game, in the same manner that Battle.net does.  Should be used to pre-fill a LanCache with game data from Blizzard's CDN.
        /// </summary>
        /// <param name="skipDiskCache">If set to true, then no cache files will be written to disk.  Every run will re-request the required files</param>
        /// <param name="forcePrefill">By default, a product will be skipped if it has already prefilled the latest product version.
        ///                             Setting this to true will force a prefill regardless of previous runs.</param>
        public async Task<ComparisonResult> ProcessProductAsync(TactProduct product)
        {
            _ansiConsole.LogMarkupLine($"Starting {Cyan(product.DisplayName)}");

            var metadataTimer = Stopwatch.StartNew();

            // Initializing classes, now that we have our CDN info loaded
            using var cdnRequestManager = new CdnRequestManager(AppConfig.BattleNetPatchUri, _ansiConsole, _skipDiskCache);
            var downloadFileHandler = new DownloadFileHandler(cdnRequestManager);
            var configFileHandler = new ConfigFileHandler(cdnRequestManager);
            var installFileHandler = new InstallFileHandler(cdnRequestManager);
            var archiveIndexHandler = new ArchiveIndexHandler(cdnRequestManager, product);
            var patchLoader = new PatchLoader(cdnRequestManager);
            await cdnRequestManager.InitializeAsync(product);

            // Finding the latest version of the game
            VersionsEntry? targetVersion = await configFileHandler.GetLatestVersionEntryAsync(product);

            // Skip prefilling if we've already prefilled the latest version 
            if (!_forcePrefill && IsProductUpToDate(product, targetVersion.Value))
            {
                _prefillSummaryResult.AlreadyUpToDate++;
                _ansiConsole.Write("\n");
                return null;
            }
            

            await _ansiConsole.StatusSpinner().StartAsync("Start", async ctx =>
            {
                // Getting other configuration files for this version, that detail where we can download the required files from.
                ctx.Status("Getting latest config files...");
                BuildConfigFile buildConfig = await BuildConfigParser.GetBuildConfigAsync(targetVersion.Value, cdnRequestManager, product);
                CDNConfigFile cdnConfig = await configFileHandler.GetCdnConfigAsync(targetVersion.Value);

                ctx.Status("Building Archive Indexes...");
                await Task.WhenAll(archiveIndexHandler.BuildArchiveIndexesAsync(cdnConfig),
                                    downloadFileHandler.ParseDownloadFileAsync(buildConfig));

                // Start processing to determine which files need to be downloaded
                ctx.Status("Determining files to download...");
                await installFileHandler.HandleInstallFileAsync(buildConfig, archiveIndexHandler, cdnConfig);
                await downloadFileHandler.HandleDownloadFileAsync(archiveIndexHandler, cdnConfig, product);
                await patchLoader.HandlePatchesAsync(buildConfig, product, cdnConfig);
            });

            _ansiConsole.LogMarkupLine("Retrieved product metadata", metadataTimer);

            // Actually start the download of any deferred requests
            var downloadSuccess = await cdnRequestManager.DownloadQueuedRequestsAsync();
            if (downloadSuccess)
            {
                MarkDownloadAsSuccessful(product, targetVersion.Value);
                _prefillSummaryResult.Updated++;
            }

            if (!AppConfig.CompareAgainstRealRequests)
            {
                return null;
            }

            var comparisonUtil = new ComparisonUtil();
            return await comparisonUtil.CompareAgainstRealRequestsAsync(cdnRequestManager.allRequestsMade.ToList(), product);
        }

        /// <summary>
        /// Checks to see if the previously prefilled version is up to date with the latest version on the CDN
        /// </summary>
        private bool IsProductUpToDate(TactProduct product, VersionsEntry latestVersion)
        {
            // Checking to see if a file has been previously prefilled
            var versionFilePath = $"{AppConfig.CacheDir}/prefilledVersion-{product.ProductCode}.txt";
            if (!File.Exists(versionFilePath))
            {
                return false;
            }

            // Checking to see if the game version previously prefilled is up to date with the latest version on the CDN.
            var lastPrefilledVersion = File.ReadAllText(versionFilePath);
            return latestVersion.versionsName == lastPrefilledVersion;
        }

        private void MarkDownloadAsSuccessful(TactProduct product, VersionsEntry latestVersion)
        {
            var versionFilePath = $"{AppConfig.CacheDir}/prefilledVersion-{product.ProductCode}.txt";
            File.WriteAllText(versionFilePath, latestVersion.versionsName);
        }

        #region Select Apps

        public void SetAppsAsSelected(List<TuiAppInfo> tuiAppModels)
        {
            List<string> selectedAppIds = tuiAppModels.Where(e => e.IsSelected)
                                                    .Select(e => e.AppId)
                                                    .ToList();
            File.WriteAllText(AppConfig.UserSelectedAppsPath, JsonSerializer.Serialize(selectedAppIds, SerializationContext.Default.ListString));

            _ansiConsole.LogMarkupLine($"Selected {Magenta(selectedAppIds.Count)} apps to prefill!");
        }

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
