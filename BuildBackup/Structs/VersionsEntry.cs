using Shared;
using Spectre.Console;

namespace BuildBackup.Structs
{
    public struct VersionsEntry
    {
        public string region;
        public string buildConfig;
        public string cdnConfig;
        public string buildId;
        public string versionsName;
        public string productConfig;
        public string keyRing;

        public void PrintTable()
        {
            //TODO move this to the VersionsEntry class
            // Formatting output to table
            var table = new Table();
            table.AddColumn(new TableColumn(SpectreColors.Blue("Version")).Centered());
            table.AddColumn(new TableColumn(SpectreColors.Blue("Region")).Centered());
            table.AddColumn(new TableColumn(SpectreColors.Blue("CDN Config Id")).Centered());
            table.AddColumn(new TableColumn(SpectreColors.Blue("Build Config Id")).Centered());
            table.AddRow(versionsName, region, cdnConfig, buildConfig);
            AnsiConsole.Write(table);
        }
    }
}