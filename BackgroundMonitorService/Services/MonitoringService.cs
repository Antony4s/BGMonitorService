using BackgroundMonitorService.Config;
using BackgroundMonitorService.Utilities;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;

namespace BackgroundMonitorService.Services
{
    public class MonitoringService
    {
        private FileSystemWatcher _watcher; // Watches the specified folder for file system changes
        private readonly BackupService _backupService; // Handles the backup operations
        private Timer _cleanupTimer; // Timer to trigger periodic cleanup operations
        private readonly ServiceConfiguration _config; // Configuration settings for the service

        // Constructor initializes dependencies and validates input
        public MonitoringService(BackupService backupService, ServiceConfiguration config)
        {
            _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        // Starts monitoring the configured folder
        public void StartMonitoring()
        {
            if (string.IsNullOrWhiteSpace(_config.FolderToMonitor))
            {
                throw new InvalidOperationException("Folder to monitor is not specified in the configuration.");
            }

            Logger.LogEvent($"Monitoring service started for folder: {_config.FolderToMonitor}");

            // Initialize the cleanup timer to run once every 24 hours
            _cleanupTimer = new Timer(CleanupCallback, null, TimeSpan.Zero, TimeSpan.FromHours(24));

            // Set up FileSystemWatcher to monitor file events
            _watcher = new FileSystemWatcher(_config.FolderToMonitor)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            // Attach event handlers for file events
            _watcher.Created += OnFileCreated;
            _watcher.Changed += OnFileChanged;
            _watcher.Deleted += OnFileDeleted;
            _watcher.Renamed += OnFileRenamed;
        }

        // Stops monitoring and disposes resources
        public void StopMonitoring()
        {
            try
            {
                Logger.LogEvent("Stopping monitoring service.");
                _cleanupTimer?.Change(Timeout.Infinite, 0); // Disable the timer
                _cleanupTimer?.Dispose(); // Dispose the timer
                _cleanupTimer = null;

                if (_watcher != null)
                {
                    _watcher.EnableRaisingEvents = false; // Stop listening for events
                    _watcher.Dispose(); // Release FileSystemWatcher resources
                }

                Logger.LogEvent("Monitoring service stopped.");
            }
            catch (Exception ex)
            {
                Logger.LogEvent($"Error stopping monitoring service: {ex.Message}");
            }
        }

        // Event handler for file creation
        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            HandleFileEvent(e.FullPath, "created");
        }

        // Event handler for file modification
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            HandleFileEvent(e.FullPath, "changed");
        }

        // Event handler for file deletion
        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            Logger.LogEvent($"File deleted: {e.FullPath}");
        }

        // Event handler for file renaming
        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            try
            {
                Logger.LogEvent($"File renamed from '{e.OldFullPath}' to '{e.FullPath}'");

                // Optionally backup both the old and new file paths
                if (ShouldBackup(e.FullPath))
                {
                    _backupService.BackupFile(e.FullPath);
                }

                if (ShouldBackup(e.OldFullPath))
                {
                    _backupService.BackupFile(e.OldFullPath);
                }
            }
            catch (Exception ex)
            {
                Logger.LogEvent($"Error handling file rename: {ex.Message}");
            }
        }

        // Centralized method for handling file events
        private void HandleFileEvent(string filePath, string eventType)
        {
            try
            {
                Logger.LogEvent($"File {eventType}: {filePath}");

                if (ShouldBackup(filePath))
                {
                    _backupService.BackupFile(filePath);
                }
                else
                {
                    Logger.LogEvent($"File skipped (unsupported extension): {filePath}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogEvent($"Error handling file {eventType} for '{filePath}': {ex.Message}");
            }
        }

        // Determines if a file should be backed up based on its extension
        private bool ShouldBackup(string filePath)
        {
            if (_config.FileExtensionsToBackup == null || !_config.FileExtensionsToBackup.Any())
            {
                Logger.LogEvent("No file extensions are configured for backup.");
                return false;
            }

            string extension = Path.GetExtension(filePath);
            return _config.FileExtensionsToBackup.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        // Callback method for periodic cleanup
        private void CleanupCallback(object state)
        {
            try
            {
                Logger.LogEvent("Performing periodic cleanup of old backups.");
                _backupService.CleanupOldBackups(_config.CleanupRetentionDays);
            }
            catch (Exception ex)
            {
                Logger.LogEvent($"Error during cleanup callback: {ex.Message}");
            }
        }
    }
}
