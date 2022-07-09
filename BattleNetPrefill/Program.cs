using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CliFx;

namespace BattleNetPrefill
{
    // TODO - Feature - Possibly add a clear cache command?
    // TODO - Tech Debt - Cleanup trim/single file warnings
    // TODO - Tech Debt - Dotnet 7 - See if AOT improvements help performance
    // TODO - Documentation - Add battle.net slow download to known issues page ? https://lancache.net/docs/common-issues/
    // TODO - Resolve issues on Github issues
    // TODO - General - Promote this app on r/lanparty and discord
    // TODO - Test out https://github.com/microsoft/infersharpaction
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