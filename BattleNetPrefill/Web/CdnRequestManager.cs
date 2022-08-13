using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using BattleNetPrefill.Parsers;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Structs.Enums;
using BattleNetPrefill.Utils;
using BattleNetPrefill.Utils.Debug;
using BattleNetPrefill.Utils.Debug.Models;
using ByteSizeLib;
using Spectre.Console;
using static BattleNetPrefill.Utils.SpectreColors;

namespace BattleNetPrefill.Web
{
    public sealed class CdnRequestManager : IDisposable
    {
        private readonly HttpClient _client;

        private readonly List<string> _cdnList = new List<string> 
        {
            "level3.blizzard.com",  // Level3
            "cdn.blizzard.com",     // Official region-less CDN - Slow downloads
            "us.cdn.blizzard.com"
        };

        private int _retryCount;
        private string _currentCdn => _cdnList[_retryCount];

        /// <summary>
        /// The root path used to find the product's data on the CDN.  Must be queried from the patch API.
        /// </summary>
        private string _productBasePath;

        private readonly Uri _battleNetPatchUri;

        private readonly Dictionary<MD5Hash, List<Request>> _queuedRequests = new Dictionary<MD5Hash, List<Request>>();

        /// <summary>
        /// When enabled, will skip using any cached files from disk.  The disk cache can speed up repeated runs, however it can use up a non-trivial amount
        /// of storage in some cases (Wow uses several hundred mb of index files).
        /// </summary>
        private bool SkipDiskCache;

        #region Debugging

        /// <summary>
        /// When set to true, will skip any requests where the response is not required.  This can be used to dramatically speed up debugging time, as
        /// you won't need to wait for the full file transfer to complete.
        /// </summary>
        private bool DebugMode;

        /// <summary>
        /// Used only for debugging purposes.  Records all requests made, so that they can be later compared against the expected requests made.
        ///
        /// Must always be a ConcurrentBag, otherwise odd issues with unit tests can pop up due to concurrency
        /// </summary>
        public ConcurrentBag<Request> allRequestsMade = new ConcurrentBag<Request>();

        #endregion
        
