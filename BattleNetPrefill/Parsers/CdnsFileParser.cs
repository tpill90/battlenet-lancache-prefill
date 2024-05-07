namespace BattleNetPrefill.Parsers
{
    [SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments", Justification = "Performance isn't an issue here, and implementing this warning's suggestion will impact readability negatively.")]
    public static class CdnsFileParser
    {
        public static async Task<CdnsEntry[]> ParseCdnsFileAsync(CdnRequestManager cdnRequestManager, TactProduct targetProduct)
        {
            string content = await cdnRequestManager.MakePatchRequestAsync(targetProduct, PatchRequest.cdns);
            var lines = content.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                               .Where(e => e[0] != '#')
                               .ToArray();

            if (!lines.Any())
            {
                throw new Exception($"Unexpected empty CDNs file for {targetProduct.DisplayName}.  CDNs file cannot be empty!");
            }


            var entries = new CdnsEntry[lines.Length - 1];

            var cols = lines[0].Split('|').Select(e => e.Replace("!STRING:0", "")).ToList();
            for (var c = 0; c < cols.Count; c++)
            {
                for (var i = 1; i < lines.Length; i++)
                {
                    var row = lines[i].Split('|');

                    switch (cols[c])
                    {
                        case "Path":
                            entries[i - 1].path = row[c];
                            break;
                        case "Hosts":
                            var hosts = row[c].Split(' ');
                            entries[i - 1].hosts = new string[hosts.Length];
                            for (var h = 0; h < hosts.Length; h++)
                            {
                                entries[i - 1].hosts[h] = hosts[h];
                            }
                            break;
                        // We don't use these fields, so we're skipping over them
                        case "Name":
                        case "ConfigPath":
                        case "Servers":
                            break;
                        default:
                            AnsiConsole.Console.LogMarkupError($"!!!!!!!! Unknown CdnEntry variable '{cols[0]}'");
                            break;
                    }
                }
            }

            if (!entries.Any())
            {
                throw new Exception($"Invalid CDNs file for {targetProduct.DisplayName}, skipping!");
            }

            return entries;
        }
    }
}
