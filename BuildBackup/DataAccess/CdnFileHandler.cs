using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using BuildBackup.Structs;
using Newtonsoft.Json;
using Shared;

namespace BuildBackup.DataAccess
{
    public class CdnFileHandler
    {
        private CDN cdn;
        private readonly Uri _battleNetPatchUri;

        public CdnFileHandler(CDN cdn, Uri battleNetPatchUri)
        {
            this.cdn = cdn;
            _battleNetPatchUri = battleNetPatchUri;
        }

        public CdnsFile ParseCdnsFile(TactProduct tactProduct)
        {
            var timer2 = Stopwatch.StartNew();
            var cdns = GetCDNs(tactProduct);
            Console.WriteLine($"GetCDNs loaded in {Colors.Yellow(timer2.Elapsed.ToString(@"mm\:ss\.FFFF"))}");

            return cdns;
        }

        //TODO should this be part of the CDN class?
        public CdnsFile GetCDNs(TactProduct tactProduct)
        {
            var cacheFile = $"{Config.CacheDir}/cdns-{tactProduct.ProductCode}.json";

            // Load cached version, only valid for 1 hour
            if (File.Exists(cacheFile) && DateTime.Now < File.GetLastWriteTime(cacheFile).AddHours(1))
            {
                return JsonConvert.DeserializeObject<CdnsFile>(File.ReadAllText(cacheFile));
            }

            string content;

            CdnsFile cdns = new CdnsFile();

            using (HttpResponseMessage response = cdn.client.GetAsync(new Uri($"{_battleNetPatchUri}{tactProduct.ProductCode}/cdns")).Result)
            {
                if (response.IsSuccessStatusCode)
                {
                    using HttpContent res = response.Content;
                    content = res.ReadAsStringAsync().Result;
                }
                else
                {
                    throw new Exception("Error during retrieving HTTP cdns: Received bad HTTP code " + response.StatusCode);
                }
            }

            var lines = content.Split(new string[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

            var lineList = new List<string>();

            for (var i = 0; i < lines.Count(); i++)
            {
                if (lines[i][0] != '#')
                {
                    lineList.Add(lines[i]);
                }
            }

            lines = lineList.ToArray();

            if (lines.Count() > 0)
            {
                cdns.entries = new CdnsEntry[lines.Count() - 1];

                var cols = lines[0].Split('|');

                for (var c = 0; c < cols.Count(); c++)
                {
                    var friendlyName = cols[c].Split('!').ElementAt(0);

                    for (var i = 1; i < lines.Count(); i++)
                    {
                        var row = lines[i].Split('|');

                        switch (friendlyName)
                        {
                            case "Name":
                                cdns.entries[i - 1].name = row[c];
                                break;
                            case "Path":
                                cdns.entries[i - 1].path = row[c];
                                break;
                            case "Hosts":
                                var hosts = row[c].Split(' ');
                                cdns.entries[i - 1].hosts = new string[hosts.Count()];
                                for (var h = 0; h < hosts.Count(); h++)
                                {
                                    cdns.entries[i - 1].hosts[h] = hosts[h];
                                }
                                break;
                            case "ConfigPath":
                                cdns.entries[i - 1].configPath = row[c];
                                break;
                            default:
                                //Console.WriteLine("!!!!!!!! Unknown cdns variable '" + friendlyName + "'");
                                break;
                        }
                    }
                }

                foreach (var subcdn in cdns.entries)
                {
                    foreach (var cdnHost in subcdn.hosts)
                    {
                        if (!cdn.cdnList.Contains(cdnHost))
                        {
                            cdn.cdnList.Add(cdnHost);
                        }
                    }
                }
            }

            if (cdns.entries == null || !cdns.entries.Any())
            {
                Console.WriteLine($"Invalid CDNs file for {tactProduct.DisplayName}, skipping!");
                throw new Exception($"Invalid CDNs file for {tactProduct.DisplayName}, skipping!");
            }
            Console.WriteLine($"Loaded {Colors.Cyan(cdns.entries.Count())} CDNs");

            // Writes results to disk, to be used as cache later
            File.WriteAllText(cacheFile, JsonConvert.SerializeObject(cdns));

            return cdns;
        }
    }
}