        public CdnRequestManager(Uri battleNetPatchUri, bool useDebugMode = false, bool skipDiskCache = false)
        {
            _battleNetPatchUri = battleNetPatchUri;
            SkipDiskCache = skipDiskCache;
            DebugMode = useDebugMode;

            _client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        /// <summary>
        /// Initialization logic must be called prior to using this class.  Determines which root folder to download the CDN data from
        /// </summary>
        public async Task InitializeAsync(TactProduct currentProduct)
        {
            // Loading current CDNs
            var cdnsFile = await CdnsFileParser.ParseCdnsFileAsync(this, currentProduct);

            // Adds any missing CDN hosts
            foreach (var host in cdnsFile.entries.SelectMany(e => e.hosts))
            {
                if (!_cdnList.Contains(host))
                {
                    _cdnList.Add(host);
                }
            }

            _productBasePath = cdnsFile.entries[0].path;
        }

        #region Queued Request Handling

        public void QueueRequest(RootFolder rootPath, in MD5Hash hash, in long? startBytes = null, in long? endBytes = null, bool isIndex = false)
        {
            Request request = new Request(_productBasePath, rootPath, hash, startBytes, endBytes, writeToDevNull: true, isIndex);
         
            List<Request> requests;
            _queuedRequests.TryGetValue(hash, out requests);
            if (requests == null)
            {
                requests = new List<Request>();
                _queuedRequests.Add(hash, requests);
            }

            requests.Add(request);
        }

        /// <summary>
        /// Attempts to download all queued requests.  If all downloads are successful, will return true.
        /// In the case of any failed downloads, the failed downloads will be retried up to 3 times.  If the downloads fail 3 times, then
        /// false will be returned
        /// </summary>
        /// <param name="ansiConsole"></param>
        /// <returns>True if all downloads succeeded.  False if downloads failed 3 times.</returns>
        public async Task<bool> DownloadQueuedRequestsAsync(IAnsiConsole ansiConsole)
        {
            // Combining requests to improve download performance
            var coalescedRequests = RequestUtils.CoalesceRequests(_queuedRequests, true);
            _queuedRequests.Clear();

            ByteSize totalSize = coalescedRequests.SumTotalBytes();
            AnsiConsole.MarkupLine($"Downloading {Blue(coalescedRequests.Count)} total queued requests {LightYellow(totalSize.GibiBytes.ToString("N2") + " GiB")}");

            var failedRequests = new ConcurrentBag<Request>();
            await ansiConsole.CreateSpectreProgress().StartAsync(async ctx =>
            {
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
                ansiConsole.MarkupLine(Red($"{failedRequests.Count} failed downloads"));
                foreach (var request in failedRequests)
                {
                    AnsiConsole.MarkupLine(Red($"Error downloading : {request.Uri} {request.LowerByteRange}-{request.UpperByteRange}"));
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// Attempts to download the specified requests.  Returns a list of any requests that have failed.
        /// </summary>
        /// <returns>A list of failed requests</returns>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Want to catch all exceptions, regardless of type")]
        [SuppressMessage("CodeSmell", "ERP022:Unobserved exception in generic exception handler", Justification = "Want to catch all exceptions, regardless of type")]
        private async Task<ConcurrentBag<Request>> AttemptDownloadAsync(ProgressContext ctx, string taskTitle, List<Request> requests)
        {
            var progressTask = ctx.AddTask(taskTitle, new ProgressTaskSettings { MaxValue = requests.SumTotalBytes().Bytes });

            var failedRequests = new ConcurrentBag<Request>();
            var downloadRequest = async (Request request, CancellationToken _) =>
            {
                try
                {
                    await GetRequestAsBytesAsync(request, progressTask);
                }
                catch
                {
                    failedRequests.Add(request);
                }
            };

            // Splitting up small/large requests into two batches.  Splitting into two batches with different # of parallel requests will prevent the small
            // requests from choking out overall throughput.
            var byteThreshold = (long)ByteSize.FromMegaBytes(1).Bytes;
            var smallRequests = requests.Where(e => e.TotalBytes < byteThreshold).ToList();
            var smallDownloadTask = Parallel.ForEachAsync(smallRequests, new ParallelOptions { MaxDegreeOfParallelism = 15 }, async (request, _) => await downloadRequest(request, _));

            var largeRequests = requests.Where(e => e.TotalBytes >= byteThreshold).ToList();
            var largeDownloadTask = Parallel.ForEachAsync(largeRequests, new ParallelOptions { MaxDegreeOfParallelism = 10 }, async (request, _) => await downloadRequest(request, _));

            await Task.WhenAll(smallDownloadTask, largeDownloadTask);

            // Making sure the progress bar is always set to its max value, some files don't have a size, so the progress bar will appear as unfinished.
            return failedRequests;
        }

        #endregion

        /// <summary>
        /// Requests data from Blizzard's CDN, and returns the raw response.
        /// </summary>
        /// <param name="isIndex">If true, will attempt to download a .index file for the specified hash</param>
        /// <param name="writeToDevNull">If true, the response data will be ignored, and dumped to a null stream.
        ///                              This can be used with "non-required" requests, to speed up processing since we only care about reading the data once to prefill.
        /// </param>
        /// <returns></returns>
        public Task<byte[]> GetRequestAsBytesAsync(RootFolder rootPath, MD5Hash hash, bool isIndex = false, 
            bool writeToDevNull = false, long? startBytes = null, long? endBytes = null)
        {
            Request request = new Request(_productBasePath, rootPath, hash, startBytes, endBytes, writeToDevNull, isIndex);
            return GetRequestAsBytesAsync(request);
        }

        public async Task<byte[]> GetRequestAsBytesAsync(Request request, ProgressTask task = null)
        {
            var writeToDevNull = request.WriteToDevNull;
            var startBytes = request.LowerByteRange;
            var endBytes = request.UpperByteRange;

            allRequestsMade.Add(request);

            // When we are running in debug mode, we can skip any requests that will end up written to dev/null.  Will speed up debugging.
            if (DebugMode && writeToDevNull)
            {
                return null;
            }
            
            var uri = new Uri($"http://{_currentCdn}/{request.Uri}");

            // Try to return a cached copy from the disk first, before making an actual request
            if (!writeToDevNull && !SkipDiskCache)
            {
                string outputFilePath = Path.Combine(AppConfig.CacheDir + uri.AbsolutePath);
                if (File.Exists(outputFilePath))
                {
                    return await File.ReadAllBytesAsync(outputFilePath);
                }
            }
            
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            if (!request.DownloadWholeFile)
            {
                requestMessage.Headers.Range = new RangeHeaderValue(startBytes, endBytes);
            }

            using var responseMessage = await _client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            await using Stream responseStream = await responseMessage.Content.ReadAsStreamAsync();

            responseMessage.EnsureSuccessStatusCode();
            if(writeToDevNull)
            {
                byte[] buffer = ArrayPool<byte>.Shared.Rent(524_288);
                var totalBytesRead = 0;
                try
                {
                    while (true)
                    {
                        // Dump the received data, so we don't have to waste time writing it to disk.
                        var read = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                        if (read == 0)
                        {
                            ArrayPool<byte>.Shared.Return(buffer);
                            return null;
                        }
                        task.Increment(read);
                        totalBytesRead += read;
                    }
                }
                catch (Exception)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                    // Making sure that the current request is marked as "complete" in the progress bar, otherwise the progress bar will never hit 100%
                    task.Increment(request.TotalBytes - totalBytesRead);
                    throw;
                }
            }

            await using var memoryStream = new MemoryStream();
            await responseStream.CopyToAsync(memoryStream);

            var byteArray = memoryStream.ToArray();
            if (SkipDiskCache)
            {
                return await Task.FromResult(byteArray);
            }
                
            // Cache to disk
            FileInfo file = new FileInfo(Path.Combine(AppConfig.CacheDir + uri.AbsolutePath));
            file.Directory.Create();
            await File.WriteAllBytesAsync(file.FullName, byteArray);

            return await Task.FromResult(byteArray);
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
            if (!SkipDiskCache && File.Exists(cacheFile) && DateTime.Now < File.GetLastWriteTime(cacheFile).AddMinutes(30))
            {
                return await File.ReadAllTextAsync(cacheFile);
            }

            using HttpResponseMessage response = await _client.GetAsync(new Uri($"{_battleNetPatchUri}{tactProduct.ProductCode}/{endpoint.Name}"));
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Error during retrieving HTTP cdns: Received bad HTTP code " + response.StatusCode);
            }
            using HttpContent res = response.Content;
            string content = await res.ReadAsStringAsync();

            if (SkipDiskCache)
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