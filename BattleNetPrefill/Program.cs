using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CliFx;

namespace BattleNetPrefill
{
    // TODO - Tech Debt - Cleanup trim/single file warnings
    // TODO - Tech Debt - Dotnet 7 - See if AOT improvements help performance
    // TODO - Documentation - Document where + what is stored in the /cache dir
    // TODO - Documentation - Update documentation to show why you should be using UTF16.  Include an image showing before/after.  Also possibly do a check on startup?
    // TODO - Documentation - Add battle.net slow download to known issues page ? https://lancache.net/docs/common-issues/
    // TODO - Resolve issues on Github issues
    // TODO - Allocations - https://github.com/bretcope/PerformanceTypes
    // TODO - Metrics - Setup and configure Github historical statistics (Downloads, page views, etc).  https://github.com/jgehrcke/github-repo-stats
    // TODO - General - Promote this app on r/lanparty and discord
    // TODO - Spectre - Once pull request has been merged into Spectre, remove reference to forked copy of the project
    // TODO - Spectre - Documentation on website needs to be updated to include changes
    public static class Program
    {
        public static async Task<int> Main()
        {
            var executableName = "BattleNetPrefill";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                executableName = "BattleNetPrefill.exe";
            }
            return await new CliApplicationBuilder()
                         .AddCommandsFromThisAssembly()
                         .SetTitle("BattleNetPrefill")
                         .SetExecutableName(executableName)
                         .Build()
                         .RunAsync();
        }
    }
}