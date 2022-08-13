using System.Threading.Tasks;
using CliFx;

namespace BattleNetPrefill
{

    // TODO - Documentation - Document process for updating app
    // TODO - readme - Update readme to match style of steam prefill readme
    // TODO - Remove personal machine build path from being displayed in exceptions when they are thrown
    // TODO - Feature - Consider implementing a multi-select command for interactively choosing which products to prefill.  Similar to steamPrefill.
    // TODO - Tech Debt - Cleanup trim/single file warnings
    // TODO - Feature - Possibly add a clear cache command
    // TODO - General - Promote this app on r/lanparty

    // TODO - Spectre - Once pull request has been merged into Spectre, remove reference to forked copy of the project
    // TODO - Spectre - Documentation on website needs to be updated to include changes
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