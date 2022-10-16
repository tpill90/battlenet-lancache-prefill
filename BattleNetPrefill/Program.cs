namespace BattleNetPrefill
{
    // TODO - Update to dotnet 7 sdk + dotnet 7 target
    // TODO - Spectre - Once pull request has been merged into Spectre, remove reference to forked copy of the project
    // TODO - I wish there was a way to color the help text output from CliFx.  Everything is so flat, and cant draw attention to important parts
    // TODO - Determine if its possible to detect ipv6, and display a message to the user that ipv6 is not supported
    // TODO - In LancacheIpResolver.cs, change 127.0.0.1 over to say 'localhost' instead.
    // TODO - When running on the server, it doesn't seem to be detecting 127.0.0.1 correctly
    // TODO - Documentation - Install instructions.Possibly add the wget + unzip command as well for linux users?
    // TODO - Documentation - Add linux command examples to readme.
    // TODO - Port Lancache common over to this repo
    // TODO - Add publish build pipeline 
    public static class Program
    {
        public static async Task<int> Main()
        {
            var description = "Automatically fills a Lancache with games from Battle.net, so that subsequent downloads will be \n" +
                              "  served from the Lancache, improving speeds and reducing load on your internet connection.";
            return await new CliApplicationBuilder()
                         .AddCommandsFromThisAssembly()
                         .SetTitle("BattleNetPrefill")
                         .SetExecutableName($"BattleNetPrefill{(IsWindows() ? ".exe" : "")}")
                         .SetDescription(description)
                         .Build()
                         .RunAsync();
        }

        public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }
}