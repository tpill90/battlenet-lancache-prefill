using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuildBackup.Structs;
using ByteSizeLib;
using Konsole;
using Colors = Shared.Colors;

namespace BuildBackup.DataAccess
{
    public class PatchLoader
    {
        private CDN _cdn;
        private CdnsFile _cdns;
        private readonly IConsole _console;
        private readonly TactProduct _currentProduct;
        private readonly CDNConfigFile _cdnConfig;

        List<TactProduct> productsToSkip = new List<TactProduct> 
        {
            TactProducts.Diablo3,
            TactProducts.Hearthstone,
            TactProducts.HeroesOfTheStorm,
            TactProducts.Overwatch,
            TactProducts.Starcraft1,
            TactProducts.Starcraft2,
            TactProducts.WorldOfWarcraft
        };

        public PatchLoader(CDN cdn, CdnsFile cdns, IConsole console, TactProduct currentProduct, CDNConfigFile cdnConfig)
        {
            _cdn = cdn;
            Debug.Assert(cdns.entries != null, "Cdns must be initialized before using");
            _cdns = cdns;
            _console = console;
            _currentProduct = currentProduct;
            _cdnConfig = cdnConfig;
        }

        //TODO comment
        public PatchFile DownloadPatchConfig(BuildConfigFile buildConfig)
        {
            if (!string.IsNullOrEmpty(buildConfig.patchConfig))
            {
                _cdn.Get($"{_cdns.entries[0].path}/config/", buildConfig.patchConfig);
            }

            if (!string.IsNullOrEmpty(buildConfig.patch))
            {
                return GetPatchFile(_cdns.entries[0].path, buildConfig.patch);
            }

            return new PatchFile();
        }

        public void HandlePatches(PatchFile patch)
        {
            Console.Write("Handling patches...".PadRight(Config.PadRight));
            var timer = Stopwatch.StartNew();
            

            DownloadPatchArchives(patch);
            DownloadPatchFiles(_cdnConfig);
            DownloadFullPatchArchives(_cdnConfig);

            Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}".PadLeft(Config.Padding));
        }

        private PatchFile GetPatchFile(string url, string hash)
        {
            var patchFile = new PatchFile();

            byte[] content = _cdn.Get($"{url}/patch/", hash);

            using (BinaryReader bin = new BinaryReader(new MemoryStream(content)))
            {
                if (Encoding.UTF8.GetString(bin.ReadBytes(2)) != "PA") { throw new Exception("Error while parsing patch file!"); }

                patchFile.version = bin.ReadByte();
                patchFile.fileKeySize = bin.ReadByte();
                patchFile.sizeB = bin.ReadByte();
                patchFile.patchKeySize = bin.ReadByte();
                patchFile.blockSizeBits = bin.ReadByte();
                patchFile.blockCount = bin.ReadUInt16(true);
                patchFile.flags = bin.ReadByte();
                patchFile.encodingContentKey = bin.ReadBytes(16);
                patchFile.encodingEncodingKey = bin.ReadBytes(16);
                patchFile.decodedSize = bin.ReadUInt32(true);
                patchFile.encodedSize = bin.ReadUInt32(true);
                patchFile.especLength = bin.ReadByte();
                patchFile.encodingSpec = new string(bin.ReadChars(patchFile.especLength));

                patchFile.blocks = new PatchBlock[patchFile.blockCount];
                for (var i = 0; i < patchFile.blockCount; i++)
                {
                    patchFile.blocks[i].lastFileContentKey = bin.ReadBytes(patchFile.fileKeySize);
                    patchFile.blocks[i].blockMD5 = bin.ReadBytes(16);
                    patchFile.blocks[i].blockOffset = bin.ReadUInt32(true);

                    var prevPos = bin.BaseStream.Position;

                    var files = new List<BlockFile>();

                    bin.BaseStream.Position = patchFile.blocks[i].blockOffset;
                    while (bin.BaseStream.Position <= patchFile.blocks[i].blockOffset + 0x10000)
                    {
                        var file = new BlockFile();

                        file.numPatches = bin.ReadByte();
                        if (file.numPatches == 0) break;
                        file.targetFileContentKey = bin.ReadBytes(patchFile.fileKeySize);
                        file.decodedSize = bin.ReadUInt40(true);

                        var filePatches = new List<FilePatch>();

                        for (var j = 0; j < file.numPatches; j++)
                        {
                            var filePatch = new FilePatch();
                            filePatch.sourceFileEncodingKey = bin.ReadBytes(patchFile.fileKeySize);
                            filePatch.decodedSize = bin.ReadUInt40(true);
                            filePatch.patchEncodingKey = bin.ReadBytes(patchFile.patchKeySize);
                            filePatch.patchSize = bin.ReadUInt32(true);
                            filePatch.patchIndex = bin.ReadByte();
                            filePatches.Add(filePatch);
                        }

                        file.patches = filePatches.ToArray();

                        files.Add(file);
                    }

                    patchFile.blocks[i].files = files.ToArray();
                    bin.BaseStream.Position = prevPos;
                }
            }

            return patchFile;
        }

