using BackgroundMonitorService.Config;
using BackgroundMonitorService.Services;
using BackgroundMonitorService.Utilities;
using System;
using System.Diagnostics;
using System.ServiceProcess;


namespace BackgroundMonitorService
{
    public partial class Service1 : ServiceBase
    {


        private MonitoringService _monitoringService;
        public Service1()
        {
            InitializeComponent();

        }

        protected override void OnStart(string[] args)
        {
            try
            {
                // Log that the service started successfully
                Logger.LogEvent("Service started.");

                // Load and validate the configuration for the service
                ServiceConfiguration config = ConfigurationHelper.GetConfiguration();
                ValidateConfiguration(config);
                // Initialize BackupService with the path to store backups (from the configuration)
                BackupService backupService = InitializeBackupService(config);

                // Initialize MonitoringService with the BackupService as a dependency
                _monitoringService = new MonitoringService(backupService, config);

                // Start monitoring the folder for changes (created, modified, deleted)
                _monitoringService.StartMonitoring();

                // Cleanup old backups based on retention days in the config
                PerformInitialCleanup(backupService, config.CleanupRetentionDays);

                Logger.LogEvent("Service started successfully.");
            }
            catch (Exception ex)
            {
                HandleExceptionDuringStart(ex);
            }

        }

        protected override void OnStop()
        {
            try
            {
                Logger.LogEvent("Service stopping.");
                _monitoringService?.StopMonitoring();
                Logger.LogEvent("Service stopped.");
            }
            catch (Exception ex)
            {
                Logger.LogEvent($"Error stopping the service: {ex.Message}");
                EventLog.WriteEntry("BackgroundMonitorService", $"Error stopping service: {ex.Message}", EventLogEntryType.Error);
            }
        }

        private void ValidateConfiguration(ServiceConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.BackupFolder) || string.IsNullOrWhiteSpace(config.FolderToMonitor))
                throw new ArgumentException("BackupFolder or MonitorFolder is not properly configured.");
        }

        private BackupService InitializeBackupService(ServiceConfiguration config)
        {
            Logger.LogEvent("Initializing BackupService.");
            return new BackupService(config.BackupFolder, config.MaxRetries, TimeSpan.FromSeconds(config.RetryDelay));
        }

        private void PerformInitialCleanup(BackupService backupService, int retentionDays)
        {
            Logger.LogEvent("Performing initial cleanup of old backups.");
            backupService.CleanupOldBackups(retentionDays);
        }

        private void HandleExceptionDuringStart(Exception ex)
        {
            Logger.LogEvent($"Error starting service: {ex.Message}");
            Logger.LogEvent($"StackTrace: {ex.StackTrace}");
            EventLog.WriteEntry("BackgroundMonitorService", $"Critical error during OnStart: {ex}", EventLogEntryType.Error);
        }
    }
}
