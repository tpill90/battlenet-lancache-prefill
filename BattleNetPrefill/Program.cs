using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CliFx;

namespace BattleNetPrefill
{
    //TODO Add more analyzers
    //TODO add documentation on how to add unicode support to windows https://spectreconsole.net/best-practices
    //TODO research buffer pools https://www.google.com/search?client=firefox-b-1-d&q=c%23+binaryprimitives
    //TODO Add a feature that only runs the prefill if there is a newer version than the previously prefilled version.  Add --force flag to override
    //TODO Upgrade to dotnet 6.  Compare performance increase, if any
    //TODO Consider getting some actual test coverage on this project.  Exclude the "Download" tests, since they're not techinically "unit tests"
    //TODO work on reducing the wild fluctuations that can happen when re-prefilling a product.  Ex. zeus will bounce between 100mbs -> 1.1gbs
    //TODO rename publish folder to not be BattleNetBackup, maybe battlenet prefill
    //TODO did Wow get slower?  Currently 1.3 seconds, I thought it was sub 1 second before.
    //TODO Look into 502 bad gateway error.  Possibly work on a queue/retry that handles these with increasing wait periods
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