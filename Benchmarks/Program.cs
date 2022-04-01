using System;
using BattleNetPrefill.Structs;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Benchmarks
{
    public class Program
    {
        public class Md5Hash_HashCode
        {
            private readonly MD5Hash md5hash;


            public Md5Hash_HashCode()
            {
                md5hash = new MD5Hash(6051113216891152126L, 49107880105117937L);
            }

            //TODO improve upon this
            [Benchmark]
            public int Current()
            {
                return md5hash.GetHashCode();
            }

            [Benchmark]
            public int BuiltIn()
            {
                //TODO benchmark this some more
                return HashCode.Combine(md5hash.lowPart, md5hash.highPart); 
            }
        }

        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
        }
    }
}
