using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using BattleNetPrefill.Utils.Debug.Models;
using Newtonsoft.Json;

namespace BattleNetPrefill.Utils.Debug
{
    /// <summary>
    /// The majority of requests made by Battle.Net specify a byte range, ex 0-100.
    /// However not all requests for all products do this, some products will actually request the whole file from the CDN.
    ///
    /// The purpose of this class, is that it will figure out the response size of these "whole file" requests, and cache them for future use.
    /// This greatly helps with debugging, as we will be able to compare range requests against range requests, without having to implement "whole file" comparison logic.
    /// </summary>
    public class FileSizeProvider
    {
        private readonly TactProduct _targetProduct;
        private readonly string _blizzardCdnBaseUri;

        private readonly HttpClient _client = new HttpClient();

        private readonly ConcurrentDictionary<string, long> _cachedContentLengths;
        private int _cacheMisses;
        
        private string _cacheDir = "cache/cachedContentLengths";
        private string CachedFileName => $"{_cacheDir}/{_targetProduct.ProductCode}.json";
        private object _cacheFileLock = new object();

        public FileSizeProvider(TactProduct targetProduct, string baseCdnUri)
        {
            _targetProduct = targetProduct;
            _blizzardCdnBaseUri = baseCdnUri;
            if(!Directory.Exists(_cacheDir))
            {
                Directory.CreateDirectory(_cacheDir);
            }
            if (File.Exists(CachedFileName))
            {
                _cachedContentLengths = JsonConvert.DeserializeObject<ConcurrentDictionary<string, long>>(File.ReadAllText(CachedFileName));
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
                File.WriteAllText(CachedFileName, JsonConvert.SerializeObject(_cachedContentLengths));
            }
        }

        public bool HasBeenCached(Request request)
        {
            return _cachedContentLengths.ContainsKey(request.Uri);
        }

        public async Task<long> GetContentLengthAsync(Request request)
        {
            if (_cachedContentLengths.ContainsKey(request.Uri))
            {
                return _cachedContentLengths[request.Uri];
            }

            var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Head, new Uri($"{_blizzardCdnBaseUri}/{request.Uri}")));
            var contentLength = response.Content.Headers.ContentLength.Value;

            _cachedContentLengths.TryAdd(request.Uri, contentLength);
            _cacheMisses++;

            if (_cacheMisses == 100)
            {
                Save();
            }

            return contentLength;
        }
    }
}
