using BackgroundMonitorService.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace BackgroundMonitorService.Services
{
    public class BackupService
    {
        private readonly string _backupFolderPath; // Path where backups will be stored
        private readonly int _maxRetries; // Maximum retry attempts for file operations
        private readonly TimeSpan _retryDelay; // Delay between retry attempts

        /// <summary>
        /// Initializes a new instance of the BackupService class with required parameters.
        /// </summary>
        /// <param name="backupFolder">The folder path where backups will be saved.</param>
        /// <param name="maxRetries">Maximum retry attempts for backup operations.</param>
        /// <param name="retryDelay">Time delay between retry attempts.</param>
        public BackupService(string backupFolder, int maxRetries, TimeSpan retryDelay)
        {
            _backupFolderPath = backupFolder ?? throw new ArgumentNullException(nameof(backupFolder));
            _maxRetries = maxRetries > 0 ? maxRetries : 3;
            _retryDelay = retryDelay > TimeSpan.Zero ? retryDelay : TimeSpan.FromSeconds(5);
        }

        /// <summary>
        /// Backs up a specified file to the backup folder.
        /// </summary>
        /// <param name="sourceFilePath">Path of the file to back up.</param>
        public void BackupFile(string sourceFilePath)
        {
            try
            {
                // Check if the source file exists before proceeding
                if (!File.Exists(sourceFilePath))
                {
                    Logger.LogEvent($"File not found: {sourceFilePath}");
                    return;
                }

                // Perform the backup operation with retry logic
                ExecuteWithRetry(() =>
                {
                    string backupFilePath = GenerateBackupFilePath(sourceFilePath);
                    File.Copy(sourceFilePath, backupFilePath); // Copy file to backup folder
                    LogBackupAction(sourceFilePath, backupFilePath); // Log the successful backup
                }, "BackupFile", _maxRetries, _retryDelay);
            }
            catch (Exception ex)
            {
                // Log any unexpected exceptions during the backup process
                Logger.LogEvent($"Error during backup of '{sourceFilePath}': {ex.Message}");
            }
        }

        /// <summary>
        /// Cleans up old backup files that exceed the retention period.
        /// </summary>
        /// <param name="retentionDays">Number of days to retain backups before deletion.</param>
        public void CleanupOldBackups(int retentionDays)
        {
            try
            {
                // Verify that the backup folder exists
                if (!Directory.Exists(_backupFolderPath))
                {
                    Logger.LogEvent($"Backup folder does not exist: {_backupFolderPath}");
                    return;
                }

                DateTime retentionThreshold = DateTime.Now.AddDays(-retentionDays); // Calculate retention cutoff
                foreach (string file in Directory.GetFiles(_backupFolderPath))
                {
                    // Check the file's creation time against the retention threshold
                    if (File.GetCreationTime(file) < retentionThreshold)
                    {
                        File.Delete(file); // Delete files that exceed the retention period
                        Logger.LogEvent($"Deleted old backup: {file}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log any errors that occur during the cleanup process
                Logger.LogEvent($"Error during backup cleanup: {ex.Message}");
            }
        }

        /// <summary>
        /// Executes an operation with retry logic for specified retries and delay.
        /// </summary>
        /// <param name="operation">The operation to execute with retry.</param>
        /// <param name="operationName">The name of the operation for logging purposes.</param>
        /// <param name="maxRetries">Maximum retry attempts for the operation.</param>
        /// <param name="retryDelay">Time delay between retry attempts.</param>
        private void ExecuteWithRetry(Action operation, string operationName, int maxRetries, TimeSpan retryDelay)
        {
            int retryCount = 0;
            while (retryCount < maxRetries)
            {
                try
                {
                    // Try to execute the operation
                    operation();
                    return; // Exit if the operation succeeds
                }
                catch (IOException ex) when (ex.Message.Contains("locked"))
                {
                    // Handle file locking issues and retry
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        Logger.LogEvent($"Operation '{operationName}' failed after {maxRetries} retries.");
                        throw; // Re-throw the exception if max retries are reached
                    }

                    Logger.LogEvent($"Operation '{operationName}' failed on attempt {retryCount}. Retrying in {retryDelay.TotalSeconds} seconds...");
                    Thread.Sleep(retryDelay); // Wait before retrying
                }
                catch (Exception)
                {
                    // Log and re-throw any unhandled exceptions
                    Logger.LogEvent($"Unhandled exception during operation '{operationName}'.");
                    throw;
                }
            }
        }

        /// <summary>
        /// Generates a unique backup file path with a timestamp.
        /// </summary>
        /// <param name="sourceFilePath">Path of the source file to back up.</param>
        /// <returns>Unique backup file path including timestamp and source file name.</returns>
        private string GenerateBackupFilePath(string sourceFilePath)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss"); // Current timestamp
            string fileName = Path.GetFileName(sourceFilePath); // Extract the file name
            return Path.Combine(_backupFolderPath, $"{timestamp}_{fileName}"); // Combine with backup folder
        }

        /// <summary>
        /// Logs successful backup actions to file and event log.
        /// </summary>
        /// <param name="sourceFilePath">Path of the source file backed up.</param>
        /// <param name="backupFilePath">Path of the backup file created.</param>
        private void LogBackupAction(string sourceFilePath, string backupFilePath)
        {
            string logMessage = $"{DateTime.Now}: File '{sourceFilePath}' was backed up to '{backupFilePath}'.";
            Logger.LogEvent(logMessage); // Log the action to the logger

            try
            {
                File.AppendAllText(@"C:\ServiceLogs\backup_log.txt", logMessage + Environment.NewLine); // Log to file
            }
            catch (Exception ex)
            {
                Logger.LogEvent($"Failed to log backup action to file: {ex.Message}"); // Handle any logging errors
            }
        }
    }
}
