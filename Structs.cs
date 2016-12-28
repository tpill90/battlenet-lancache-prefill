using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildBackup
{
    public struct versionsFile
    {
        public versionsEntry[] entries;
    }

    public struct versionsEntry
    {
        public string region;
        public string buildConfig;
        public string cdnConfig;
        public string buildId;
        public string versionsName;
        public string productConfig;
        public string keyRing;
    }

    public struct cdnsFile
    {
        public cdnsEntry[] entries;
    }

    public struct cdnsEntry
    {
        public string name;
        public string path;
        public string[] hosts;
    }

    public struct buildConfigFile
    {
        public string root;
        public string download;
        public string install;
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
        public string installSize;
        public string downloadSize;
        public string partialPriority;
        public string partialPrioritySize;
    }

    public struct cdnConfigFile
    {
        public string[] archives;
        public string archiveGroup;
        public string[] patchArchives;
        public string patchArchiveGroup;
        public string[] builds;
    }

    public struct archiveIndex
    {
        public string name;
        public archiveIndexEntry[] archiveIndexEntries;
    }

    public struct archiveIndexEntry
    {
        public string headerHash;
        public uint offset;
        public uint size;
    }

    public struct encodingFile
    {
        public byte unk1;
        public byte checksumSizeA;
        public byte checksumSizeB;
        public ushort flagsA;
        public ushort flagsB;
        public uint numEntriesA;
        public uint numEntriesB;
        public byte unk2;
        public int stringBlockSize;
        public encodingHeaderEntry[] headers;
        public encodingFileEntry[] entries;

    }

    public struct encodingHeaderEntry
    {
        public string firstHash;
        public string checksum;
    }

    public struct encodingFileEntry
    {
        public ushort keyCount;
        public uint size;
        public string hash;
        public string key;
    }

    public struct installFile
    {
        public uint unk;
        public uint numEntries;
        public installHeaderEntry[] headers;
        public installFileEntry[] entries;
    }

    public struct installHeaderEntry
    {

    }

    public struct installFileEntry
    {

    }

    public struct downloadFile
    {
        public byte[] unk;
        public uint numEntries;
        public uint numTags;
        public downloadEntry[] entries;
    }

    public struct downloadEntry
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

    public struct rootBlock
    {
        public ContentFlags contentFlags;
        public LocaleFlags localeFlags;
    }

    public struct rootEntry
    {
        public rootBlock block;
        public int fileDataID;
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
        LowViolence = 0x80, // many models have this flag
        NoCompression = 0x80000000 // sounds have this flag
    }
}
