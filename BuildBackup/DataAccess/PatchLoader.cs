using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using BuildBackup.Structs;
using Colors = Shared.Colors;

namespace BuildBackup.DataAccess
{
    public class PatchLoader
    {
        private readonly CDN _cdn;
        private readonly CDNConfigFile _cdnConfig;

        public PatchLoader(CDN cdn, CDNConfigFile cdnConfig)
        {
            _cdn = cdn;
            _cdnConfig = cdnConfig;
        }

        public void HandlePatches(BuildConfigFile buildConfig, TactProduct targetProduct)
        {
            var timer = Stopwatch.StartNew();

            // For whatever reason, CodVanguard + Warzone do not make this request.
            if (!string.IsNullOrEmpty(buildConfig.patchConfig) 
                && targetProduct != TactProducts.CodVanguard && targetProduct != TactProducts.CodWarzone
                && targetProduct != TactProducts.Hearthstone)
            {
                _cdn.QueueRequest(RootFolder.config, buildConfig.patchConfig);
            }

            if (!string.IsNullOrEmpty(buildConfig.patch))
            {
                GetPatchFile(buildConfig.patch);
            }

            // Unused by Hearthstone
            if (targetProduct != TactProducts.Hearthstone)
            {
                _cdn.QueueRequest(RootFolder.patch, _cdnConfig.patchFileIndex, isIndex: true);
            }
            
            if (buildConfig.patchIndex != null)
            {
                _cdn.QueueRequest(RootFolder.data, buildConfig.patchIndex[1], 0, 4095);
            }

            // Unused by Hearthstone
            if (_cdnConfig.patchArchives != null && targetProduct != TactProducts.Hearthstone)
            {
                foreach (var patchIndex in _cdnConfig.patchArchives)
                {
                    _cdn.QueueRequest(RootFolder.patch, patchIndex, isIndex: true);
                }
            }

            timer.Stop();
            if (timer.Elapsed.TotalMilliseconds > 10)
            {
                Console.Write("Handled patches...".PadRight(Config.PadRight));
                Console.WriteLine($"{Colors.Yellow(timer.Elapsed.ToString(@"mm\:ss\.FFFF"))}".PadLeft(Config.Padding));
            }
        }

        private PatchFile GetPatchFile(string hash)
        {
            var patchFile = new PatchFile();

            byte[] content = _cdn.Get(RootFolder.patch, hash);

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
    }
}
