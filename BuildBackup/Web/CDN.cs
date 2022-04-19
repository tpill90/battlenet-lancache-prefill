using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BuildBackup.DebugUtil;
using BuildBackup.DebugUtil.Models;
using BuildBackup.Parsers;
using BuildBackup.Structs;
using ByteSizeLib;
using Spectre.Console;
using Colors = Shared.Colors;

namespace BuildBackup.Web
{
    //TODO rename to something like HttpRequestHandler
    public class CDN
    {
        private readonly IAnsiConsole _ansiConsole;

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

        private CachedStringLookup _cachedStringLookup;

        /// <summary>
        /// When set to true, will skip any requests where the response is not required.  This can be used to dramatically speed up debugging time, as
        /// you won't need to wait for the full file transfer to complete.
        /// </summary>
        public bool DebugMode = false;

        //TODO document
        public bool SkipDiskCache = false;

        public CDN(IAnsiConsole ansiConsole, Uri battleNetPatchUri, bool useDebugMode = false, bool skipDiskCache = false)
        {
            _ansiConsole = ansiConsole;
            _battleNetPatchUri = battleNetPatchUri;
            SkipDiskCache = skipDiskCache;
            DebugMode = useDebugMode;

            client = new HttpClient
            {
                Timeout = new TimeSpan(0, 5, 0)
            };
        }

        public void LoadCdnsFile(TactProduct currentProduct)
        {
            // Loading CDNs
            var cdnsFile = CdnsFileParser.ParseCdnsFile(this, currentProduct);

            _productBasePath = cdnsFile.entries[0].path;
            _cachedStringLookup = new CachedStringLookup(_productBasePath);

            // Adds any missing CDN hosts
            foreach (var host in cdnsFile.entries.SelectMany(e => e.hosts))
            {
                if (!_cdnList.Contains(host))
                {
                    _cdnList.Add(host);
                }
            }
        }

        //TODO finish making everything use this
        public void QueueRequest(RootFolder rootPath, in MD5Hash hash, long? startBytes = null, long? endBytes = null, bool isIndex = false)
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
        
        public void DownloadQueuedRequests(IAnsiConsole ansiConsole)
        {
            var coalescedRequests = RequestUtils.CoalesceRequests(_queuedRequests, true);
            
            var totalSize = ByteSize.FromBytes(coalescedRequests.Sum(e => e.TotalBytes));
            AnsiConsole.WriteLine($"Downloading {Colors.Cyan(coalescedRequests.Count)} total queued requests {Colors.Yellow(totalSize.GibiBytes.ToString("N2") + " GB")}");

            var progress = ansiConsole.Progress()
                       .HideCompleted(false)
                       .AutoClear(false)
                       .Columns(new ProgressColumn[]
                       {
                           new ProgressBarColumn(),
                           new PercentageColumn(),
                           new RemainingTimeColumn(),
                           new DownloadedColumn(),
                           new TransferSpeedColumn()
                       });
            progress.RefreshRate = TimeSpan.FromMilliseconds(100);
            progress.Start(ctx =>
            {
                var task = ctx.AddTask("Downloading...", new ProgressTaskSettings
                {
                    MaxValue = totalSize.Bytes
                });
                Parallel.ForEach(coalescedRequests, new ParallelOptions { MaxDegreeOfParallelism = 4 }, entry =>
                {
                    GetRequestAsBytes(entry, task).Wait();
                });
            });
            
        }

        public async Task<byte[]> GetRequestAsBytes(RootFolder rootPath, MD5Hash hash, bool isIndex = false, bool writeToDevNull = false,
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
            return await GetRequestAsBytes(request);
        }

        //TODO comment
        //TODO come up with a better name for writeToDevNull
        public async Task<byte[]> GetRequestAsBytes(Request request = null, ProgressTask task = null)
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
            if (startBytes != 0 && endBytes != 0)
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
                        var read = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                        if (read == 0)
                        {
                            break;
                        }

                        // Increment the number of read bytes for the progress task
                        task.Increment(read);
                    }

                    // Dump the received data, so we don't have to waste time writing it to disk.
                    //responseStream.CopyToAsync(Stream.Null).Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine(Colors.Red($"Error downloading : {uri} {startBytes}-{endBytes}"));
                }
                
                return null;
            }
            await using var memoryStream = new MemoryStream();
            responseStream.CopyToAsync(memoryStream).Wait();

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
            if (response.IsSuccessStatusCode)
            {
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

            throw new Exception("Error during retrieving HTTP cdns: Received bad HTTP code " + response.StatusCode);
        }
    }
}