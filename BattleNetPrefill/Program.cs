using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CliFx;

namespace BattleNetPrefill
{
    // TODO - Bug - Work on reducing the wild fluctuations that can happen when re-prefilling a product.  Ex. zeus will bounce between 100mbs -> 1.1gbs
    // TODO Tech Debt - Add more Roslyn analyzers
    // TODO Build Pipeline - Get a basic build pipeline setup in GitHub
    // TODO Build Pipeline - Add dotnet format + configuration to this project.  Run it as a build stage
    // TODO Build Pipeline - Look into having 
    // TODO Tech Debt - Upgrade to dotnet 6.  Compare performance increase, if any.  Compare SingleFile disk size versus dotnet 5
    // TODO Tech Debt - Consider getting some actual test coverage on this project.  Exclude the "Download" tests, since they're not techinically "unit tests"
    // TODO Performance - Research buffer pools to see how they might be able to reduce allocations https://www.google.com/search?client=firefox-b-1-d&q=c%23+binaryprimitives
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