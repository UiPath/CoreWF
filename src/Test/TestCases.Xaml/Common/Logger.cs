using System;

namespace TestCases.Xaml.Common
{
    public class Logger
    {
        public static void Trace(string source, string format, params object[] args)
        {
            Trace(source, string.Format(format, args));
        }

        public static void Trace(string source, string message)
        {
            string messageToTrace = string.Format("[{0}] {1}", source, message);

            if (Environment.GetEnvironmentVariable(Global.ToConsoleEnvironmentVariable) != null)
            {
                Console.WriteLine(messageToTrace);
            }
            else
            {
                //Log.Info(messageToTrace);
            }
        }
    }
}
