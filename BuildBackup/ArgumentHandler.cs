//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace BuildBackup
//{
//    public static class ArgumentHandler
//    {
//        TODO reimplement later
//        private static bool HandleArguments(string[] args)
//        {
//            if (args.Length > 0)
//            {

//                if (args[0] == "calchashlistfile")
//                {
//                    string target = "";

//                    if (args.Length == 2 && File.Exists(args[1]))
//                    {
//                        target = args[1];
//                    }
//                    else
//                    {
//                        UpdateListfile();
//                        target = "listfile.txt";
//                    }

//                    var hasher = new Jenkins96();

//                    foreach (var line in File.ReadLines(target))
//                    {
//                        if (string.IsNullOrEmpty(line)) continue;
//                        var hash = hasher.ComputeHash(line);
//                        Console.WriteLine(line + " = " + hash.ToString("x").PadLeft(16, '0'));
//                    }

//                    Environment.Exit(0);
//                }


//                if (args[0] == "extractfilebycontenthash" || args[0] == "extractrawfilebycontenthash")
//                {
//                    if (args.Length != 6)
//                        throw new Exception(
//                            "Not enough arguments. Need mode, product, buildconfig, cdnconfig, contenthash, outname");

//                    cdns = GetCDNs(args[1]);

//                    args[4] = args[4].ToLower();

//                    buildConfig = GetBuildConfig(cdns.entries[0].path, args[2]);
//                    if (string.IsNullOrWhiteSpace(buildConfig.buildName))
//                    {
//                        Console.WriteLine("Invalid buildConfig!");
//                    }

//                    encoding = GetEncoding(Path.Combine(cdns.entries[0].path), buildConfig.encoding[1]);

//                    string target = "";

//                    foreach (var entry in encoding.aEntries)
//                    {
//                        if (entry.hash.ToLower() == args[4])
//                        {
//                            target = entry.key.ToLower();
//                            break;
//                        }
//                    }

//                    if (string.IsNullOrEmpty(target))
//                    {
//                        throw new Exception("File not found in encoding!");
//                    }

//                    var cdnConfig = GetCDNconfig(cdns.entries[0].path, args[3]);

//                    GetIndexes(Path.Combine(cdns.entries[0].path), cdnConfig.archives);

//                    if (args[0] == "extractrawfilebycontenthash")
//                    {
//                        var unarchivedName = Path.Combine(cdn.cacheDir, cdns.entries[0].path, "data",
//                            target[0] + "" + target[1], target[2] + "" + target[3], target);

//                        Directory.CreateDirectory(Path.GetDirectoryName(unarchivedName));

//                        File.WriteAllBytes(unarchivedName, RetrieveFileBytes(target, cdnConfig, true, cdns.entries[0].path));
//                    }
//                    else
//                    {
//                        File.WriteAllBytes(args[5], RetrieveFileBytes(target, cdnConfig, false, cdns.entries[0].path));
//                    }

//                    Environment.Exit(0);
//                }

//                if (args[0] == "extractfilesbylist")
//                {
//                    if (args.Length != 5)
//                        throw new Exception("Not enough arguments. Need mode, buildconfig, cdnconfig, basedir, list");

//                    buildConfig = GetBuildConfig(Path.Combine("tpr", "wow"), args[1]);
//                    if (string.IsNullOrWhiteSpace(buildConfig.buildName))
//                    {
//                        Console.WriteLine("Invalid buildConfig!");
//                    }

//                    encoding = GetEncoding(Path.Combine("tpr", "wow"), buildConfig.encoding[1]);

//                    var basedir = args[3];

//                    var lines = File.ReadLines(args[4]);

//                    var cdnConfig = GetCDNconfig(Path.Combine("tpr", "wow"), args[2]);

//                    GetIndexes(Path.Combine("tpr", "wow"), cdnConfig.archives);

//                    foreach (var line in lines)
//                    {
//                        var splitLine = line.Split(',');
//                        var contenthash = splitLine[0];
//                        var filename = splitLine[1];

//                        if (!Directory.Exists(Path.Combine(basedir, Path.GetDirectoryName(filename))))
//                        {
//                            Directory.CreateDirectory(Path.Combine(basedir, Path.GetDirectoryName(filename)));
//                        }

//                        Console.WriteLine(filename);

//                        string target = "";

//                        foreach (var entry in encoding.aEntries)
//                        {
//                            if (entry.hash.ToLower() == contenthash.ToLower())
//                            {
//                                target = entry.key.ToLower();
//                                Console.WriteLine("Found target: " + target);
//                                break;
//                            }
//                        }

//                        if (string.IsNullOrEmpty(target))
//                        {
//                            Console.WriteLine("File " + filename + " (" + contenthash + ") not found in encoding!");
//                            continue;
//                        }

//                        try
//                        {
//                            File.WriteAllBytes(Path.Combine(basedir, filename), RetrieveFileBytes(target, cdnConfig));
//                        }
//                        catch (Exception e)
//                        {
//                            Console.WriteLine(e.Message);
//                        }
//                    }

//                    Environment.Exit(0);
//                }

