using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleNetPrefill.EncryptDecrypt;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Utils;
using BattleNetPrefill.Web;

namespace BattleNetPrefill.Handlers
{
    /// <summary>
    /// https://wowdev.wiki/TACT#Install_manifest
    /// </summary>
    public class InstallFileHandler
    {
        private readonly CdnRequestManager _cdnRequestManager;

        public InstallFileHandler(CdnRequestManager cdnRequestManager)
        {
            _cdnRequestManager = cdnRequestManager;
        }

        /// <summary>
        /// Downloads the install manifest, then determines which files will need to be downloaded for the specified product's installation,
        /// and proceeds to download them.
        ///
        /// Not all products will actually use this install manifest, a large majority of them don't.
        /// </summary>
        /// <param name="buildConfig"></param>
        /// <param name="archiveIndexHandler"></param>
        /// <param name="cdnConfigFile"></param>
        /// <returns></returns>
        public async Task HandleInstallFileAsync(BuildConfigFile buildConfig, ArchiveIndexHandler archiveIndexHandler, CDNConfigFile cdnConfigFile)
        {
            InstallFile installFile = await ParseInstallFileAsync(buildConfig);
            
            //TODO make this more flexible/multi region.  Should probably be passed in/ validated per product.
            //TODO do a check to make sure that the tags being used are actually valid for the product
            List<InstallFileEntry> filtered = installFile.entries
                    .Where(e => e.tags.Contains("1=enUS") && e.tags.Contains("2=Windows"))
                    .ToList();
        
            if (!filtered.Any())
            {
                return;
            }

            var encodingFileHandler = new EncodingFileHandler(_cdnRequestManager);
            EncodingFile encodingTable = await encodingFileHandler.GetEncodingAsync(buildConfig);

            foreach (var file in filtered)
            {
                //The manifest contains pairs of IndexId-ContentHash, reverse lookup for matches based on the ContentHash
                if (!encodingTable.ReversedEncodingDictionary.ContainsKey(file.contentHash))
                {
                    continue;
                }

                // If we found a match for the archive content, look into the archive index to see where the file can be downloaded from
                MD5Hash upperHash = encodingTable.ReversedEncodingDictionary[file.contentHash];

                ArchiveIndexEntry? archiveIndex = archiveIndexHandler.ArchivesContainKey(upperHash);
                if (archiveIndex == null)
                {
                    continue;
                }

                ArchiveIndexEntry e = archiveIndex.Value;

                // Need to subtract 1, since the byte range is "inclusive"
                var upperByteRange = ((int)e.offset + (int)e.size - 1);
                MD5Hash archiveIndexKey = cdnConfigFile.archives[e.index].hashIdMd5;
                _cdnRequestManager.QueueRequest(RootFolder.data, archiveIndexKey, (int)e.offset, upperByteRange);
            }
        }

        private async Task<InstallFile> ParseInstallFileAsync(BuildConfigFile buildConfig)
        {
            var install = new InstallFile();
            var endBytes = Math.Max(4095, buildConfig.installSize[1] - 1);
            byte[] content = await _cdnRequestManager.GetRequestAsBytesAsync(RootFolder.data, buildConfig.install[1], startBytes: 0, endBytes: endBytes);

            using var memoryStream = BLTE.Parse(content);
            using BinaryReader bin = new BinaryReader(memoryStream);
            if (Encoding.UTF8.GetString(bin.ReadBytes(2)) != "IN")
            {
                throw new Exception("Error while parsing install file. Did BLTE header size change?"); 
            }

            bin.ReadByte();

            install.hashSize = bin.ReadByte();
            if (install.hashSize != 16) throw new Exception("Unsupported install hash size!");

            install.numTags = bin.ReadUInt16BigEndian();
            install.numEntries = bin.ReadUInt32BigEndian();

            int bytesPerTag = ((int)install.numEntries + 7) / 8;

            install.tags = new InstallTagEntry[install.numTags];

            for (var i = 0; i < install.numTags; i++)
            {
                install.tags[i].name = bin.ReadCString();
                install.tags[i].type = bin.ReadUInt16BigEndian();

                var filebits = bin.ReadBytes(bytesPerTag);

                for (int j = 0; j < bytesPerTag; j++)
                    filebits[j] = (byte)((filebits[j] * 0x0202020202 & 0x010884422010) % 1023);

                install.tags[i].files = new BitArray(filebits);
            }

            install.entries = new InstallFileEntry[install.numEntries];

            for (var i = 0; i < install.numEntries; i++)
            {
                install.entries[i].name = bin.ReadCString();
                install.entries[i].contentHash = bin.Read<MD5Hash>();
                install.entries[i].size = bin.ReadUInt32BigEndian();
                install.entries[i].tags = new List<string>();
                for (var j = 0; j < install.numTags; j++)
                {
                    if (install.tags[j].files[i] == true)
                    {
                        install.entries[i].tags.Add(install.tags[j].type + "=" + install.tags[j].name);
                    }
                }
            }
            return install;
        }
    }
}