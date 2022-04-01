using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Web;

namespace BattleNetPrefill.Parsers
{
    public static class CdnsFileParser
    {
        public static async Task<CdnsFile> ParseCdnsFileAsync(CDN cdn, TactProduct targetProduct)
        {
            string content = await cdn.MakePatchRequestAsync(targetProduct, "cdns");

            CdnsFile cdns = new CdnsFile();

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
                                //TODO
                                //Console.WriteLine("!!!!!!!! Unknown cdns variable '" + friendlyName + "'");
                                break;
                        }
                    }
                }

            }

            if (cdns.entries == null || !cdns.entries.Any())
            {
                throw new Exception($"Invalid CDNs file for {targetProduct.DisplayName}, skipping!");
            }

            return cdns;
        }
    }
}
