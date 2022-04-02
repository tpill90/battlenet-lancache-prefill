using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json;
using Shared;

namespace BuildBackup.DebugUtil
{
    //TODO comment the purpose of this class
    public class FileSizeProvider
    {
        private readonly TactProduct _targetProduct;
        private HttpClient _client = new HttpClient();

        private ConcurrentDictionary<string, long> _cachedContentLengths;
        private int _cacheMisses = 0;

        //TODO make all cache files point to the /cache directory
        private string _cacheDir = "cache/cachedContentLengths";
        private string CachedFileName => $"{_cacheDir}/{_targetProduct.ProductCode}.json";
        private object _cacheFileLock = new object();

        public FileSizeProvider(TactProduct targetProduct)
        {
            _targetProduct = targetProduct;
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

        public long GetContentLength(Uri uri)
        {
            if (_cachedContentLengths.ContainsKey(uri.ToString()))
            {
                return _cachedContentLengths[uri.ToString()];
            }

            var response = _client.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri)).Result;
            var contentLength = response.Content.Headers.ContentLength.Value;

            _cachedContentLengths.TryAdd(uri.ToString(), contentLength);
            _cacheMisses++;

            if (_cacheMisses == 100)
            {
                Save();
            }

            return contentLength;
        }
    }
}
