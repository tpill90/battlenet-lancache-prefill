using BuildBackup.Structs;

namespace BuildBackup
{
    public struct VersionsFile
    {
        public VersionsEntry[] entries;
    }

    public struct CdnsFile
    {
        public CdnsEntry[] entries;
    }

    public struct Archive
    {
        public string hashId;
        public MD5Hash hashIdMd5;

        // Not sure what this means right now
        public int archiveIndexSize;

        public override string ToString()
        {
            return $"{hashId}";
        }
    }

    public sealed class CDNConfigFile
    {
        /// <summary>
        /// A list of CDN identifiers for all archives
        /// </summary>
        public Archive[] archives;

        public string archiveGroup;

        public MD5Hash[] patchArchives;
        public int[] patchArchivesIndexSize;
        public string patchArchiveGroup;

        public string[] builds;

        /// <summary>
        /// CDN Key for the "file" index.  Can be requested and parsed.
        ///
        /// The "file" index lists what known "unarchived" files exist on the CDN.  Using the index you can take a content MD5,  and can be used to lookup the CDNKey
        /// used to download the file.
        /// </summary>
        public MD5Hash fileIndex;
        public string fileIndexSize;

        public MD5Hash patchFileIndex;
        public int patchFileIndexSize;
    }

    //TODO comment + rename
    public struct IndexEntry
    {
        /// <summary>
        /// Use this to lookup the CdnHash of the archive index that holds this file.
        /// </summary>
        public short index;

        public uint offset;
        public uint size;

        public override string ToString()
        {
            return $"{index} Offset: {offset} size: {size}";
        }
    }

    public struct IndexFooter
    {
        public byte[] tocHash;
        public byte version;
        public byte unk0;
        public byte unk1;
        public byte blockSizeKB;
        public byte offsetBytes;
        public byte sizeBytes;
        public byte keySizeInBytes;
        public byte checksumSize;
        public uint numElements;
        public byte[] footerChecksum;
    }

    public struct BLTEChunkInfo
    {
        public bool isFullChunk;
        public int compSize;
        public int decompSize;
        public byte[] checkSum;
    }

    public struct PatchFile
    {
        public byte version;
        public byte fileKeySize;
        public byte sizeB;
        public byte patchKeySize;
        public byte blockSizeBits;
        public ushort blockCount;
        public byte flags;
        public byte[] encodingContentKey;
        public byte[] encodingEncodingKey;
        public uint decodedSize;
        public uint encodedSize;
        public byte especLength;
        public string encodingSpec;
        public PatchBlock[] blocks;
    }

    public struct PatchBlock
    {
        public byte[] lastFileContentKey;
        public byte[] blockMD5;
        public uint blockOffset;
        public BlockFile[] files;
    }

    public struct BlockFile
    {
        public byte numPatches;
        public byte[] targetFileContentKey;
        public ulong decodedSize;
        public FilePatch[] patches;
    }

    public struct FilePatch
    {
        public byte[] sourceFileEncodingKey;
        public ulong decodedSize;
        public byte[] patchEncodingKey;
        public uint patchSize;
        public byte patchIndex;
    }
}
