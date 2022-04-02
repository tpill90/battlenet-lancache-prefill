using System;
using System.IO;

namespace BuildBackup.Utils
{
    //TODO remove
    public static class Logger
    {
        private static StreamWriter writer;

        static Logger()
        {
            writer = new StreamWriter(File.Open("errors.txt", FileMode.OpenOrCreate, FileAccess.Write)) { AutoFlush = true };
        }

        public static void WriteLine(string line, bool output = false)
        {
            writer.WriteLine(line);
            if (output)
            {
                Console.WriteLine(line);
            }
        }
    }
}
