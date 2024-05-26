namespace BattleNetPrefill.Web
{
    public sealed class CdnRequestManager : IDisposable
    {
        private readonly HttpClient _client;
        private readonly IAnsiConsole _ansiConsole;

        private readonly List<string> _cdnList = new List<string>
        {
            "level3.blizzard.com",  // Level3
            "cdn.blizzard.com",     // Official region-less CDN - Slow downloads
            "us.cdn.blizzard.com"
        };

        private int _retryCount;
        private string _currentCdn => _cdnList[_retryCount];

        /// <summary>
        /// The URL/IP Address where the Lancache has been detected.
        /// </summary>
        private string _lancacheAddress;

        /// <summary>
        /// The root path used to find the product's data on the CDN.  Must be queried from the patch API.
        /// </summary>
        private string _productBasePath;

        private readonly List<Request> _queuedRequests = new List<Request>();

        #region Debugging

        /// <summary>
        /// Used only for debugging purposes.  Records all requests made, so that they can be later compared against the expected requests made.
        ///
        /// Must always be a ConcurrentBag, otherwise odd issues with unit tests can pop up due to concurrency
        /// </summary>
        public readonly ConcurrentBag<Request> allRequestsMade = new ConcurrentBag<Request>();

        #endregion

        public CdnRequestManager(IAnsiConsole ansiConsole)
        {
            _ansiConsole = ansiConsole;
            _client = new HttpClient();
        }

        /// <summary>
        /// Initialization logic must be called prior to using this class.  Determines which root folder to download the CDN data from
        /// </summary>
        public async Task InitializeAsync(TactProduct currentProduct)
        {
            // Loading current CDNs
            var entries = await CdnsFileParser.ParseCdnsFileAsync(this, currentProduct);

            // Adds any missing CDN hosts
            foreach (var host in entries.SelectMany(e => e.hosts))
            {
                if (!_cdnList.Contains(host))
                {
                    _cdnList.Add(host);
                }
            }

            _productBasePath = entries[0].path;
            _lancacheAddress = await LancacheIpResolver.ResolveLancacheIpAsync(_ansiConsole, _currentCdn);
        }

        #region Queued Request Handling

        public void QueueRequest(RootFolder rootPath, in MD5Hash hash, in long? startBytes = null, in long? endBytes = null, bool isIndex = false)
        {
            var request = new Request(_productBasePath, rootPath, hash, startBytes, endBytes, writeToDevNull: true, isIndex);
            _queuedRequests.Add(request);
        }

        /// <summary>
        /// Attempts to download all queued requests.  If all downloads are successful, will return true.
        /// In the case of any failed downloads, the failed downloads will be retried up to 3 times.  If the downloads fail 3 times, then
        /// false will be returned
        /// </summary>
        /// <returns>True if all downloads succeeded.  False if downloads failed 3 times.</returns>
        public async Task<bool> DownloadQueuedRequestsAsync(PrefillSummaryResult prefillSummaryResult)
        {
            // Combining requests to improve download performance
            List<Request> coalescedRequests = _queuedRequests.CoalesceRequests(true);
            _queuedRequests.Clear();

            ByteSize totalDownloadSize = coalescedRequests.SumTotalBytes();
            prefillSummaryResult.TotalBytesTransferred += totalDownloadSize;

            _ansiConsole.LogMarkupVerbose($"Downloading {Magenta(totalDownloadSize.ToDecimalString())} from {LightYellow(coalescedRequests.Count)} queued requests");

            if (AppConfig.SkipDownloads)
            {
                //TODO not a fan of writing it like this just so that the comparison logic keeps working
                foreach (var request in coalescedRequests)
                {
                    allRequestsMade.Add(request);
                }
                _ansiConsole.WriteLine();
                return true;
            }

            var downloadTimer = Stopwatch.StartNew();
            var failedRequests = new ConcurrentBag<Request>();
            await _ansiConsole.CreateSpectreProgress(AppConfig.TransferSpeedUnit).StartAsync(async ctx =>
            {
                //TODO can probably cleanup this attempt 3 times logic since there is the polly stuff in place now.
                // Run the initial download
                failedRequests = await AttemptDownloadAsync(ctx, "Downloading..", coalescedRequests);

                // Handle any failed requests
                while (failedRequests.Any() && _retryCount < 3)
                {
                    _retryCount++;
                    failedRequests = await AttemptDownloadAsync(ctx, $"Retrying  {_retryCount}..", failedRequests.ToList());
                    await Task.Delay(2000 * _retryCount);
                }
            });

            // Handling final failed requests
            if (failedRequests.Any())
            {
                _ansiConsole.MarkupLine(Red($"{failedRequests.Count} failed downloads"));
                foreach (var request in failedRequests)
                {
                    _ansiConsole.MarkupLine(Red($"Error downloading : {request.Uri} {request.LowerByteRange}-{request.UpperByteRange}"));
                }
                return false;
            }

            // Logging some metrics about the download
            _ansiConsole.LogMarkupLine($"Finished in {LightYellow(downloadTimer.FormatElapsedString())} - {Magenta(totalDownloadSize.CalculateBitrate(downloadTimer))}");
            _ansiConsole.WriteLine();


            return true;
        }

        /// <summary>
        /// Attempts to download the specified requests.  Returns a list of any requests that have failed.
        /// </summary>
        /// <param name="forceRecache">When specified, will cause the cache to delete the existing cached data for a request, and re-download it again.</param>
        /// <returns>A list of failed requests</returns>
        private async Task<ConcurrentBag<Request>> AttemptDownloadAsync(ProgressContext ctx, string taskTitle, List<Request> requests, bool forceRecache = false)
        {
            var progressTask = ctx.AddTask(taskTitle, new ProgressTaskSettings { MaxValue = requests.SumTotalBytes().Bytes });

            var failedRequests = new ConcurrentBag<Request>();

            // Splitting up small/large requests into two batches.  Splitting into two batches with different # of parallel requests will prevent the small
            // requests from choking out overall throughput.
            var byteThreshold = (long)ByteSize.FromMegaBytes(1).Bytes;

            var smallRequests = requests.Where(e => e.TotalBytes < byteThreshold).ToList();
            var smallDownloadTask = Parallel.ForEachAsync(smallRequests, new ParallelOptions { MaxDegreeOfParallelism = 15 }, async (request, _) => await DownloadRequestWrapper(request, _));

            var largeRequests = requests.Where(e => e.TotalBytes >= byteThreshold).ToList();
            var largeDownloadTask = Parallel.ForEachAsync(largeRequests, new ParallelOptions { MaxDegreeOfParallelism = 10 }, async (request, _) => await DownloadRequestWrapper(request, _));

            await Task.WhenAll(smallDownloadTask, largeDownloadTask);

            // Making sure the progress bar is always set to its max value, some files don't have a size, so the progress bar will appear as unfinished.
            return failedRequests;

            async Task DownloadRequestWrapper(Request request, CancellationToken _)
            {
                try
                {
                    await DownloadRequestAsync(request, forceRecache);
                }
                catch
                {
                    failedRequests.Add(request);
                }

                progressTask.Increment(request.TotalBytes);
            };
        }

        #endregion

        /// <summary>
        /// Requests data from Blizzard's CDN, and returns the raw response.  Will retry automatically.
        /// </summary>
        /// <param name="isIndex">If true, will attempt to download a .index file for the specified hash</param>
        /// <param name="writeToDevNull">If true, the response data will be ignored, and dumped to a null stream.
        ///                              This can be used with "non-required" requests, to speed up processing since we only care about reading the data once to prefill.
        /// </param>
        /// <returns></returns>
        public async Task<byte[]> GetRequestAsBytesAsync(RootFolder rootPath, MD5Hash hash, bool isIndex = false,
            bool writeToDevNull = false, long? startBytes = null, long? endBytes = null)
        {
            Request request = new Request(_productBasePath, rootPath, hash, startBytes, endBytes, writeToDevNull, isIndex);
            return await AppConfig.RetryPolicy.ExecuteAsync(async () =>
            {
                return await GetRequestAsBytesAsync(request);
            });
        }

        //TODO not a fan of how many times forceRecache is passed down
        private async Task<byte[]> GetRequestAsBytesAsync(Request request, bool forceRecache = false)
        {
            allRequestsMade.Add(request);

            var uri = new Uri($"http://{_lancacheAddress}/{request.Uri}");
            if (forceRecache)
            {
                uri = new Uri($"http://{_lancacheAddress}/{request.Uri}?nocache=1");
            }

            // Try to return a cached copy from the disk first, before making an actual request
            if (!AppConfig.NoLocalCache)
            {
                string outputFilePath = AppConfig.CacheDir + uri.AbsolutePath;
                if (File.Exists(outputFilePath))
                {
                    return await File.ReadAllBytesAsync(outputFilePath);
                }
            }

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            requestMessage.Headers.Host = _currentCdn;
            if (!request.DownloadWholeFile)
            {
                requestMessage.Headers.Range = new RangeHeaderValue(request.LowerByteRange, request.UpperByteRange);
            }

            using var cts = new CancellationTokenSource();
            using var responseMessage = await _client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            await using Stream responseStream = await responseMessage.Content.ReadAsStreamAsync(cts.Token);
            responseMessage.EnsureSuccessStatusCode();

            await using var memoryStream = new MemoryStream();
            await responseStream.CopyToAsync(memoryStream, cts.Token);

            var byteArray = memoryStream.ToArray();
            // Prevents the response from being cached to disk when --no-cache is specified
            if (AppConfig.NoLocalCache)
            {
                return await Task.FromResult(byteArray);
            }

            // Cache to disk
            FileInfo file = new FileInfo(AppConfig.CacheDir + uri.AbsolutePath);
            file.Directory.Create();
            await File.WriteAllBytesAsync(file.FullName, byteArray, cts.Token);

            return await Task.FromResult(byteArray);
        }

        //TODO comment the difference
        private async Task DownloadRequestAsync(Request request, bool forceRecache = false)
        {
            allRequestsMade.Add(request);

            var uri = new Uri($"http://{_lancacheAddress}/{request.Uri}");
            if (forceRecache)
            {
                uri = new Uri($"http://{_lancacheAddress}/{request.Uri}?nocache=1");
            }

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            requestMessage.Headers.Host = _currentCdn;
            if (!request.DownloadWholeFile)
            {
                requestMessage.Headers.Range = new RangeHeaderValue(request.LowerByteRange, request.UpperByteRange);
            }

            using var cts = new CancellationTokenSource();
            using var responseMessage = await _client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            await using Stream responseStream = await responseMessage.Content.ReadAsStreamAsync(cts.Token);
            responseMessage.EnsureSuccessStatusCode();

            // Don't save the data anywhere, so we don't have to waste time writing it to disk.
            var buffer = new byte[4096];
            while (await responseStream.ReadAsync(buffer, cts.Token) != 0)
            {
            }
        }

        /// <summary>
        /// Makes a request to the Patch API.  Caches the response if possible.
        ///
        /// https://wowdev.wiki/TACT#HTTP_URLs
        /// </summary>
        /// <returns></returns>
        public async Task<string> MakePatchRequestAsync(TactProduct tactProduct, PatchRequest endpoint)
        {
            var cacheFile = $"{AppConfig.CacheDir}/{endpoint.Name}-{tactProduct.ProductCode}.txt";

            // Load cached version, only valid for 30 minutes so that updated versions don't get accidentally ignored
            if (!AppConfig.NoLocalCache && File.Exists(cacheFile) && DateTime.Now < File.GetLastWriteTime(cacheFile).AddMinutes(30))
            {
                return await File.ReadAllTextAsync(cacheFile);
            }

            using HttpResponseMessage response = await _client.GetAsync(new Uri($"{AppConfig.BattleNetPatchUri}{tactProduct.ProductCode}/{endpoint.Name}"));
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Error during retrieving HTTP cdns: Received bad HTTP code " + response.StatusCode);
            }
            using HttpContent res = response.Content;
            string content = await res.ReadAsStringAsync();

            if (AppConfig.NoLocalCache)
            {
                return content;
            }

            // Writes results to disk, to be used as cache later
            await File.WriteAllTextAsync(cacheFile, content);
            return content;
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}