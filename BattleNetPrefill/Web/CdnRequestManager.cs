using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using BattleNetPrefill.Parsers;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Structs.Enums;
using BattleNetPrefill.Utils.Debug;
using BattleNetPrefill.Utils.Debug.Models;
using ByteSizeLib;
using Dasync.Collections;
using Spectre.Console;
using static BattleNetPrefill.Utils.SpectreColors;

namespace BattleNetPrefill.Web
{
    public class CdnRequestManager
    {
        private readonly HttpClient _client;

        private readonly List<string> _cdnList = new List<string> 
        {
            "level3.blizzard.com",      // Level3
            "cdn.blizzard.com"          // Official regionless CDN
        };

        /// <summary>
        /// The root path used to find the product's data on the CDN.  Must be queried from the patch API.
        /// </summary>
        private string _productBasePath;

        private readonly Uri _battleNetPatchUri;
        
        private readonly List<Request> _queuedRequests = new List<Request>();

        public int ErrorCount { get; private set; }

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
				//TODO is this needed?
                Timeout = Timeout.InfiniteTimeSpan
            };
        }

        /// <summary>
        /// Initialization logic must be called prior to using this class.  Determines which root folder to download the CDN data from
        /// </summary>
        public async Task InitializeAsync(TactProduct currentProduct)
        {
            // Loading CDNs
            var cdnsFile = await CdnsFileParser.ParseCdnsFileAsync(this, currentProduct);

            _productBasePath = cdnsFile.entries[0].path;

            // Adds any missing CDN hosts
            foreach (var host in cdnsFile.entries.SelectMany(e => e.hosts))
            {
                if (!_cdnList.Contains(host))
                {
                    _cdnList.Add(host);
                }
            }
        }

        public void QueueRequest(RootFolder rootPath, in MD5Hash hash, in long? startBytes = null, in long? endBytes = null, bool isIndex = false)
        {
            Request request = new Request
            {
                ProductRootUri = _productBasePath,
                RootFolder = rootPath,
                CdnKey = hash,
                IsIndex = isIndex,
                WriteToDevNull = true
            };
            if (startBytes != null && endBytes != null)
            {
                request.LowerByteRange = startBytes.Value;
                request.UpperByteRange = endBytes.Value;
            }
            else
            {
                request.DownloadWholeFile = true;
            }
            _queuedRequests.Add(request);
        }
        
        public async Task DownloadQueuedRequestsAsync(IAnsiConsole ansiConsole)
        {
            var coalescedRequests = RequestUtils.CoalesceRequests(_queuedRequests, true);
            ByteSize totalSize = ByteSize.FromBytes(coalescedRequests.Sum(e => e.TotalBytes));
            AnsiConsole.MarkupLine($"Downloading {Blue(coalescedRequests.Count)} total queued requests {Yellow(totalSize.GibiBytes.ToString("N2") + " GB")}");

            // Configuring the progress bar
            var progressBar = ansiConsole.Progress()
                       .HideCompleted(false)
                       .AutoClear(false)
                       .Columns(new ProgressBarColumn(), new PercentageColumn(), new RemainingTimeColumn(), new DownloadedColumn(), new TransferSpeedColumn());
            
            await progressBar.StartAsync(async ctx =>
            {
                // Kicking off the download
                var progressTask = ctx.AddTask("Downloading...", new ProgressTaskSettings { MaxValue = totalSize.Bytes });
                
                await coalescedRequests.ParallelForEachAsync(async item =>
                {
                    await GetRequestAsBytesAsync(item, progressTask);
                }, maxDegreeOfParallelism: 8);

                // Making sure the progress bar is always set to its max value, some files don't have a size, so the progress bar will appear as unfinished.
                progressTask.Increment(progressTask.MaxValue);
            });
        }

        /// <summary>
        /// Requests data from Blizzard's CDN, and returns the raw response.
        /// </summary>
        /// <param name="isIndex">If true, will attempt to download a .index file for the specified hash</param>
        /// <param name="writeToDevNull">If true, the response data will be ignored, and dumped to a null stream.
        ///                              This can be used with "non-required" requests, to speed up processing since we only care about reading the data once to prefill.
        /// </param>
        /// <returns></returns>
        public async Task<byte[]> GetRequestAsBytesAsync(RootFolder rootPath, MD5Hash hash, bool isIndex = false, 
            bool writeToDevNull = false, long? startBytes = null, long? endBytes = null)
        {
            Request request = new Request
            {
                ProductRootUri = _productBasePath,
                RootFolder = rootPath,
                CdnKey = hash,
                IsIndex = isIndex,

                WriteToDevNull = writeToDevNull
            };

            if (startBytes != null && endBytes != null)
            {
                request.LowerByteRange = startBytes.Value;
                request.UpperByteRange = endBytes.Value;
            }
            else
            {
                request.DownloadWholeFile = true;
            }
            return await GetRequestAsBytesAsync(request);
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

            var uri = new Uri($"http://{_cdnList[0]}/{request.Uri}");

            // Try to return a cached copy from the disk first, before making an actual request
            if (!writeToDevNull && !SkipDiskCache)
            {
                string outputFilePath = Path.Combine(Config.CacheDir + uri.AbsolutePath);
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

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new FileNotFoundException($"Error retrieving file: HTTP status code {responseMessage.StatusCode} on URL http://{uri}");
            }
            if(writeToDevNull)
            {
                try
                {
                    var buffer = new byte[8192];
                    while (true)
                    {
                        // Dump the received data, so we don't have to waste time writing it to disk.
                        var read = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                        if (read == 0)
                        {
                            return null;
                        }
                        task.Increment(read);
                    }
                }
                catch (Exception)
                {
                    ErrorCount++;
                    AnsiConsole.MarkupLine(Red($"Error downloading : {uri} {startBytes}-{endBytes}"));
                }
                
                return null;
            }

            await using var memoryStream = new MemoryStream();
            await responseStream.CopyToAsync(memoryStream);

            var byteArray = memoryStream.ToArray();
            if (SkipDiskCache)
            {
                return await Task.FromResult(byteArray);
            }
                
            // Cache to disk
            FileInfo file = new FileInfo(Path.Combine(Config.CacheDir + uri.AbsolutePath));
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
            var cacheFile = $"{Config.CacheDir}/{endpoint.Name}-{tactProduct.ProductCode}.txt";

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
    }
}