//                if (args[0] == "extractfilesbyfnamelist" || args[0] == "extractfilesbyfdidlist")
//                {
//                    if (args.Length != 5)
//                        throw new Exception("Not enough arguments. Need mode, buildconfig, cdnconfig, basedir, list");

//                    buildConfig = GetBuildConfig(Path.Combine("tpr", "wow"), args[1]);
//                    if (string.IsNullOrWhiteSpace(buildConfig.buildName))
//                    {
//                        Console.WriteLine("Invalid buildConfig!");
//                    }

//                    encoding = GetEncoding(Path.Combine("tpr", "wow"), buildConfig.encoding[1]);

//                    var cdnConfig = GetCDNconfig(Path.Combine("tpr", "wow"), args[2]);

//                    GetIndexes(Path.Combine("tpr", "wow"), cdnConfig.archives);

//                    var basedir = args[3];

//                    var lines = File.ReadLines(args[4]);

//                    var rootHash = "";

//                    foreach (var entry in encoding.aEntries)
//                    {
//                        if (entry.hash.ToLower() == buildConfig.root.ToLower())
//                        {
//                            rootHash = entry.key.ToLower();
//                            break;
//                        }
//                    }

//                    var hasher = new Jenkins96();
//                    var nameList = new Dictionary<ulong, string>();
//                    var fdidList = new Dictionary<uint, string>();

//                    if (args[0] == "extractfilesbyfnamelist")
//                    {
//                        foreach (var line in lines)
//                        {
//                            var hash = hasher.ComputeHash(line);
//                            nameList.Add(hash, line);
//                        }
//                    }
//                    else if (args[0] == "extractfilesbyfdidlist")
//                    {
//                        foreach (var line in lines)
//                        {
//                            if (string.IsNullOrEmpty(line))
//                                continue;

//                            var expl = line.Split(';');
//                            if (expl.Length == 1)
//                            {
//                                fdidList.Add(uint.Parse(expl[0]), expl[0]);
//                            }
//                            else
//                            {
//                                fdidList.Add(uint.Parse(expl[0]), expl[1]);
//                            }
//                        }
//                    }

//                    Console.WriteLine("Looking up in root..");

//                    root = GetRoot(Path.Combine("tpr", "wow"), rootHash, true);

//                    var encodingList = new Dictionary<string, List<string>>();

//                    foreach (var entry in root.entriesFDID)
//                    {
//                        foreach (var subentry in entry.Value)
//                        {
//                            if (subentry.contentFlags.HasFlag(ContentFlags.LowViolence)) continue;

//                            if (!subentry.localeFlags.HasFlag(LocaleFlags.All_WoW) &&
//                                !subentry.localeFlags.HasFlag(LocaleFlags.enUS))
//                            {
//                                continue;
//                            }

//                            if (args[0] == "extractfilesbyfnamelist")
//                            {
//                                if (nameList.ContainsKey(subentry.lookup))
//                                {
//                                    var cleanContentHash =
//                                        BitConverter.ToString(subentry.md5).Replace("-", string.Empty).ToLower();

//                                    if (encodingList.ContainsKey(cleanContentHash))
//                                    {
//                                        encodingList[cleanContentHash].Add(nameList[subentry.lookup]);
//                                    }
//                                    else
//                                    {
//                                        encodingList.Add(cleanContentHash, new List<string>() { nameList[subentry.lookup] });
//                                    }
//                                }
//                            }
//                            else if (args[0] == "extractfilesbyfdidlist")
//                            {
//                                if (fdidList.ContainsKey(subentry.fileDataID))
//                                {
//                                    var cleanContentHash =
//                                        BitConverter.ToString(subentry.md5).Replace("-", string.Empty).ToLower();

//                                    if (encodingList.ContainsKey(cleanContentHash))
//                                    {
//                                        encodingList[cleanContentHash].Add(fdidList[subentry.fileDataID]);
//                                    }
//                                    else
//                                    {
//                                        encodingList.Add(cleanContentHash, new List<string>() { fdidList[subentry.fileDataID] });
//                                    }
//                                }
//                            }

//                            continue;
//                        }
//                    }

//                    var fileList = new Dictionary<string, List<string>>();

//                    Console.WriteLine("Looking up in encoding..");
//                    foreach (var encodingEntry in encoding.aEntries)
//                    {
//                        string target = "";

//                        if (encodingList.ContainsKey(encodingEntry.hash.ToLower()))
//                        {
//                            target = encodingEntry.key.ToLower();
//                            //Console.WriteLine(target);
//                            foreach (var subName in encodingList[encodingEntry.hash.ToLower()])
//                            {
//                                if (fileList.ContainsKey(target))
//                                {
//                                    fileList[target].Add(subName);
//                                }
//                                else
//                                {
//                                    fileList.Add(target, new List<string>() { subName });
//                                }
//                            }

//                            encodingList.Remove(encodingEntry.hash.ToLower());
//                        }
//                    }

//                    var archivedFileList = new Dictionary<string, Dictionary<string, List<string>>>();
//                    var unarchivedFileList = new Dictionary<string, List<string>>();

