using System.IO;

namespace BuildBackup
{
    public static class Configuration
    {
        static Configuration()
        {
            if (!Directory.Exists(CacheDir))
            {
                Directory.CreateDirectory(CacheDir);
            }
        }

        //TODO comment
        public static string CacheDir => "cache";
    }
}
