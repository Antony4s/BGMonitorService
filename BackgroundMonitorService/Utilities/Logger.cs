using BackgroundMonitorService.Config;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace BackgroundMonitorService.Utilities
{
    public static class Logger
    {
        static ServiceConfiguration config = ConfigurationHelper.GetConfiguration();
        private static readonly string LogFilePath = config.LogFilePath;

        public static void LogEvent(string message)
        {
            // Ensure the log directory exists
            string directoryPath = Path.GetDirectoryName(LogFilePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath); // Create directory if it doesn't exist
            }

            // Append log message to the file
            File.AppendAllText(LogFilePath, DateTime.Now + " - " + message + Environment.NewLine);
        }
    }
}
