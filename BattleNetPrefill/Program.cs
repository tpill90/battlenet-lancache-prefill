using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CliFx;

namespace BattleNetPrefill
{
    // TODO - Feature - Only run the prefill if there is a newer version on the CDN than the previously pre-filled version.  Add --force flag to override
    // TODO - Bug - Look into 502 bad gateway error.  Possibly work on a queue/retry that handles these with increasing wait periods
    // TODO - Bug - Work on reducing the wild fluctuations that can happen when re-prefilling a product.  Ex. zeus will bounce between 100mbs -> 1.1gbs
    // TODO - Add documentation to readme, on how to add Unicode support to Windows.  Document that by configuring this support, CLI output will look much nicer
    //      https://spectreconsole.net/best-practices
    // TODO Tech Debt - Add more Roslyn analyzers
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