﻿using System;
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

        public CdnFileHandler(CDN cdn)
        {
            this.cdn = cdn;
        }

        //TODO should this be part of the CDN class?
        public CdnsFile ParseCdnsFile(TactProduct tactProduct)
        {
            var timer = Stopwatch.StartNew();

            string content = cdn.MakePatchRequest(tactProduct, "cdns");

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
                Console.WriteLine($"Invalid CDNs file for {tactProduct.DisplayName}, skipping!");
                throw new Exception($"Invalid CDNs file for {tactProduct.DisplayName}, skipping!");
            }

            timer.Stop();
            if (timer.Elapsed.Milliseconds > 10)
            {
                Console.Write("CDNs File loaded in ".PadRight(Config.PadRight));
                Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF")).PadLeft(Config.Padding)}");
            }
            
            return cdns;
        }
    }
}
