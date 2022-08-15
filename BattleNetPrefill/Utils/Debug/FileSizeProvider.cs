namespace BattleNetPrefill.Utils.Debug
{
    /// <summary>
    /// The majority of requests made by Battle.Net specify a byte range, ex 0-100.
    /// However not all requests for all products do this, some products will actually request the whole file from the CDN.
    ///
    /// The purpose of this class, is that it will figure out the response size of these "whole file" requests, and cache them for future use.
    /// This greatly helps with debugging, as we will be able to compare range requests against range requests, without having to implement "whole file" comparison logic.
    /// </summary>
    public sealed class FileSizeProvider : IDisposable
    {
        private readonly TactProduct _targetProduct;
        private readonly string _blizzardCdnBaseUrl;

        private readonly HttpClient _client = new HttpClient();

        private readonly ConcurrentDictionary<string, long> _cachedContentLengths;
        private int _cacheMisses;
        
        private string _cacheDir = "cache/cachedContentLengths";
        private string CachedFileName => $"{_cacheDir}/{_targetProduct.ProductCode}.json";
        private object _cacheFileLock = new object();

        public FileSizeProvider(TactProduct targetProduct, string baseCdnUrl)
        {
            _targetProduct = targetProduct;
            _blizzardCdnBaseUrl = baseCdnUrl;
            if(!Directory.Exists(_cacheDir))
            {
                Directory.CreateDirectory(_cacheDir);
            }
            if (File.Exists(CachedFileName))
            {
                _cachedContentLengths = JsonSerializer.Deserialize(File.ReadAllText(CachedFileName), Structs.Enums.SerializationContext.Default.ConcurrentDictionaryStringInt64);
                return;
            }

            _cachedContentLengths = new ConcurrentDictionary<string, long>();
        }

        /// <summary>
        /// Saves current cache to disk
        /// </summary>
        public void Save()
        {
            lock (_cacheFileLock)
            {
                _cacheMisses = 0;
                File.WriteAllText(CachedFileName, JsonSerializer.Serialize(_cachedContentLengths, Structs.Enums.SerializationContext.Default.ConcurrentDictionaryStringInt64));
            }
        }

        public async Task<long> GetContentLengthAsync(Request request)
        {
            if (_cachedContentLengths.ContainsKey(request.Uri))
            {
                return _cachedContentLengths[request.Uri];
            }

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Head, new Uri($"{_blizzardCdnBaseUrl}/{request.Uri}"));
            var response = await _client.SendAsync(httpRequestMessage);
            var contentLength = response.Content.Headers.ContentLength.Value;

            _cachedContentLengths.TryAdd(request.Uri, contentLength);
            _cacheMisses++;

            if (_cacheMisses == 100)
            {
                Save();
            }

            return contentLength;
        }

        public async Task PopulateRequestSizesAsync(List<Request> requests)
        {
            foreach (var request in requests)
            {
                if (!request.DownloadWholeFile)
                {
                    continue;
                }

                request.DownloadWholeFile = false;
                request.LowerByteRange = 0;
                // Subtracting 1, because byte ranges are "inclusive".  Ex range 0-9 == 10 bytes length.
                var contentLength = await GetContentLengthAsync(request);
                request.UpperByteRange = contentLength - 1;
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
