namespace BattleNetPrefill
{
    // TODO - Documentation - Install instructions.Possibly add the wget + unzip command as well for linux users?
    // TODO - Documentation - Add linux command examples to readme.
    // TODO - Port Lancache common over to this repo
    // TODO - Add publish build pipeline 
    // TODO - Add summary table
    // TODO - Make sure that the prefill command has flag/option parity with steamprefill
    // TODO - Setup mkdocs and copy from SteamPrefill.  Update docs in general
    public static class Program
    {
        public static async Task<int> Main()
        {
            // Checking to see if the user double clicked the exe in Windows, and display a message on how to use the app
            OperatingSystemUtils.DetectDoubleClickOnWindows("BattleNetPrefill");

            //TODO dedupe exception handling at the top level.  Migrate to custom CLIFX binary just like SteamPrefill
            var description = "Automatically fills a Lancache with games from Battle.net, so that subsequent downloads will be \n" +
                              "  served from the Lancache, improving speeds and reducing load on your internet connection.";
            return await new CliApplicationBuilder()
                         .AddCommandsFromThisAssembly()
                         .SetTitle("BattleNetPrefill")
                         .SetExecutableName($"BattleNetPrefill{(IsWindows() ? ".exe" : "")}")
                         .SetDescription(description)
                         .SetVersion($"v{ThisAssembly.Info.InformationalVersion}")
                         .Build()
                         .RunAsync();
        }

        public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }
}