using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BuildBackup.DataAccess;
using BuildBackup.DebugUtil;
using BuildBackup.DebugUtil.Models;
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
        private readonly Uri _battleNetPatchUri;

        //TODO make these all private
        public readonly HttpClient client;

        public List<string> cdnList;

        //TODO break these requests out into a different class later
        public ConcurrentBag<Request> allRequestsMade = new ConcurrentBag<Request>();

        private readonly List<Request> _queuedRequests = new List<Request>();

        private CdnsFile _cdnsFile;
        private CachedStringLookup _cachedStringLookup;

        /// <summary>
        /// When set to true, will skip any requests where the response is not required.  This can be used to dramatically speed up debugging time, as
        /// you won't need to wait for the full file transfer to complete.
        /// </summary>
        public bool DebugMode = false;

        public CDN(IConsole console, Uri BattleNetPatchUri)
        {
            _console = console;
            _battleNetPatchUri = BattleNetPatchUri;
            client = new HttpClient
            {
                Timeout = new TimeSpan(0, 5, 0)
            };
            
            cdnList = new List<string> 
            {
                "level3.blizzard.com",      // Level3
                "eu.cdn.blizzard.com",      // Official EU CDN
                "blzddist1-a.akamaihd.net", // Akamai first
                "cdn.blizzard.com",         // Official regionless CDN
                "us.cdn.blizzard.com",      // Official US CDN
                "blizzard.nefficient.co.kr" // Korea 
            };
        }

        public void LoadCdnsFile(TactProduct currentProduct)
        {
            // Loading CDNs
            var cdnFileHandler = new CdnFileHandler(this);
            //TODO assign _cdnsFile.entries[0].path to a property instead of looking it up every time.
            _cdnsFile = cdnFileHandler.ParseCdnsFile(currentProduct);

            // Adds any missing cdn hosts
            foreach (var host in _cdnsFile.entries.SelectMany(e => e.hosts))
            {
                if (!cdnList.Contains(host))
                {
                    cdnList.Add(host);
                }
            }

            _cachedStringLookup = new CachedStringLookup(_cdnsFile);
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
                uri = $"{_cdnsFile.entries[0].path}/{rootPath.Name}/{hashString[0]}{hashString[1]}/{hashString[2]}{hashString[3]}/{hashString}";
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
                    uri = $"{_cdnsFile.entries[0].path}/{rootPath.Name}/{hashId[0]}{hashId[1]}/{hashId[2]}{hashId[3]}/{hashId}";
                    _queuedRequestLookupTable.Add(hashId, uri);
                }
            }
            else
            {
                uri = $"{_cdnsFile.entries[0].path}/{rootPath.Name}/{hashId[0]}{hashId[1]}/{hashId[2]}{hashId[3]}/{hashId}";
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

        //TODO rename to GetWholeFile
        public byte[] Get(RootFolder rootPath, string hashId, bool writeToDevNull = false)
        {
            //TODO remove this ToLower() call
            hashId = hashId.ToLower();
            var uri = $"{_cdnsFile.entries[0].path}/{rootPath.Name}/{hashId.Substring(0, 2)}/{hashId.Substring(2, 2)}/{hashId}";
            return Get(uri, writeToDevNull);
        }

        //TODO merge this with Get(), and add a flag as an option to get the index
        public byte[] GetIndex(RootFolder rootPath, string hashId)
        {
            //TODO indexes should have a size
            var uri = $"{_cdnsFile.entries[0].path}/{rootPath.Name}/{hashId.Substring(0, 2)}/{hashId.Substring(2, 2)}/{hashId}.index";
            return Get(uri);
        }

        //TODO nicer progress bar
        //TODO this doesn't max out my connection at 300mbs
        public void DownloadQueuedRequests()
        {
            var timer = Stopwatch.StartNew();

			//TODO log time that coalescing takes.  Figure out how many requests there are before and after.
			//TODO need to calculate the actual file size, for full file downloads.
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
                    Get(entry.Uri, writeToDevNull: entry.WriteToDevNull);
                }
                else
                {
                    Get(entry.Uri, writeToDevNull: entry.WriteToDevNull, entry.LowerByteRange, entry.UpperByteRange);
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

        //TODO comment
        private byte[] Get(string requestPath, bool writeToDevNull = false, long? startBytes = null, long? endBytes = null)
        {
            // Record the requests we're making, so we can use it for debugging
            if (startBytes != null && endBytes != null)
            {
                allRequestsMade.Add(new Request
                {
                    Uri = requestPath,
                    LowerByteRange = startBytes.Value,
                    UpperByteRange = endBytes.Value
                });
            }
            else
            {
                allRequestsMade.Add(new Request
                {
                    Uri = requestPath,
                    DownloadWholeFile = true
                });
            }
            
            // When we are running in debug mode, we can skip entirely any requests that will end up written to dev/null.  Will speed up debugging.
            if (DebugMode && writeToDevNull)
            {
                return null;
            }

            // TODO cache this in a dict
            var uri = new Uri($"http://{cdnList[0]}/{requestPath}");

            if (!writeToDevNull)
            {
                string outputFilePath = Path.Combine(Config.CacheDir + uri.AbsolutePath);
                if (File.Exists(outputFilePath))
                {
                    return File.ReadAllBytes(outputFilePath);
                }
            }
            
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            if (startBytes != null && endBytes != null)
            {
                requestMessage.Headers.Range = new RangeHeaderValue(startBytes, endBytes);
            }

            //TODO Handle "The response ended prematurely" exceptions.  Maybe add them to the queue again to be retried?
            using HttpResponseMessage response = client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead).Result;
            using Stream responseStream = response.Content.ReadAsStreamAsync().Result;

            if (response.IsSuccessStatusCode)
            {
                if(writeToDevNull)
                {
                    try
                    {
                        // Dump the received data, so we don't have to waste time writing it to disk.
                        responseStream.CopyToAsync(Stream.Null).Wait();
                    }
                    catch (Exception e)
                    {
                        //Console.WriteLine(e);
                        Console.WriteLine(Colors.Red($"Error downloading : {uri.ToString()} {startBytes}-{endBytes}"));
                        //throw;
                    }
                    
                    return null;
                }
                else
                {
                    using var memoryStream = new MemoryStream();
                    responseStream.CopyToAsync(memoryStream).Wait();

                    var byteArray = memoryStream.ToArray();
                    
                    // Cache to disk
                    string outputFilePath = Path.Combine(Config.CacheDir + uri.AbsolutePath);
                    FileInfo file = new FileInfo(outputFilePath);
                    file.Directory.Create();
                    File.WriteAllBytes(file.FullName, byteArray);

                    return byteArray;
                }
            }
            else
            {
                throw new FileNotFoundException($"Error retrieving file: HTTP status code {response.StatusCode} on URL http://{cdnList[0]}/{requestPath.ToLower()}");
            }

            Console.WriteLine($"Exhausted all CDNs looking for file {Path.GetFileNameWithoutExtension(requestPath)}, cannot retrieve it!");
            return Array.Empty<byte>();
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