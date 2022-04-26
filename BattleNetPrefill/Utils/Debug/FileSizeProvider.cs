using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using BattleNetPrefill.Utils.Debug.Models;
using Newtonsoft.Json;

namespace BattleNetPrefill.Utils.Debug
{
    //TODO comment the purpose of this class
    public class FileSizeProvider
    {
        private readonly TactProduct _targetProduct;
        private readonly string _blizzardCdnBaseUri;
        private HttpClient _client = new HttpClient();

        private ConcurrentDictionary<string, long> _cachedContentLengths;
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
            return _cachedContentLengths.ContainsKey(request.Uri2);
        }

        public async Task<long> GetContentLengthAsync(Request request)
        {
            if (_cachedContentLengths.ContainsKey(request.Uri2))
            {
                return _cachedContentLengths[request.Uri2];
            }

            var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Head, new Uri($"{_blizzardCdnBaseUri}/{request.Uri2}")));
            var contentLength = response.Content.Headers.ContentLength.Value;

            _cachedContentLengths.TryAdd(request.Uri2, contentLength);
            _cacheMisses++;

            if (_cacheMisses == 100)
            {
                Save();
            }

            return contentLength;
        }
    }
}