//                    Console.WriteLine("Looking up in indexes..");
//                    foreach (var fileEntry in fileList)
//                    {
//                        if (!indexDictionary.TryGetValue(fileEntry.Key.ToUpper(), out IndexEntry entry))
//                        {
//                            unarchivedFileList.Add(fileEntry.Key, fileEntry.Value);
//                        }

//                        var index = cdnConfig.archives[entry.index];
//                        if (!archivedFileList.ContainsKey(index))
//                        {
//                            archivedFileList.Add(index, new Dictionary<string, List<string>>());
//                        }

//                        archivedFileList[index].Add(fileEntry.Key, fileEntry.Value);
//                    }

//                    var extractedFiles = 0;
//                    var totalFiles = fileList.Count;

//                    Console.WriteLine("Extracting " + unarchivedFileList.Count + " unarchived files..");
//                    foreach (var fileEntry in unarchivedFileList)
//                    {
//                        var target = fileEntry.Key;

//                        foreach (var filename in fileEntry.Value)
//                        {
//                            if (!Directory.Exists(Path.Combine(basedir, Path.GetDirectoryName(filename))))
//                            {
//                                Directory.CreateDirectory(Path.Combine(basedir, Path.GetDirectoryName(filename)));
//                            }
//                        }

//                        var unarchivedName = Path.Combine(cdn.cacheDir, "tpr", "wow", "data", target[0] + "" + target[1],
//                            target[2] + "" + target[3], target);
//                        if (File.Exists(unarchivedName))
//                        {
//                            foreach (var filename in fileEntry.Value)
//                            {
//                                try
//                                {
//                                    File.WriteAllBytes(Path.Combine(basedir, filename),
//                                        BLTE.Parse(File.ReadAllBytes(unarchivedName)));
//                                }
//                                catch (Exception e)
//                                {
//                                    Console.WriteLine(e.Message);
//                                }
//                            }
//                        }
//                        else
//                        {
//                            Console.WriteLine("Unarchived file does not exist " + unarchivedName + ", cannot extract " +
//                                              string.Join(',', fileEntry.Value));
//                        }

//                        extractedFiles++;

//                        if (extractedFiles % 100 == 0)
//                        {
//                            Console.WriteLine("[" + DateTime.Now.ToString() + "] Extracted " + extractedFiles + " out of " +
//                                              totalFiles + " files");
//                        }
//                    }

//                    foreach (var archiveEntry in archivedFileList)
//                    {
//                        var archiveName = Path.Combine(cdn.cacheDir, "tpr", "wow", "data",
//                            archiveEntry.Key[0] + "" + archiveEntry.Key[1], archiveEntry.Key[2] + "" + archiveEntry.Key[3],
//                            archiveEntry.Key);
//                        Console.WriteLine("[" + DateTime.Now.ToString() + "] Extracting " + archiveEntry.Value.Count +
//                                          " files from archive " + archiveEntry.Key + "..");

//                        using (var stream = new MemoryStream(File.ReadAllBytes(archiveName)))
//                        {
//                            foreach (var fileEntry in archiveEntry.Value)
//                            {
//                                var target = fileEntry.Key;

//                                foreach (var filename in fileEntry.Value)
//                                {
//                                    if (!Directory.Exists(Path.Combine(basedir, Path.GetDirectoryName(filename))))
//                                    {
//                                        Directory.CreateDirectory(Path.Combine(basedir, Path.GetDirectoryName(filename)));
//                                    }
//                                }

//                                if (indexDictionary.TryGetValue(target.ToUpper(), out IndexEntry entry))
//                                {
//                                    foreach (var filename in fileEntry.Value)
//                                    {
//                                        if (File.Exists(Path.Combine(basedir, filename)))
//                                            continue;

//                                        try
//                                        {
//                                            stream.Seek(entry.offset, SeekOrigin.Begin);

//                                            if (entry.offset > stream.Length || entry.offset + entry.size > stream.Length)
//                                            {
//                                                throw new Exception("File is beyond archive length, incomplete archive!");
//                                            }

//                                            var archiveBytes = new byte[entry.size];
//                                            stream.Read(archiveBytes, 0, (int)entry.size);
//                                            File.WriteAllBytes(Path.Combine(basedir, filename), BLTE.Parse(archiveBytes));
//                                        }
//                                        catch (Exception e)
//                                        {
//                                            Console.WriteLine(e.Message);
//                                        }
//                                    }
//                                }
//                                else
//                                {
//                                    Console.WriteLine("!!!!! Unable to find " + fileEntry.Key + " (" + fileEntry.Value[0] +
//                                                      ") in archives!");
//                                }

//                                extractedFiles++;

//                                if (extractedFiles % 1000 == 0)
//                                {
//                                    Console.WriteLine("[" + DateTime.Now.ToString() + "] Extracted " + extractedFiles +
//                                                      " out of " + totalFiles + " files");
//                                }
//                            }
//                        }
//                    }

//                    Environment.Exit(0);
//                }

//            }

//            return false;
//        }
//    }
//}
