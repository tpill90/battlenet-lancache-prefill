using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BuildBackup.DebugUtil;
using BuildBackup.DebugUtil.Models;
using BuildBackup.Parsers;
using BuildBackup.Structs;
using BuildBackup.Utils;
using ByteSizeLib;
using Konsole;
using Colors = Shared.Colors;

namespace BuildBackup
{
    //TODO rename to something like HttpRequestHandler
    public class CDN
    {
        private readonly IConsole _console;
        private readonly HttpClient client;

        private readonly List<string> _cdnList = new List<string> 
        {
            "level3.blizzard.com",      // Level3
            "cdn.blizzard.com",         // Official regionless CDN
        };

        //TODO comment
        private string _productBasePath;

        //TODO comment
        private readonly Uri _battleNetPatchUri;

        //TODO break these requests out into a different class later
        public ConcurrentBag<Request> allRequestsMade = new ConcurrentBag<Request>();

        private readonly List<Request> _queuedRequests = new List<Request>();

        private CachedStringLookup _cachedStringLookup;

        /// <summary>
        /// When set to true, will skip any requests where the response is not required.  This can be used to dramatically speed up debugging time, as
        /// you won't need to wait for the full file transfer to complete.
        /// </summary>
        public bool DebugMode = false;

        public CDN(IConsole console, Uri battleNetPatchUri)
        {
            _console = console;
            _battleNetPatchUri = battleNetPatchUri;
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

        public void QueueRequest(RootFolder rootPath, MD5Hash hash, long? startBytes = null, long? endBytes = null, bool isIndex = false)
        {
            string uri;
            if (rootPath == RootFolder.data)
            {
                uri = _cachedStringLookup.TryGetPrecomputedValue(hash, rootPath);
            }
            else
            {
                var hashString = hash.ToString().ToLower();
                uri = $"{_productBasePath}/{rootPath.Name}/{hashString[0]}{hashString[1]}/{hashString[2]}{hashString[3]}/{hashString}";
            }

            if (isIndex)
            {
                uri += ".index";
            }

            if (startBytes != null && endBytes != null)
            {
                _queuedRequests.Add(new Request
                {
                    Uri = uri,
                    LowerByteRange = startBytes.Value,
                    UpperByteRange = endBytes.Value,
                    WriteToDevNull = true
                });
            }
            else
            {
                _queuedRequests.Add(new Request
                {
                    Uri = uri,
                    DownloadWholeFile = true,
                    WriteToDevNull = true
                });
            }
        }

        //TODO better name + comment
        private Dictionary<string, string> _queuedRequestLookupTable = new Dictionary<string, string>();

        //TODO finish making everything use this
        //TODO Switch hashId to MD5Hash
        public void QueueRequest(RootFolder rootPath, string hashId, long? startBytes = null, long? endBytes = null, bool isIndex = false)
        {
            string uri;
            if (rootPath == RootFolder.data)
            {
                if (_queuedRequestLookupTable.ContainsKey(hashId))
                {
                    uri = _queuedRequestLookupTable[hashId];
                }
                else
                {
                    uri = $"{_productBasePath}/{rootPath.Name}/{hashId[0]}{hashId[1]}/{hashId[2]}{hashId[3]}/{hashId}";
                    _queuedRequestLookupTable.Add(hashId, uri);
                }
            }
            else
            {
                uri = $"{_productBasePath}/{rootPath.Name}/{hashId[0]}{hashId[1]}/{hashId[2]}{hashId[3]}/{hashId}";
            }

            if (isIndex)
            {
                uri += ".index";
            }

            if (startBytes != null && endBytes != null)
            {
                _queuedRequests.Add(new Request
                {
                    Uri = uri,
                    LowerByteRange = startBytes.Value,
                    UpperByteRange = endBytes.Value,
                    WriteToDevNull = true
                });
            }
            else
            {
                _queuedRequests.Add(new Request
                {
                    Uri = uri,
                    DownloadWholeFile = true,
                    WriteToDevNull = true
                });
            }
        }

        //TODO nicer progress bar
        //TODO this doesn't max out my connection at 300mbs
        public void DownloadQueuedRequests()
        {
            var timer = Stopwatch.StartNew();

            var coalesced = NginxLogParser.CoalesceRequests(_queuedRequests, true);

            Console.WriteLine($"Downloading {Colors.Cyan(coalesced.Count)} total queued requests " +
                              $"Totaling {Colors.Magenta(ByteSize.FromBytes(coalesced.Sum(e => e.TotalBytes)))}");
            int count = 0;
            var progressBar = new ProgressBar(_console, PbStyle.SingleLine, coalesced.Count, 50);
            //TODO There is an issue here where exceptions get thrown for Warzone, Vanguard, BOCW, and Starcraft 2.  Probably related to a threading issue..
            Parallel.ForEach(coalesced, new ParallelOptions { MaxDegreeOfParallelism = 30 }, entry =>
            {
                if (entry.DownloadWholeFile)
                {
                    GetRequestAsBytes(entry.Uri, writeToDevNull: entry.WriteToDevNull).Wait();
                }
                else
                {
                    GetRequestAsBytes(entry.Uri, writeToDevNull: entry.WriteToDevNull, entry.LowerByteRange, entry.UpperByteRange).Wait();
                }

                if (!DebugMode)
                {
                    // Skip refreshing the progress bar when debugging.  Slows things down
                    progressBar.Refresh(count, "");
                }

                count++;
            });

            timer.Stop();
            progressBar.Refresh(count, $"     Done! {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");
        }
        
        public Task<byte[]> GetRequestAsBytes(RootFolder rootPath, string hashId, bool isIndex = false)
        {
            //TODO remove this ToLower() call
            hashId = hashId.ToLower();
            var uri = $"{_productBasePath}/{rootPath.Name}/{hashId.Substring(0, 2)}/{hashId.Substring(2, 2)}/{hashId}";
            if (isIndex)
            {
                uri = $"{uri}.index";
            }
            return GetRequestAsBytes(uri);
        }

        //TODO comment
        //TODO come up with a better name for writeToDevNull
        private async Task<byte[]> GetRequestAsBytes(string requestUri, bool writeToDevNull = false, long? startBytes = null, long? endBytes = null)
        {
            LogRequestMade(requestUri, startBytes, endBytes);

            // When we are running in debug mode, we can skip entirely any requests that will end up written to dev/null.  Will speed up debugging.
            if (DebugMode && writeToDevNull)
            {
                return null;
            }

            // TODO cache this in a dict
            var uri = new Uri($"http://{_cdnList[0]}/{requestUri}");

            // Try to return a cached copy from the disk first, before making an actual request
            if (!writeToDevNull)
            {
                string outputFilePath = Path.Combine(Config.CacheDir + uri.AbsolutePath);
                if (File.Exists(outputFilePath))
                {
                    return await File.ReadAllBytesAsync(outputFilePath);
                }
            }
            
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            if (startBytes != null && endBytes != null)
            {
                requestMessage.Headers.Range = new RangeHeaderValue(startBytes, endBytes);
            }

            //TODO Handle "The response ended prematurely" exceptions.  Maybe add them to the queue again to be retried?
            using var responseMessage = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            await using Stream responseStream = await responseMessage.Content.ReadAsStreamAsync();

            if (!responseMessage.IsSuccessStatusCode)
            {
                throw new FileNotFoundException($"Error retrieving file: HTTP status code {responseMessage.StatusCode} on URL http://{uri.ToString()}");
            }
            if(writeToDevNull)
            {
                try
                {
                    // Dump the received data, so we don't have to waste time writing it to disk.
                    responseStream.CopyToAsync(Stream.Null).Wait();
                }
                catch (Exception)
                {
                    Console.WriteLine(Colors.Red($"Error downloading : {uri} {startBytes}-{endBytes}"));
                }
                
                return null;
            }
            await using var memoryStream = new MemoryStream();
            responseStream.CopyToAsync(memoryStream).Wait();

            var byteArray = memoryStream.ToArray();
                
            // Cache to disk
            FileInfo file = new FileInfo(Path.Combine(Config.CacheDir + uri.AbsolutePath));
            file.Directory.Create();
            await File.WriteAllBytesAsync(file.FullName, byteArray);

            return await Task.FromResult(byteArray);
        }

        private void LogRequestMade(string requestUri, long? startBytes, long? endBytes)
        {
            // Record the requests we're making, so we can use it for debugging
            if (startBytes != null && endBytes != null)
            {
                allRequestsMade.Add(new Request
                {
                    Uri = requestUri,
                    LowerByteRange = startBytes.Value,
                    UpperByteRange = endBytes.Value
                });
            }
            else
            {
                allRequestsMade.Add(new Request
                {
                    Uri = requestUri,
                    DownloadWholeFile = true
                });
            }
        }

        //TODO comment
        public string MakePatchRequest(TactProduct tactProduct, string target)
        {
            var cacheFile = $"{Config.CacheDir}/{target}-{tactProduct.ProductCode}.txt";

            // Load cached version, only valid for 1 hour
            if (File.Exists(cacheFile) && DateTime.Now < File.GetLastWriteTime(cacheFile).AddHours(1))
            {
                return File.ReadAllText(cacheFile);
            }

            using HttpResponseMessage response = client.GetAsync(new Uri($"{_battleNetPatchUri}{tactProduct.ProductCode}/{target}")).Result;
            if (response.IsSuccessStatusCode)
            {
                using HttpContent res = response.Content;
                string content = res.ReadAsStringAsync().Result;

                // Writes results to disk, to be used as cache later
                File.WriteAllText(cacheFile, content);
                return content;
            }

            throw new Exception("Error during retrieving HTTP cdns: Received bad HTTP code " + response.StatusCode);
        }
    }
}