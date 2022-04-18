using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BuildBackup.DebugUtil.Models;
using ByteSizeLib;
using Shared;
using ShellProgressBar;

namespace RequestReplayer
{
    //TODO comment class
    public class Downloader
    {
        private readonly string _blizzardCdnBaseUri;

        private readonly HttpClient _client = new HttpClient();

        private int _fileNotFoundCount;
        private int _failureCount;

        private long _totalBytesRead;
        private long _bufferSize = 4096 * 2;
        private long readCount;

        private int _maxConcurrentDownloads = 6;

        private Stopwatch _elapsedDownloadTime;
        private ByteSize _totalDownloadSize;

        public Downloader(string blizzardCdnBaseUri)
        {
            _blizzardCdnBaseUri = blizzardCdnBaseUri;

            _client.Timeout = new TimeSpan(0, 5, 0);
        }

        /// <summary>
        /// Takes a list of requests, and "replays" them by re-requesting the data.  No data from the request is saved, as the intention of replaying the requests
        /// is to re-prime the LanCache. 
        /// </summary>
        /// <param name="requestsToReplay"></param>
        public void DownloadRequestsParallel(List<Request> requestsToReplay)
        {
            _totalDownloadSize = ByteSize.FromBytes(requestsToReplay.Sum(e => e.TotalBytes));
            _elapsedDownloadTime = Stopwatch.StartNew();
            
            // Setting up the progress bar
            int maxTicks = (int)((long)_totalDownloadSize.Bytes / _bufferSize);
            using var progressBar = new ProgressBar(maxTicks, message: $"{_totalDownloadSize} remaining..", new ProgressBarOptions {ProgressBarOnBottom = true});

            // Download the files and update onscreen status
            Parallel.ForEach(requestsToReplay, new ParallelOptions { MaxDegreeOfParallelism = _maxConcurrentDownloads }, request =>
            {
                DownloadAsync(request, progressBar).Wait();
            });
            progressBar.Tick(maxTicks, "Done!");
            progressBar.Dispose();
        }

        private async Task DownloadAsync(Request request, ProgressBar progressBar)
        {
            var requestUri = new Uri($"{_blizzardCdnBaseUri}/{request}");

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
            if (!request.DownloadWholeFile)
            {
                requestMessage.Headers.Range = new RangeHeaderValue(request.LowerByteRange, request.UpperByteRange);
            }
            
            using var response = await _client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            await using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            try
            {
                if (response.IsSuccessStatusCode)
                {
                    await ProcessContentStream(contentStream, progressBar);
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _fileNotFoundCount++;
                }
                else
                {
                    throw new FileNotFoundException($"Error retrieving file: HTTP status code {response.StatusCode} on URL ");
                }
            }
            catch (IOException e)
            {
                if (e.Message.Contains("ended prematurely"))
                {
                    _failureCount++;
                }
                else
                {
                    throw;
                }
            }
            
        }

        private async Task ProcessContentStream(Stream contentStream, ProgressBar progressBar)
        {
            var buffer = new byte[_bufferSize];
            var isMoreToRead = true;
            do
            {
                var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    isMoreToRead = false;
                    continue;
                }
                _totalBytesRead += bytesRead;
                readCount++;

                // Dump the received data to null, so we don't have to waste time writing it to disk.
                await Stream.Null.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                RefreshProgressBar(progressBar);

            } while (isMoreToRead);
            
        }

        private void RefreshProgressBar(ProgressBar progressBar)
        {
            // Reduces the number of times that the progress bar updates, to reduce jitter
            if (readCount % 7000 != 0)
            {
                return;
            }

            var remainingBytes = ByteSize.FromBytes(_totalDownloadSize.Bytes - _totalBytesRead);
            // Bytes/second
            var transferRateBytes = ByteSize.FromBytes(_totalBytesRead / _elapsedDownloadTime.Elapsed.TotalSeconds);

            progressBar.Tick((int) (_totalBytesRead / _bufferSize), $"{remainingBytes.GigaBytes,7:0.00} GB remaining -- {transferRateBytes}/s");
        }

        public void PrintStatistics()
        {
            if (_failureCount > 0)
            {
                Console.WriteLine($"     Total Errors : {Colors.Red(_failureCount)}");
            }
            if (_fileNotFoundCount > 0)
            {
                Console.WriteLine($"     Total files not found : {Colors.Yellow(_fileNotFoundCount)}");
            }
        }
    }
}