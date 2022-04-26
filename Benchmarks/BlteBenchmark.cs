using System.IO;
using BattleNetPrefill.EncryptDecrypt;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    public partial class Program
    {
        public class BlteBenchmark
        {
            private readonly byte[] content;

            public BlteBenchmark()
            {
                var rootPath = @"C:\Users\Tim\Dropbox\Programming\dotnet-public\BattleNetBackup\BattleNetPrefill\bin\Release\net5.0\cache";
                var filePath = @$"{rootPath}/tpr/sc2/data/08/4c/084c746ee0aa1b7c13868b44788605d4";
                content = File.ReadAllBytes(filePath);
            }

            [Benchmark]
            public byte[] ArrayBased()
            {
                return BLTE.Parse(content);
            }
        }
    }
}
