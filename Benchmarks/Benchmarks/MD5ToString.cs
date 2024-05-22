using Benchmarks.Implementations;
using MD5 = Benchmarks.Implementations.MD5;

namespace Benchmarks.Benchmarks
{
    [MemoryDiagnoser]
    // ReSharper disable once ClassCanBeSealed.Global - Can't be sealed otherwise Benchmark.NET can't do its setup.
    public class MD5ToString
    {
        private const int Iterations = 1_500_000;

        private readonly MD5[] _inputMD5 = new MD5[Iterations];
        private readonly byte[][] _inputsAsByteArray = new byte[Iterations][];

        public MD5ToString()
        {
            var random = new Random();
            for (int i = 0; i < Iterations; i++)
            {
                // Creating test MD5
                var md5 = new MD5((ulong)random.NextInt64(), (ulong)random.NextInt64());
                _inputMD5[i] = md5;

                // Converting back to byte arrays for the methods that need it
                var bytes = new List<byte>();
                bytes.AddRange(BitConverter.GetBytes(md5.lowPart));
                bytes.AddRange(BitConverter.GetBytes(md5.highPart));
                _inputsAsByteArray[i] = bytes.ToArray();
            }
        }

        [Benchmark(Baseline = true)]
        public void Hexmate()
        {
            foreach (var bytes in _inputsAsByteArray)
            {
                HexMate.Convert.ToHexString(bytes);
            }
        }

        [Benchmark]
        public void HexmateConvertStructToByteArray()
        {
            foreach (var md5 in _inputMD5)
            {
                ToStringImplementations.HexmateConvertStructToByteArray(md5);
            }
        }

        [Benchmark]
        public void BitConverterToString()
        {
            foreach (var bytes in _inputsAsByteArray)
            {
                ToStringImplementations.BitConverterToString(bytes);
            }
        }

        [Benchmark]
        public void StringCreate()
        {
            foreach (var md5 in _inputMD5)
            {
                ToStringImplementations.StringCreate(md5.lowPart, md5.highPart);
            }
        }
    }
}