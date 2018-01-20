using System;
using System.Collections;
using System.Collections.Generic;

namespace BuildBackup
{
    public struct VersionsFile
    {
        public VersionsEntry[] entries;
    }

    public struct VersionsEntry
    {
        public string region;
        public string buildConfig;
        public string cdnConfig;
        public string buildId;
        public string versionsName;
        public string productConfig;
        public string keyRing;
    }

    public struct CdnsFile
    {
        public CdnsEntry[] entries;
    }

    public struct CdnsEntry
    {
        public string name;
        public string path;
        public string[] hosts;
        public string configPath;
    }

    public struct GameBlobFile
    {
        public string decryptionKeyName;
    }

    public struct BuildConfigFile
    {
        public string root;
        public string[] download;
        public string[] downloadSize;
        public string[] install;
        public string[] installSize;
        public string[] encoding;
        public string[] encodingSize;
        public string buildName;
        public string buildPlaybuildInstaller;
        public string buildProduct;
        public string buildUid;
        public string buildBranch;
        public string buildNumber;
        public string buildAttributes;
        public string buildComments;
        public string buildCreator;
        public string buildFixedHash;
        public string buildReplayHash;
        public string buildManifestVersion;
        public string patch;
        public string patchSize;
        public string patchConfig;
        public string partialPriority;
        public string partialPrioritySize;
    }

    public struct CDNConfigFile
    {
        public string[] archives;
        public string archiveGroup;
        public string[] patchArchives;
        public string patchArchiveGroup;
        public string[] builds;
    }

    public struct ArchiveIndexEntry
    {
        public short index;
        public uint offset;
        public uint size;
    }

    public struct EncodingFile
    {
        public byte unk1;
        public byte checksumSizeA;
        public byte checksumSizeB;
        public ushort flagsA;
        public ushort flagsB;
        public uint numEntriesA;
        public uint numEntriesB;
        public byte unk2;
        public ulong stringBlockSize;
        public string[] stringBlockEntries;
        public EncodingHeaderEntry[] aHeaders;
        public EncodingFileEntry[] aEntries;
        public EncodingHeaderEntry[] bHeaders;
        public EncodingFileDescEntry[] bEntries;
    }

    public struct EncodingHeaderEntry
    {
        public string firstHash;
        public string checksum;
    }

    public struct EncodingFileEntry
    {
        public ushort keyCount;
        public uint size;
        public string hash;
        public string key;
    }

    public struct EncodingFileDescEntry
    {
        public string key;
        public uint stringIndex;
        public ulong compressedSize;
    }

    public struct InstallFile
    {
        public byte hashSize;
        public ushort numTags;
        public uint numEntries;
        public InstallTagEntry[] tags;
        public InstallFileEntry[] entries;
    }

    public struct InstallTagEntry
    {
        public string name;
        public ushort type;
        public BitArray files;
    }

    public struct InstallFileEntry
    {
        public string name;
        public byte[] contentHash;
        public uint size;
        public List<string> tags;
    }

    public struct DownloadFile
    {
        public byte[] unk;
        public uint numEntries;
        public uint numTags;
        public DownloadEntry[] entries;
    }

    public struct DownloadEntry
    {
        public string hash;
        public byte[] unk;
    }

    public struct BLTEChunkInfo
    {
        public bool isFullChunk;
        public int inFileSize;
        public int actualSize;
        public byte[] checkSum;
    }

    public struct RootFile
    {
        public MultiDictionary<ulong, RootEntry> entries;
    }

    public struct RootEntry
    {
        public ContentFlags contentFlags;
        public LocaleFlags localeFlags;
        public ulong lookup;
        public uint fileDataID;
        public byte[] md5;
    }

    [Flags]
    public enum LocaleFlags : uint
    {
        All = 0xFFFFFFFF,
        None = 0,
        //Unk_1 = 0x1,
        enUS = 0x2,
        koKR = 0x4,
        //Unk_8 = 0x8,
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
        F00000008 = 0x8, // added in 7.2.0.23436
        F00000010 = 0x10, // added in 7.2.0.23436
        LowViolence = 0x80, // many models have this flag
        F10000000 = 0x10000000,
        F20000000 = 0x20000000, // added in 21737
        Bundle = 0x40000000,
        NoCompression = 0x80000000 // sounds have this flag
    }
}
