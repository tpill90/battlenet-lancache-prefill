using System;
using System.IO;

namespace BattleNetPrefill
{
    public static class Config
    {
        static Config()
        {
            if (!Directory.Exists(CacheDir))
            {
                Directory.CreateDirectory(CacheDir);
            }
        }

        public static readonly Uri BattleNetPatchUri = new Uri("http://us.patch.battle.net:1119");

        //TODO comment
        public static string CacheDir => "cache";

        public static DebugConfig DebugConfig = new DebugConfig()
        {

        };

        //TODO move these log files to a root directory called "Logs".  Also remove the hardcoded path and use a relative path
        public static readonly string LogFileBasePath = @"C:\Users\Tim\Dropbox\Programming\dotnet-public\BattleNetBackup\RequestReplayer\Logs";
        
        public static bool UseCdnDebugMode = false;
        public static bool ShowDebugStats = false;

        public static bool WriteOutputFiles = false;
    }

    // TODO comment
    public class DebugConfig
    {

    }
}
