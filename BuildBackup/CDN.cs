using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BuildBackup.Utils;
using Shared.Models;

namespace BuildBackup
{
    public class CDN
    {
        //TODO make these all private
        public readonly HttpClient client;

        public List<string> cdnList;

        //TODO break these requests out into a different class later
        public ConcurrentBag<Request> allRequestsMade = new ConcurrentBag<Request>();

        /// <summary>
        /// When set to true, will skip any requests where the response is not required.  This can be used to dramatically speed up debugging time, as
        /// you won't need to wait for the full file transfer to complete.
        /// </summary>
        public bool DebugMode = false;

        public CDN()
        {
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

        //TODO comment
        public byte[] Get(string rootPath, string hashId, bool writeToDevNull = false, [CallerMemberName] string callerName = "", [CallerFilePath] string callerFile = "")
        {
            hashId = hashId.ToLower();
            var uri = $"{rootPath}{hashId.Substring(0, 2)}/{hashId.Substring(2, 2)}/{hashId}";
            return Get(uri, writeToDevNull, callingMethod: $"{Path.GetFileName(callerFile)} - {callerName}");
        }

        //TODO comment
        public byte[] GetIndex(string rootPath, string id, [CallerMemberName] string callerName = "", [CallerFilePath] string callerFile = "")
        {
            var uri = $"{rootPath}{id.Substring(0, 2)}/{id.Substring(2, 2)}/{id}.index";
            return Get(uri, callingMethod: $"{Path.GetFileName(callerFile)} - {callerName}");
        }

        public void GetByteRange(string rootPath, string id, long start, long end, bool writeToDevNull, [CallerMemberName] string callerName = "", [CallerFilePath] string callerFile = "")
        {
            var uri = $"{rootPath}{id.Substring(0, 2)}/{id.Substring(2, 2)}/{id}";
            Get(uri, writeToDevNull, start, end, callingMethod: $"{Path.GetFileName(callerFile)} - {callerName}");
        }

        //TODO comment
        private byte[] Get(string requestPath, bool writeToDevNull = false, long? startBytes = null, long? endBytes = null, string callingMethod = null)
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
                    UpperByteRange = endBytes.Value,
                    CallingMethod = callingMethod
                });
            }
            else
            {
                allRequestsMade.Add(new Request
                {
                    Uri = requestPath,
                    DownloadWholeFile = true,
                    CallingMethod = callingMethod
                });
            }

            // TODO comment
            if (DebugMode && writeToDevNull)
            {
                return null;
            }

            // Attempts to search for the file through each known cdn
            foreach (var cdn in cdnList)
            {
                var uri = new Uri($"http://{cdn}/{requestPath}");

                //if (DebugMode)
                //{
                //    string outputFilePath = Path.Combine("cache" + uri.AbsolutePath);
                //    if (!Directory.Exists(outputFilePath))
                //    {
                //        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
                //    }

                //    //TODO I don't want this to run, unless I'm in "debug" mode.
                //    if (File.Exists(outputFilePath))
                //    {
                //        return File.ReadAllBytes(outputFilePath);
                //    }
                //}
                
                try
                {
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

                            //if (DebugMode)
                            //{
                            //    string outputFilePath = Path.Combine("cache" + uri.AbsolutePath);
                            //    File.WriteAllBytes(outputFilePath, byteArray);
                            //}
                            
                            return byteArray;
                        }
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        Logger.WriteLine($"File not found on CDN {cdn} trying next CDN (if available)..");
                    }
                    else
                    {
                        throw new FileNotFoundException("Error retrieving file: HTTP status code " + response.StatusCode + " on URL " + $"http://{cdn}/{requestPath.ToLower()}");
                    }
                }
                catch (TaskCanceledException e)
                {
                    if (!e.CancellationToken.IsCancellationRequested)
                    {
                        Logger.WriteLine("!!! Timeout while retrieving file " + $"http://{cdn}/{requestPath.ToLower()}");
                    }
                }
                catch (Exception e)
                {
                    Logger.WriteLine("!!! Error retrieving file " + $"http://{cdn}/{requestPath.ToLower()}" + ": " + e.Message);
                }
            }
            Logger.WriteLine($"Exhausted all CDNs looking for file {Path.GetFileNameWithoutExtension(requestPath)}, cannot retrieve it!", true);
            return Array.Empty<byte>();
        }
    }
}