# BackgroundMonitorService

A Windows service that monitors the `C:\MonitorFolder` directory for file changes, creating backups and logging events.

## Features
- Monitors file creations, deletions, modifications and renaming.
- Backup files with timestamped names in `C:\Backup` with the Cleanup Retention Days feature.
- N.B.: the prject utilises hard-coded paths in config.json file such as: 'FolderToMonitor', 'BackupFolder' and 'LogFilePath', so feel free to test the project with your own folders by manually create them wherever you want.

## How to Install/Use
1. Clone this repository.
2. Open the solution in Visual Studio.
3. Build the project in Release mode.
4. After building, the BackgroundMonitorService.exe will be located in the bin\Release folder within your project directory.
5. Install the service using `installutil`, so open the cmd in administrator and run installutil.exe "C:\your_path\bin\Release\BackgroundMonitorService.exe"
6. Start the service via Windows Services Manager.
7. Configure backup paths and logging in the configuration file.
