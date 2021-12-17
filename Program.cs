using GothicSaveBackupper;
using static GothicSaveBackupper.Helpers;

ConsoleWriteLine("Start...");

var backupManager = new BackupManager("Backups");

ConsoleWriteLine("Application is ready.", ConsoleColor.Green);

Console.WriteLine();

while (true)
{
    backupManager.FileSystemWatcher.WaitForChanged(WatcherChangeTypes.All);
}


