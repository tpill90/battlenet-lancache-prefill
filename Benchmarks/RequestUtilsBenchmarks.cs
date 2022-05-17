using System.Collections.Generic;
using System.IO;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Utils.Debug;
using BattleNetPrefill.Utils.Debug.Models;
using BenchmarkDotNet.Attributes;
using Utf8Json;
using Utf8Json.Resolvers;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class RequestUtilsBenchmarks
    {
        private List<Request> requestsToCoalesce;
        private Dictionary<MD5Hash, List<Request>> requestsToCoalesceDict = new Dictionary<MD5Hash, List<Request>>();

        public RequestUtilsBenchmarks()
        {
            var filePath = @"C:\Users\Tim\Dropbox\Programming\dotnet-public\queuedRequests.json";
            var allText = File.ReadAllText(filePath);

            var DefaultUtf8JsonResolver = CompositeResolver.Create(new IJsonFormatter[] { new RootFolderFormatter() }, new[] { StandardResolver.Default });
            requestsToCoalesce = JsonSerializer.Deserialize<List<Request>>(allText, DefaultUtf8JsonResolver);

            foreach (var request in requestsToCoalesce)
            {
                List<Request> requests;
                var hash = request.CdnKey;
                requestsToCoalesceDict.TryGetValue(hash, out requests);
                if (requests == null)
                {
                    requests = new List<Request>();
                    requestsToCoalesceDict.Add(hash, requests);
                }
                requests.Add(request);
            }
            
        }

        //[Benchmark(Baseline = true)]
        //public List<Request> Original()
        //{
        //    return RequestUtils_Original.CoalesceRequests(requestsToCoalesce);
        //}

        [Benchmark]
        public List<Request> Dictionary()
        {
            return RequestUtils.CoalesceRequests(requestsToCoalesceDict);
        }

    }
}
