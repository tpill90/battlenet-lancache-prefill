using System.IO;
using System.Threading.Tasks;
using BattleNetPrefill;
using BattleNetPrefill.EncryptDecrypt;
using BattleNetPrefill.Handlers;
using BattleNetPrefill.Parsers;
using BattleNetPrefill.Structs;
using BattleNetPrefill.Web;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class BlteBenchmarks
    {
        private byte[] fileContents;

        public BlteBenchmarks()
        {
            var downloadFilePath = @"C:\Users\Tim\Dropbox\Programming\dotnet-public\BattleNetBackup\BattleNetPrefill\bin\Debug\net5.0\cache\tpr\wow\data\a0\cc\a0cca39fa16d8a4e694504ea42109546";
            fileContents = File.ReadAllBytes(downloadFilePath);
        }

        //[Benchmark(Baseline = true)]
        //public byte[] Original()
        //{
        //    return BLTE_Original.Parse(fileContents);
        //}

        //[Benchmark]
        //public void New()
        //{
        //    var resultStream = BLTE.Parse(fileContents);
        //    resultStream.Close();
        //}
    }
}
