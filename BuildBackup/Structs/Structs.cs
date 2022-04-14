using System;
using System.Linq;
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

        public int fileCount;

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

        public int totalArchivedFileCount => archives.Sum(e => e.fileCount);

        public string archiveGroup;

        public string[] patchArchives;
        public int[] patchArchivesIndexSize;
        public string patchArchiveGroup;

        public string[] builds;

        public string fileIndex;
        public string fileIndexSize;

        public string patchFileIndex;
        public string patchFileIndexSize;
    }

    //TODO comment + rename
    public struct IndexEntry
    {
        public short index;

        /// <summary>
        /// CdnHash of the archive index that holds this file.
        /// </summary>
		//TODO consider removing to reduce the # of allocations
        public string IndexId;

        public uint offset;
        public uint size;

        public override string ToString()
        {
            return $"{IndexId} Offset: {offset} size: {size}";
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

    [Flags]
    public enum LocaleFlags : uint
    {
        All = 0xFFFFFFFF,
        None = 0,
        Unk_1 = 0x1,
        enUS = 0x2,
        koKR = 0x4,
        Unk_8 = 0x8,
        frFR = 0x10,
        deDE = 0x20,
        zhCN = 0x40,
        esES = 0x80,
        zhTW = 0x100,
        enGB = 0x200,
        enCN = 0x400,
        enTW = 0x800,
        esMX = 0x1000,
        ruRU = 0x2000,
        ptBR = 0x4000,
        itIT = 0x8000,
        ptPT = 0x10000,
        enSG = 0x20000000, // custom
        plPL = 0x40000000, // custom
        All_WoW = enUS | koKR | frFR | deDE | zhCN | esES | zhTW | enGB | esMX | ruRU | ptBR | itIT | ptPT
    }

    [Flags]
    public enum ContentFlags : uint
    {
        None = 0,
        F00000001 = 0x1,
        F00000002 = 0x2,
        F00000004 = 0x4,
        LoadOnWindows = 0x8, // added in 7.2.0.23436
        LoadOnMacOS = 0x10, // added in 7.2.0.23436
        LowViolence = 0x80, // many models have this flag
        DoNotLoad = 0x100,
        F00000200 = 0x200,
        F00000400 = 0x400,
        F00000800 = 0x800,
        F00001000 = 0x1000,
        F00002000 = 0x2000,
        F00004000 = 0x4000,
        F00008000 = 0x8000,
        F00010000 = 0x10000,
        F00020000 = 0x20000,
        F00040000 = 0x40000,
        F00080000 = 0x80000,
        F00100000 = 0x100000,
        F00200000 = 0x200000,
        F00400000 = 0x400000,
        F00800000 = 0x800000,
        F01000000 = 0x1000000,
        F02000000 = 0x2000000,
        F04000000 = 0x4000000,
        Encrypted = 0x8000000,
        NoNames = 0x10000000,
        F20000000 = 0x20000000, // added in 21737
        Bundle = 0x40000000,
        NoCompression = 0x80000000 // sounds have this flag
    }
}
