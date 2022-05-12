using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CliFx;

namespace BattleNetPrefill
{
    // TODO - Bug - Work on reducing the wild fluctuations that can happen when re-prefilling a product.  Ex. zeus will bounce between 100mbs -> 1.1gbs
    // TODO Tech Debt - Add more Roslyn analyzers
    // TODO Build Pipeline - Look into code analysis pipeliens https://github.com/actions/starter-workflows/tree/main/code-scanning
    // TODO Build Pipeline - Add unit tests stage
    // TODO Build Pipeline - Add dotnet format + configuration to this project.  Run it as a build stage
    // TODO Build Pipeline - Look into having a build + publish stage to create a docker image.
    // TODO Tests - Add test case to Full Download tests, that compares actual download size vs real download size.  Wow Classic seems to be off
    // TODO Tech Debt - Upgrade to dotnet 6.  Compare performance increase, if any.  Compare SingleFile disk size versus dotnet 5
    // TODO Tech Debt - Consider getting some actual test coverage on this project.  Exclude the "Download" tests, since they're not techinically "unit tests"
    // TODO Performance - Research buffer pools to see how they might be able to reduce allocations https://www.google.com/search?client=firefox-b-1-d&q=c%23+binaryprimitives
    // TODO Performance - Analyze allocations with the .NET Object Allocation tool : https://devblogs.microsoft.com/visualstudio/net-object-allocation-tool-performance/
    // TODO Performance - Make sure all structs are being defined as readonly - https://devblogs.microsoft.com/premier-developer/avoiding-struct-and-readonly-reference-performance-pitfalls-with-errorprone-net/
    // TODO Modern warfare 2 seems to hang for some reason
    // TODO readd lancache docker windows setup script
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