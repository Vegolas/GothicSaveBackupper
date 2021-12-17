using static GothicSaveBackupper.Helpers;
namespace GothicSaveBackupper;
public class BackupManager
{
    public FileSystemWatcher FileSystemWatcher { get; init; }
    private DateTime _lastBackupTime = DateTime.Today;
    private string _backupFolderName = "Backups";

    public BackupManager(string backupFolderName)
    {
        _backupFolderName = backupFolderName;

        var fsw = new FileSystemWatcher(AppDomain.CurrentDomain.BaseDirectory);
        fsw.Changed += new FileSystemEventHandler(fsw_Changed);
        fsw.Created += new FileSystemEventHandler(fsw_Changed);
        fsw.Renamed += new RenamedEventHandler(fsw_Renamed);
        fsw.Deleted += new FileSystemEventHandler(fsw_Deleted);
        
        ConsoleWriteLine("File system watcher created.");
        CreateBackupFolder();
        FileSystemWatcher = fsw;
    }


    void fsw_Deleted(object sender, FileSystemEventArgs e)
    {
        if (e.Name.Equals(_backupFolderName) && (e.ChangeType == WatcherChangeTypes.Deleted || e.ChangeType == WatcherChangeTypes.Renamed))
            FixBackupFolder();
        else
            ConsoleWriteLine($"[File {e.ChangeType}] {e.FullPath}{Environment.NewLine}Deleted files cannot be backed up.", ConsoleColor.Red);
    }

    async void fsw_Changed(object sender, FileSystemEventArgs e)
    {
        await Task.Delay(250);

        FileInfo fi = new FileInfo(e.FullPath);

        if (e.Name.Equals(_backupFolderName) && (e.ChangeType == WatcherChangeTypes.Deleted || e.ChangeType == WatcherChangeTypes.Renamed))
            FixBackupFolder();

        if (fi is null || string.IsNullOrEmpty(fi.Extension))
            return;

        if ((DateTime.Now - _lastBackupTime).TotalSeconds < 3)
            return;

        ConsoleWriteLine($"[File {e.ChangeType}] {e.FullPath}");

        _lastBackupTime = DateTime.Now;
        await CopyFileToBackupFolder(fi);
    }

    void FixBackupFolder()
    {
        ConsoleWriteLine($"[Folder change detected]: Backup folder has been deleted or renamed. Trying to fix...", ConsoleColor.Red);
        CreateBackupFolder();
    }

    void fsw_Renamed(object sender, RenamedEventArgs e)
    {
        if (e.OldName.Equals(_backupFolderName) && (e.ChangeType == WatcherChangeTypes.Deleted || e.ChangeType == WatcherChangeTypes.Renamed))
            FixBackupFolder();
        else
            ConsoleWriteLine($"[File {e.ChangeType}] {e.FullPath}{Environment.NewLine}Renamed files are not being backed up.", ConsoleColor.DarkMagenta);
    }

    async Task<bool> CopyFileToBackupFolder(FileInfo fileInfo)
    {
        var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
        await Task.Delay(500);
        try
        {
            var backupFileName = $"{_backupFolderName}\\{fileName} {DateTime.Now:dd mm yyyy HH mm ss}{fileInfo.Extension}";
            File.Copy(fileInfo.Name, backupFileName);
            ConsoleWriteLine($"[Backup created]: {backupFileName}", ConsoleColor.Green);
            return true;
        }
        catch (IOException iex)
        {
            ConsoleWriteLine($"[IOError] {iex.Message}", ConsoleColor.Red);
        }
        catch (Exception ex)
        {
            ConsoleWriteLine($"[Error] {ex.Message}", ConsoleColor.Red);
        }

        return false;
    }

    void CreateBackupFolder()
    {
        if (Directory.Exists(_backupFolderName) == false)
        {
            Directory.CreateDirectory(_backupFolderName);
            ConsoleWriteLine($"Created directory {_backupFolderName}.", ConsoleColor.DarkGreen);
        }
        else
            ConsoleWriteLine($"Directory {_backupFolderName} alredy exists.", ConsoleColor.DarkGreen);
    }
}
