using Microsoft.Extensions.Configuration;

namespace BuildBackup
{
    public static class SettingsManager
    {
        public static string cacheDir;

        static SettingsManager()
        {
            LoadSettings();
        }

        public static void LoadSettings()
        {
            var config = new ConfigurationBuilder().AddJsonFile("config.json", optional: false, reloadOnChange: false).Build();
            cacheDir = config.GetSection("config")["cacheDir"];
        }
    }
}