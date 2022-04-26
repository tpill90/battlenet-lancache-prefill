using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CliFx;

namespace BattleNetPrefill
{
    //TODO Add more analyzers
    //TODO add documentation on how to add unicode support to windows https://spectreconsole.net/best-practices
    //TODO https://github.com/Tyrrrz/CliFx/wiki/Integrating-with-Spectre.Console
    //TODO research buffer pools https://www.google.com/search?client=firefox-b-1-d&q=c%23+binaryprimitives
    //TODO add a feature that only runs the prefill if there is a newer version than the previously prefilled version.  Add --force flag to override
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