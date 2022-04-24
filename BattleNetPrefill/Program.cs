using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CliFx;

namespace BattleNetPrefill
{
    //TODO Add more analyzers
    //TODO Readme.md needs to be heavily updated.  Needs documentation on what this program does, how to use it, how to compile it, acknowledgements, external docs, etc.
    //TODO Repo - Squash old commits + generally cleanup repo history
    //TODO add documentation on how to add unicode support to windows https://spectreconsole.net/best-practices
    //TODO figure out which license to use
    //TODO some products are still throwing errors when downloading
    //TODO make sure all products "complete" their progress bar correctly.  Some of them report that they are "finished" but still continue downloading something. This is likely due to unknown file sizes
    public class Program
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