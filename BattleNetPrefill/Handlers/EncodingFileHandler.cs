namespace BattleNetPrefill.Handlers
{
    /// <summary>
    /// https://wowdev.wiki/TACT#Encoding_table
    /// </summary>
    public sealed class EncodingFileHandler
    {
        private readonly CdnRequestManager _cdnRequestManager;

        public EncodingFileHandler(CdnRequestManager cdnRequestManager)
        {
            _cdnRequestManager = cdnRequestManager;
        }

        public async Task<EncodingFile> GetEncodingAsync(BuildConfigFile buildConfig)
        {
            int encodingSize;
            if (buildConfig.encodingSize == null || buildConfig.encodingSize.Length < 2)
            {
                encodingSize = 0;
            }
            else
            {
                encodingSize = buildConfig.encodingSize[1];
            }

            var encoding = new EncodingFile();

            byte[] content = await _cdnRequestManager.GetRequestAsBytesAsync(RootFolder.data, buildConfig.encoding[1]);

            if (encodingSize != 0 && encodingSize != content.Length)
            {
                content = await _cdnRequestManager.GetRequestAsBytesAsync(RootFolder.data, buildConfig.encoding[1]);

                if (encodingSize != content.Length && encodingSize != 0)
                {
                    throw new Exception($"File corrupt/not fully downloaded! Remove data / {buildConfig.encoding[1].ToString()} from cache.");
                }
            }

            using var memoryStream = BLTE.Parse(content);
            using BinaryReader bin = new BinaryReader(memoryStream);

            if (Encoding.UTF8.GetString(bin.ReadBytes(2)) != "EN")
            {
                throw new Exception("Error while parsing encoding file. Did BLTE header size change?");
            }

            // Skip over entries we don't use anymore
            bin.ReadBytes(7);

            encoding.numEntriesA = bin.ReadUInt32BigEndian();
            bin.ReadUInt32BigEndian();
            bin.ReadByte(); // unk
            encoding.stringBlockSize = bin.ReadUInt32BigEndian();

            bin.BaseStream.Position += (long)encoding.stringBlockSize;

            /* Table A */
            bin.BaseStream.Position += encoding.numEntriesA * 32;

            var tableAstart = bin.BaseStream.Position;
            encoding.aEntriesReversed = new Dictionary<MD5Hash, MD5Hash>(Md5HashEqualityComparer.Instance);
            for (int i = 0; i < encoding.numEntriesA; i++)
            {
                ushort keysCount;
                while ((keysCount = bin.ReadUInt16()) != 0)
                {
                    // Size
                    bin.BaseStream.Position += 4;
                    var hash2 = bin.Read<MD5Hash>();
                    var key = bin.Read<MD5Hash>();

                    bin.BaseStream.Position += (keysCount - 1) * 16;

                    encoding.aEntriesReversed.Add(key, hash2);
                }

                var remaining = 4096 - ((bin.BaseStream.Position - tableAstart) % 4096);
                if (remaining > 0)
                {
                    bin.BaseStream.Position += remaining;
                }
            }

            return encoding;
        }
    }
}