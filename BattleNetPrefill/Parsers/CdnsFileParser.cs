namespace BattleNetPrefill.Parsers
{
    public static class CdnsFileParser
    {
        public static async Task<CdnsFile> ParseCdnsFileAsync(CdnRequestManager cdnRequestManager, TactProduct targetProduct)
        {
            string content = await cdnRequestManager.MakePatchRequestAsync(targetProduct, PatchRequest.cdns);
            var lines = content.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                               .Where(e => e[0] != '#')
                               .ToArray();

            if (!lines.Any())
            {
                throw new Exception($"Unexpected empty CDNs file for {targetProduct.DisplayName}.  CDNs file cannot be empty!");
            }

            var cdns = new CdnsFile
            {
                entries = new CdnsEntry[lines.Length - 1]
            };

            var cols = lines[0].Split('|').Select(e => e.Replace("!STRING:0", "")).ToList();

            for (var c = 0; c < cols.Count; c++)
            {
                for (var i = 1; i < lines.Length; i++)
                {
                    var row = lines[i].Split('|');

                    switch (cols[c])
                    {
                        case "Name":
                            cdns.entries[i - 1].name = row[c];
                            break;
                        case "Path":
                            cdns.entries[i - 1].path = row[c];
                            break;
                        case "Hosts":
                            var hosts = row[c].Split(' ');
                            cdns.entries[i - 1].hosts = new string[hosts.Length];
                            for (var h = 0; h < hosts.Length; h++)
                            {
                                cdns.entries[i - 1].hosts[h] = hosts[h];
                            }
                            break;
                        case "ConfigPath":
                            cdns.entries[i - 1].configPath = row[c];
                            break;
                        case "Servers":
                            cdns.entries[i - 1].servers = row[c];
                            break;
                        default:
                            cdns.UnknownKeyPairs.Add(row[c]);
                            break;
                    }
                }
            }

            if (!cdns.entries.Any())
            {
                throw new Exception($"Invalid CDNs file for {targetProduct.DisplayName}, skipping!");
            }

            return cdns;
        }
    }
}
