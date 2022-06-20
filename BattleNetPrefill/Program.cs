using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CliFx;

namespace BattleNetPrefill
{
    // TODO Tech Debt - Cleanup all Warnings + Messages
    // TODO Tech Debt - Dotnet 7 - See if AOT improvements help performance
    // TODO - Documentation - Document where + what is stored in the /cache dir
    // TODO - Resolve issues on Github issues
    // TODO - Allocations - https://github.com/bretcope/PerformanceTypes
    // TODO - Allocations - https://mgravell.github.io/PooledAwait/
    // TODO - Delete request replayer
    // TODO - Spectre - Once pull request has been merged into Spectre, remove reference to forked copy of the project
    // TODO - Spectre - Documentation on website needs to be updated to include changes
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