        public void DownloadPatchFiles(CDNConfigFile cdnConfig)
        {
            var patchFileIndexList = IndexParser.ParseIndex(_cdns.entries[0].path, cdnConfig.patchFileIndex, _cdn, "patch");
            
            // For whatever reason, the following products do not use these patch files.
            if (productsToSkip.Contains(_currentProduct))
            {
                return;
            }

            if (string.IsNullOrEmpty(cdnConfig.patchFileIndex))
            {
                return;
            }

            Console.WriteLine("Parsing patch file index..");
            var downloadSize = ByteSize.FromBytes((double)patchFileIndexList.Sum(e => (decimal)e.Value.size));

            Console.WriteLine($"     Downloading {Colors.Cyan(patchFileIndexList.Count)} unarchived patch files from patch file index...");
            Console.WriteLine($"     Total archive size : {Colors.Magenta(downloadSize.ToString())}");

            // Progress bar setup
            var progressBar2 = new ProgressBar(_console, PbStyle.SingleLine, patchFileIndexList.Keys.Count);
            int count = 0;
            var timer = Stopwatch.StartNew();

            // Download the files and update onscreen status
            Parallel.ForEach(patchFileIndexList.Keys, new ParallelOptions { MaxDegreeOfParallelism = 20 }, (entry) =>
            {
                _cdn.Get($"{_cdns.entries[0].path}/patch/", entry,  writeToDevNull: true);
                progressBar2.Refresh(count, $"     {_cdns.entries[0].path}/patch/{entry}");
                count++;
            });
            timer.Stop();
            progressBar2.Refresh(count, $"     Done! {Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}");
        }

        //TODO comment
        public void DownloadPatchArchives(PatchFile patchFile)
        {
            if (_cdnConfig.patchArchives == null)
            {
                return;
            }

            var patchIndexDictionary = GetPatchIndexes(_cdns.entries[0].path, _cdnConfig.patchArchives);
            
            // For whatever reason, the following products do not use these patch files.
            if (productsToSkip.Contains(_currentProduct))
            {
                return;
            }

            if (patchFile.blocks == null)
            {
                return;
            }

            var unarchivedPatchKeyList = new List<string>();

            foreach (var block in patchFile.blocks)
            {
                foreach (var fileBlock in block.files)
                {
                    foreach (var patch in fileBlock.patches)
                    {
                        var pKey = BitConverter.ToString(patch.patchEncodingKey).Replace("-", "");
                        if (!patchIndexDictionary.ContainsKey(pKey))
                        {
                            unarchivedPatchKeyList.Add(pKey);
                        }
                    }
                }
            }

            if (unarchivedPatchKeyList.Count <= 0)
            {
                return;
            }

            Console.Write($"     Downloading {Colors.Cyan(unarchivedPatchKeyList.Count)} unarchived patch files..".PadRight(Config.PadRight));
            foreach (var entry in unarchivedPatchKeyList)
            {
                _cdn.Get($"{_cdns.entries[0].path}/patch/", entry, writeToDevNull: true);
            }
            
        }

        public void DownloadFullPatchArchives(CDNConfigFile cdnConfig)
        {
            if (cdnConfig.patchArchives == null)
            {
                return;
            }

            // For whatever reason, the following products do not use these patch files.
            if (productsToSkip.Contains(_currentProduct))
            {
                return;
            }

            Console.WriteLine($"     Downloading {Colors.Cyan(cdnConfig.patchArchives.Length)} patch archives..");
            foreach (var patchId in cdnConfig.patchArchives)
            {
                _cdn.QueueRequest($"{_cdns.entries[0].path}/patch/", patchId, writeToDevNull: true);
            }
        }

        private Dictionary<string, IndexEntry> GetPatchIndexes(string url, string[] archives)
        {
            var indexDictionary = new Dictionary<string, IndexEntry>();
            for (int i = 0; i < archives.Length; i++)
            {
                byte[] indexContent = _cdn.GetIndex($"{url}/patch/", archives[i]);

                using (BinaryReader bin = new BinaryReader(new MemoryStream(indexContent)))
                {
                    int indexEntries = indexContent.Length / 4096;

                    for (var b = 0; b < indexEntries; b++)
                    {
                        for (var bi = 0; bi < 170; bi++)
                        {
                            var headerHash = BitConverter.ToString(bin.ReadBytes(16)).Replace("-", "");

                            var entry = new IndexEntry()
                            {
                                index = (short)i,
                                size = bin.ReadUInt32(true),
                                offset = bin.ReadUInt32(true)
                            };

                            if (!indexDictionary.ContainsKey(headerHash))
                            {
                                indexDictionary.Add(headerHash, entry);
                            }
                        }
                        bin.ReadBytes(16);
                    }
                }
            }
            return indexDictionary;
        }
    }
}
