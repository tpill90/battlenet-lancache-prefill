using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CliFx;

namespace BattleNetPrefill
{
    // TODO Tests - Add test case to Full Download tests, that compares actual download size vs real download size.  Wow Classic seems to be off
    // TODO Tests - Add test case to Full Download tests, that compares tests w\o a size to real requests missing size
    // TODO Performance - Make sure all structs are being defined as readonly - https://devblogs.microsoft.com/premier-developer/avoiding-struct-and-readonly-reference-performance-pitfalls-with-errorprone-net/
    // TODO Build Pipeline - Look into having a build + publish stage to create a docker image.
    // TODO Tech Debt - Cleanup all Warnings + Messages
    // TODO Tech Debt - Dotnet 7 - See if AOT improvements help performance
    // TODO Open up a ticket with Spectre.Net, to switch the units from 1024^3 to 1000^3, since this is how most network traffic is measured
    // TODO - Documentation - Document where + what is stored in the /cache dir
    // TODO - Resolve issues on Github issues
    // TODO - Allocations - https://github.com/bretcope/PerformanceTypes
    // TODO - Allocations - https://mgravell.github.io/PooledAwait/
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