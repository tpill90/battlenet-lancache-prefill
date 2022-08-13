using System.Threading.Tasks;
using CliFx;

namespace BattleNetPrefill
{
    //TODO rework the colors in this app to more closely match the colors used in SteamPrefill

    // TODO - General - Promote this app on r/lanparty
    // TODO - Spectre - Once pull request has been merged into Spectre, remove reference to forked copy of the project
    // TODO - I wish there was a way to color the help text output from CliFx.  Everything is so flat, and cant draw attention to important parts
    public static class Program
    {
        public static async Task<int> Main()
        {
            var description = "Automatically fills a Lancache with games from Battle.net, so that subsequent downloads will be \n" +
                              "  served from the Lancache, improving speeds and reducing load on your internet connection.";
            return await new CliApplicationBuilder()
                         .AddCommandsFromThisAssembly()
                         .SetTitle("BattleNetPrefill")
                         .SetExecutableName($"BattleNetPrefill{(OperatingSystem.IsWindows() ? ".exe" : "")}")
                         .SetDescription(description)
                         .Build()
                         .RunAsync();
        }
    }
}