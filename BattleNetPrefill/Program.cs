using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CliFx;

namespace BattleNetPrefill
{
    // TODO - Tech Debt - Cleanup trim/single file warnings
    // TODO - Tech Debt - Dotnet 7 - See if AOT improvements help performance
    // TODO - Documentation - Document where + what is stored in the /cache dir
    // TODO - Resolve issues on Github issues
    // TODO - Allocations - https://github.com/bretcope/PerformanceTypes
    // TODO - Allocations - https://mgravell.github.io/PooledAwait/
    // TODO - Delete request replayer
    // TODO - Spectre - Once pull request has been merged into Spectre, remove reference to forked copy of the project
    // TODO - Spectre - Documentation on website needs to be updated to include changes
    // TODO - General - Write check to see if a newer version is available, and display a message to the user if there is
    // TODO - Documentation - Update documentation to show why you should be using UTF16.  Include an image showing before/after.  Also possibly do a check on startup?
    // TODO - Metrics - Setup and configure Github historical statistics (Downloads, page views, etc).  This will be useful for seeing project usage.
    // TODO - General - Promote this app on r/lanparty and discord
    public static class Program
    {
        public static async Task<int> Main()
        {
            var cliBuilder = new CliApplicationBuilder()
                         .AddCommandsFromThisAssembly()
                         .SetTitle("BattleNetPrefill")
                         .SetExecutableName("BattleNetPrefill");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                cliBuilder.SetExecutableName("BattleNetPrefill.exe");
            }

            return await cliBuilder
                         .Build()
                         .RunAsync();
        }
    }
}