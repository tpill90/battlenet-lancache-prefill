using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace RequestReplayer
{
    public class Downloader
    {
        private readonly string _blizzardCdnBaseUri;

        private readonly HttpClient client = new HttpClient();

        public int FileNotFoundCount;
        public int FailureCount;

        public Downloader(string blizzardCdnBaseUri)
        {
            _blizzardCdnBaseUri = blizzardCdnBaseUri;

            client.Timeout = new TimeSpan(0, 5, 0);
        }

        public async Task DownloadAsync(string path, long startBytes, long endBytes, bool downloadWholeFile)
        {
            var uri = new Uri($"{_blizzardCdnBaseUri}{path.ToLower()}");

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
                    FileNotFoundCount++;
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
                    FailureCount++;
                }
                else
                {
                    throw;
                }
            }
        }
    }
}