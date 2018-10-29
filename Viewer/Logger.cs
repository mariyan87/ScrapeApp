using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer
{
    public class Logger
    {
        private static string logFile = "Log-" + DateTime.Now.Date.ToString("yyyy-MM-dd") + ".txt";

        public static void Write(string message)
        {
            File.WriteAllText(logFile, DateTime.Now + ": " + message + Environment.NewLine);
        }

        public static void Write(string message, Exception ex)
        {
            File.WriteAllText(logFile, DateTime.Now + ": " + message + Environment.NewLine + ex);
        }

        public static void Write(Exception ex)
        {
            File.WriteAllText(logFile, DateTime.Now + ": " + ex);
        }
    }
}
