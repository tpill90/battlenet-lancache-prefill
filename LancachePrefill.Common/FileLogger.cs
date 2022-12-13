namespace LancachePrefill.Common
{
    public static class FileLogger
    {
        private static string _logFilePath = "app.log";

        //TODO switch this over to something better.  Using this for the time being because I don't feel like introducing interfaces for something simple.
        public static bool RunningUnitTests { get; set; }

        //TODO need to move this over to using a logging library, rather than doing this manually
        public static void Log(string message)
        {
            if (RunningUnitTests)
            {
                return;
            }
            var messageNoAnsi = message.RemoveMarkup();
            File.AppendAllText(_logFilePath, $"[{DateTime.Now.ToString("h:mm:ss tt")}] {messageNoAnsi}\n");
        }

        public static void LogException(string message, Exception e)
        {
            Log(message);
            Log(e.ToString());
        }
    }
}
