using System.IO;

namespace BuildBackup
{
    public static class Logger
    {
        private static StreamWriter writer;

        static Logger()
        {
            writer = new StreamWriter(File.Open("errors.txt", FileMode.OpenOrCreate, FileAccess.Write)) { AutoFlush = true };
        }

        public static void WriteLine(string line)
        {
            writer.WriteLine(line);
        }
    }
}
