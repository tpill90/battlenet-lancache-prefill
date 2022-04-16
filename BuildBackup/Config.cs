using System;
using System.IO;

namespace BuildBackup
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

        public static bool ValidateData = false;

        public static readonly Uri BattleNetPatchUri = new Uri("http://us.patch.battle.net:1119");

        //TODO comment
        public static string CacheDir => "cache";

        public static readonly string LogFileBasePath = @"C:\Users\Tim\Dropbox\Programming\dotnet-public\BattleNetBackup\RequestReplayer\Logs";

        public static int PadRight = 31;
        public static int Padding = 31;
    }
}
