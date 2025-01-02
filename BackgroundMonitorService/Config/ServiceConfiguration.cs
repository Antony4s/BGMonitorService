using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackgroundMonitorService.Config
{
    public class ServiceConfiguration
    {
        public string FolderToMonitor { get; set; }
        public string BackupFolder { get; set; }
        public string LogFilePath { get; set; }
        public int CleanupRetentionDays { get; set; }
        public List<string> FileExtensionsToBackup { get; set; }
        public int MaxRetries { get; set; }
        public int RetryDelay { get; set; }
    }

    public static class ConfigurationHelper
    {
        public static ServiceConfiguration GetConfiguration()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "Config.json");
            string json = File.ReadAllText(configPath);
            return JsonConvert.DeserializeObject<ServiceConfiguration>(json);
        }
    }
}
