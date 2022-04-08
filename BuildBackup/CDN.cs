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
using ByteSizeLib;
using Konsole;
using Colors = Shared.Colors;

namespace BuildBackup
{
    //TODO rename to something like HttpRequestHandler
    public class CDN
    {
        private readonly IConsole _console;

        //TODO make these all private
        public readonly HttpClient client;

        public List<string> cdnList;

        //TODO break these requests out into a different class later
        public ConcurrentBag<Request> allRequestsMade = new ConcurrentBag<Request>();

        private readonly List<Request> _queuedRequests = new List<Request>();

        /// <summary>
        /// When set to true, will skip any requests where the response is not required.  This can be used to dramatically speed up debugging time, as
        /// you won't need to wait for the full file transfer to complete.
        /// </summary>
        public bool DebugMode = false;

        public CDN(IConsole console)
        {
            _console = console;
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

        //TODO finish making everything use this
        public void QueueRequest(string rootPath, string hashId, long? startBytes = null, long? endBytes = null, bool writeToDevNull = false)
        {
            hashId = hashId.ToLower();
            var uri = $"{rootPath}{hashId.Substring(0, 2)}/{hashId.Substring(2, 2)}/{hashId}";

            if (startBytes != null && endBytes != null)
            {
                _queuedRequests.Add(new Request
                {
                    Uri = uri,
                    LowerByteRange = startBytes.Value,
                    UpperByteRange = endBytes.Value,
                    WriteToDevNull = writeToDevNull
                });
            }
            else
            {
                _queuedRequests.Add(new Request
                {
                    Uri = uri,
                    DownloadWholeFile = true,
                    WriteToDevNull = writeToDevNull
                });
            }
        }

        public byte[] Get(string rootPath, string hashId, bool writeToDevNull = false)
        {
            hashId = hashId.ToLower();
            var uri = $"{rootPath}{hashId.Substring(0, 2)}/{hashId.Substring(2, 2)}/{hashId}";
            return Get(uri, writeToDevNull);
        }
        
        public byte[] GetIndex(string rootPath, string hashId)
        {
            var uri = $"{rootPath}{hashId.Substring(0, 2)}/{hashId.Substring(2, 2)}/{hashId}.index";
            return Get(uri);
        }

        public void DownloadQueuedRequests()
        {
            int count = 0;
            var timer = Stopwatch.StartNew();

			//TODO log time that coalescing takes.  Figure out how many requests there are before and after.
			//TODO need to calculate the actual file size, for full file downloads.

            var coalesced = NginxLogParser.CoalesceRequests(_queuedRequests).ToList();
            Console.WriteLine($"Downloading {Colors.Cyan(coalesced.Count)} total queued requests " +
                              $"Totaling {Colors.Magenta(ByteSize.FromBytes(coalesced.Sum(e => e.TotalBytes)))}");

            var progressBar = new ProgressBar(_console, PbStyle.SingleLine, coalesced.Count, 50);
            Parallel.ForEach(coalesced, new ParallelOptions { MaxDegreeOfParallelism = 20 }, entry =>
            {
                if (entry.DownloadWholeFile)
                {
                    Get(entry.Uri, entry.WriteToDevNull, startBytes: null, null);
                }
                else
                {
                    Get(entry.Uri, writeToDevNull: entry.WriteToDevNull, startBytes: entry.LowerByteRange, entry.UpperByteRange);
                }

                if (!DebugMode)
                {
                    // Skip refreshing the progress bar when debugging.  Slows things down
                    progressBar.Refresh(count, $"     ");
                }
                
                count++;
            });

            timer.Stop();
            progressBar.Refresh(count, $"     Done! {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");
        }

        //TODO comment
        private byte[] Get(string requestPath, bool writeToDevNull = false, long? startBytes = null, long? endBytes = null)
        {
            if (startBytes != null && endBytes == null)
            {
                throw new ArgumentException("Invalid parameters : endBytes is null when startBytes is not");
            }
            if (startBytes == null && endBytes != null)
            {
                throw new ArgumentException("Invalid parameters : startBytes is null when endBytes is not");
            }

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

            // Attempts to search for the file through each known cdn
            foreach (var cdn in cdnList)
            {
                var uri = new Uri($"http://{cdn}/{requestPath}");

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

                using HttpResponseMessage response = client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead).Result;
                using Stream responseStream = response.Content.ReadAsStreamAsync().Result;

                if (response.IsSuccessStatusCode)
                {
                    if(writeToDevNull)
                    {
                        // Dump the received data, so we don't have to waste time writing it to disk.
                        responseStream.CopyToAsync(Stream.Null).Wait();
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
                    throw new FileNotFoundException($"Error retrieving file: HTTP status code {response.StatusCode} on URL http://{cdn}/{requestPath.ToLower()}");
                }
            }

            Console.WriteLine($"Exhausted all CDNs looking for file {Path.GetFileNameWithoutExtension(requestPath)}, cannot retrieve it!");
            return Array.Empty<byte>();
        }
    }
}