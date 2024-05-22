namespace Benchmarks.Implementations
{
    public readonly struct MD5
    {
        public readonly ulong lowPart;
        public readonly ulong highPart;

        public MD5(ulong lowPart, ulong highPart)
        {
            this.lowPart = lowPart;
            this.highPart = highPart;
        }
    }
}