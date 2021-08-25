using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ByteSizeLib;
using Konsole;
using Shared;
using Shared.Models;
using Colors = Shared.Colors;

namespace RequestReplayer
{
    public static class Program
    {
        private static readonly string BlizzardCdnBaseUri = "http://level3.blizzard.com";
        private static readonly string LogFileBasePath = @"C:\Users\Tim\Dropbox\Programming\dotnet-public\BattleNetBackup\RequestReplayer\Logs";

        public static TactProduct[] targetProducts = new TactProduct[] { TactProducts.CodWarzone, TactProducts.Starcraft2, TactProducts.Diablo3 };

        private static readonly HttpClient client = new HttpClient();
        public static int FailureCount;

        public static void Main()
        {
            client.Timeout = new TimeSpan(0, 5, 0);

            foreach (var targetProduct in targetProducts)
            {
                Console.WriteLine($"Parsing saved logs for {Colors.Cyan(targetProduct.DisplayName)}");
                var timer = Stopwatch.StartNew();
                var requestsToReplay = NginxLogParser.ParseRequestLogs(LogFileBasePath, targetProduct)
                                                     .OrderByDescending(e => e.TotalBytes)
                                                     .ToList();
                timer.Stop();
                Console.WriteLine($"     Completed log loading in {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");

                Console.WriteLine($"Starting request replay for {Colors.Yellow(targetProduct.DisplayName)}!");

                CalculateDownloadSize(requestsToReplay);

                //DownloadSequentially(requestsToReplay);
                DownloadParallel(requestsToReplay);
            }

            Console.WriteLine($"Total Errors {FailureCount}");
            Console.WriteLine("Done");
            Console.ReadLine();
        }

        private static void CalculateDownloadSize(List<Request> requestsToReplay)
        {
            //TODO this doesn't handle whole file requests
            var downloadSize = ByteSize.FromBytes(requestsToReplay.Sum(e => e.TotalBytes)).GigaBytes.ToString("##.##");
            Console.WriteLine($"     Downloading a total of {Colors.Magenta(downloadSize + "gb")}");
        }

        private static void DownloadParallel(List<Request> requestsToReplay)
        {
            var progressBar = new ProgressBar(PbStyle.DoubleLine, requestsToReplay.Count);
            int count = 0;
            Parallel.ForEach(requestsToReplay, new ParallelOptions {MaxDegreeOfParallelism = 15}, (request) =>
            {
                DownloadAsync("/" + request.Uri, request.LowerByteRange, request.UpperByteRange, request.DownloadWholeFile).Wait();
                progressBar.Refresh(count, $"{request.ToString()}");
                count++;
            });
            progressBar.Refresh(count, "     Done!");
        }

        public static void DownloadSequentially(List<Request> requestsToReplay)
        {
            for (var index = 0; index < requestsToReplay.Count; index++)
            {
                var request = requestsToReplay[index];
                Console.WriteLine($"{index} of {requestsToReplay.Count} - {request.ToString()}");
                //TODO fix this pathing
                DownloadAsync($"/{request.Uri}", request.LowerByteRange, request.UpperByteRange, request.DownloadWholeFile).Wait();
            }
        }

        public static void DownloadTaskBased(List<Request> requestsToReplay)
        {
            //TODO figure out why async task creation takes so long
            List<Task> requestTasks = new List<Task>();
            var timer = Stopwatch.StartNew();

            foreach (var request in requestsToReplay)
            {
                requestTasks.Add(DownloadAsync("/" + request.Uri, request.LowerByteRange, request.UpperByteRange, request.DownloadWholeFile));
            }

            timer.Stop();
            Console.WriteLine(timer.Elapsed.ToString());

            //using (var progressBar = new ProgressBar(requestsToReplay.Count(), $"Replaying {requestsToReplay.Count()} requests..."))
            //{
            //    while (requestTasks.Any(e => !e.IsCompleted))
            //    {
            //        Thread.Sleep(2000);
            //        var ticks = requestTasks.Count(e => e.IsCompleted) - progressBar.CurrentTick;
            //        for (int i = 0; i < ticks; i++)
            //        {
            //            progressBar.Tick();
            //        }
            //    }

            //    Task.WhenAll(requestTasks).Wait();
            //}
        }

        public static async Task DownloadAsync(string path, long startBytes, long endBytes, bool downloadWholeFile)
        {
            var uri = new Uri($"{BlizzardCdnBaseUri}{path.ToLower()}");

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            if (!downloadWholeFile)
            {
                requestMessage.Headers.Range = new RangeHeaderValue(startBytes, endBytes);
            }
            
            using HttpResponseMessage response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            using Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            try
            {
                if (response.IsSuccessStatusCode)
                {
                    // Dump the received data, so we don't have to waste time writing it to disk.
                    await responseStream.CopyToAsync(Stream.Null).ConfigureAwait(false);
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    Console.WriteLine($"File not found. {path}");
                }
                else
                {
                    throw new FileNotFoundException($"Error retrieving file: HTTP status code {response.StatusCode} on URL ");
                }
            }
            catch (Exception e)
            {
                FailureCount++;
                //TODO 
                Console.WriteLine(e);
            }
        }
    }
}
