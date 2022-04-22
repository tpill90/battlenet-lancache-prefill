using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using BattleNetPrefill.DebugUtil;
using BattleNetPrefill.DebugUtil.Models;
using BattleNetPrefill.Parsers;
using BattleNetPrefill.Structs;
using ByteSizeLib;
using Dasync.Collections;
using Spectre.Console;
using Colors = BattleNetPrefill.Utils.Colors;

namespace BattleNetPrefill.Web
{
    //TODO rename to something like HttpRequestHandler
    public class CDN
    {
        private readonly HttpClient client;

        private readonly List<string> _cdnList = new List<string> 
        {
            "level3.blizzard.com",      // Level3
            "cdn.blizzard.com"          // Official regionless CDN
        };

        //TODO comment
        private string _productBasePath;

        //TODO comment
       
        private readonly Uri _battleNetPatchUri;
        
        public ConcurrentBag<Request> allRequestsMade = new ConcurrentBag<Request>();

        private readonly List<Request> _queuedRequests = new List<Request>();

        /// <summary>
        /// When set to true, will skip any requests where the response is not required.  This can be used to dramatically speed up debugging time, as
        /// you won't need to wait for the full file transfer to complete.
        /// </summary>
        public bool DebugMode = false;

        //TODO document
        public bool SkipDiskCache = false;

        public CDN(Uri battleNetPatchUri, bool useDebugMode = false, bool skipDiskCache = false)
        {
            _battleNetPatchUri = battleNetPatchUri;
            SkipDiskCache = skipDiskCache;
            DebugMode = useDebugMode;

            client = new HttpClient
            {
				//TODO is this needed?
                Timeout = Timeout.InfiniteTimeSpan
            };
        }

        //TODO assert that this must be required before using CDN class
        public void LoadCdnsFile(TactProduct currentProduct)
        {
            // Loading CDNs
            var cdnsFile = CdnsFileParser.ParseCdnsFile(this, currentProduct);

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

            var totalSize = ByteSize.FromBytes(coalescedRequests.Sum(e => e.TotalBytes));
            AnsiConsole.WriteLine($"Downloading {Colors.Cyan(coalescedRequests.Count)} total queued requests {Colors.Yellow(totalSize.GibiBytes.ToString("N2") + " GB")}");

            // Configuring the progress bar
            var progressBar = ansiConsole.Progress()
                       .HideCompleted(false)
                       .AutoClear(false)
                       .Columns(new ProgressBarColumn(), new PercentageColumn(), new RemainingTimeColumn(), new DownloadedColumn(), new TransferSpeedColumn());
            
            await progressBar.StartAsync(async ctx =>
            {
                // Kicking off the download
                var progressTask = ctx.AddTask("Downloading...", new ProgressTaskSettings { MaxValue = totalSize.Bytes });
                //TODO is ParallelForEachAsync even necessary?
                await coalescedRequests.ParallelForEachAsync(async item =>
                {
                    //TODO consider handling errors + retrying
                    await GetRequestAsBytesAsync(item, progressTask);
                }, maxDegreeOfParallelism: 4);

                // Making sure the progress bar is always set to its max value, some files don't have a size, so the progress bar will appear as unfinished.
                progressTask.Increment(progressTask.MaxValue);
            });
            
        }

        public async Task<byte[]> GetRequestAsBytesAsync(RootFolder rootPath, MD5Hash hash, bool isIndex = false, bool writeToDevNull = false,
            long? startBytes = null, long? endBytes = null)
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

        //TODO comment
        //TODO come up with a better name for writeToDevNull
        public async Task<byte[]> GetRequestAsBytesAsync(Request request = null, ProgressTask task = null)
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

            // TODO cache this in a dict
            var uri = new Uri($"http://{_cdnList[0]}/{request}");

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

            using var responseMessage = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
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
                catch (Exception e)
                {
                    AnsiConsole.WriteLine(Colors.Red($"Error downloading : {uri} {startBytes}-{endBytes}"));
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

        //TODO comment + possibly move to own file
        public string MakePatchRequest(TactProduct tactProduct, string target)
        {
            var cacheFile = $"{Config.CacheDir}/{target}-{tactProduct.ProductCode}.txt";

            // Load cached version, only valid for 1 hour
            if (!SkipDiskCache && File.Exists(cacheFile) && DateTime.Now < File.GetLastWriteTime(cacheFile).AddHours(1))
            {
                return File.ReadAllText(cacheFile);
            }

            using HttpResponseMessage response = client.GetAsync(new Uri($"{_battleNetPatchUri}{tactProduct.ProductCode}/{target}")).Result;
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Error during retrieving HTTP cdns: Received bad HTTP code " + response.StatusCode);
            }
            using HttpContent res = response.Content;
            string content = res.ReadAsStringAsync().Result;

            if (SkipDiskCache)
            {
                return content;
            }

            // Writes results to disk, to be used as cache later
            File.WriteAllText(cacheFile, content);
            return content;
        }
    }
}