using System.Configuration;
using System.IO.Compression;
using static GothicSaveBackupper.Helpers;
namespace GothicSaveBackupper;
public class BackupManager
{
    public FileSystemWatcher FileSystemWatcher { get; init; }

    private readonly string[] IGNORE_DIRECTORIES = new string[] { "ref", "runtimes", "Backups" };

    private string safeDateTime => $"{DateTime.Now:dd MM yyyy HH mm ss}";
    private DateTime _lastBackupTime = DateTime.Today;
    private string _backupFolderName = "Backups";
    private List<string> _directories;
    private int _backupDelay = 10;

    public BackupManager(string backupFolderName)
    {
        _backupFolderName = backupFolderName;

        try
        {
            _backupDelay = Convert.ToInt32(ConfigurationManager.AppSettings["backupDelayTimeInSeconds"].ToString());
        }
        catch
        {
            _backupDelay = 10;
        }

        FileSystemWatcher = new(AppDomain.CurrentDomain.BaseDirectory);
        EnableFileWatcher();

        _directories = new();

        DiscoverFiles();

        ConsoleWriteLine("File system watcher created.");
        CreateBackupFolder();
    }

    private void DiscoverFiles()
    {
        foreach (var directory in Directory.GetDirectories(Directory.GetCurrentDirectory()))
        {
            var dirName = Path.GetFileName(directory);
            if (IGNORE_DIRECTORIES.Contains(dirName))
                continue;

            _directories.Add(directory);
            ConsoleWriteLine($"[Discovered Directory]: {Path.GetFileNameWithoutExtension(directory)}", ConsoleColor.Cyan);

            foreach (var file in Directory.GetFiles(directory, "*.SAV"))
                ConsoleWriteLine($"[Discovered File]: {Path.GetFileNameWithoutExtension(file)}", ConsoleColor.DarkCyan);
        }
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
        DisableFileWatcher();

        await Task.Delay(250);

        FileInfo fi = new FileInfo(e.FullPath);

        if (e.Name.Equals(_backupFolderName) && (e.ChangeType == WatcherChangeTypes.Deleted || e.ChangeType == WatcherChangeTypes.Renamed))
            FixBackupFolder();

        if (fi is null)
        {
            EnableFileWatcher();
            return;
        }

        if (string.IsNullOrEmpty(fi.Extension) && e.ChangeType != WatcherChangeTypes.Deleted)
        {
            if (_directories.Contains(e.FullPath) == false | Directory.GetFiles(e.FullPath).Count() == 0)
            {
                EnableFileWatcher();
                return;
            }

            BackupDirectory(e, fi);
        }

        EnableFileWatcher();
    }
    private void fsw_Created(object sender, FileSystemEventArgs e)
    {
        FileInfo fi = new FileInfo(e.FullPath);

        if (string.IsNullOrEmpty(fi.Extension))
        {
            _directories.Add(e.FullPath);
            ConsoleWriteLine($"[Discovered Directory]: {Path.GetFileNameWithoutExtension(e.FullPath)}", ConsoleColor.Cyan);
        }
    }

    private void DisableFileWatcher()
    {
        FileSystemWatcher.Changed -= new FileSystemEventHandler(fsw_Changed);
        FileSystemWatcher.Created -= new FileSystemEventHandler(fsw_Created);
        FileSystemWatcher.Renamed -= new RenamedEventHandler(fsw_Renamed);
        FileSystemWatcher.Deleted -= new FileSystemEventHandler(fsw_Deleted);
    }

    private void EnableFileWatcher()
    {
        FileSystemWatcher.Changed += new FileSystemEventHandler(fsw_Changed);
        FileSystemWatcher.Created += new FileSystemEventHandler(fsw_Created);
        FileSystemWatcher.Renamed += new RenamedEventHandler(fsw_Renamed);
        FileSystemWatcher.Deleted += new FileSystemEventHandler(fsw_Deleted);
    }

    private async void BackupDirectory(FileSystemEventArgs e, FileInfo fi)
    {
        ConsoleWriteLine($"[Backup Begin]: Save change has been detected. Backup in {_backupDelay} seconds...", ConsoleColor.Yellow);
        await Task.Delay(_backupDelay * 1000);
        var zipName = $@"{fi.Directory}\{_backupFolderName}\{fi.Name} {safeDateTime}.zip";

        ZipFile.CreateFromDirectory(fi.FullName, $@"{zipName}", CompressionLevel.Fastest, false);

        if (File.Exists(zipName) == false)
        {
            ConsoleWriteLine($"[Backup Failed]: '{zipName}' doesn't exist.", ConsoleColor.Red);
            return;
        }

        ConsoleWriteLine($"[Backup created]: {zipName}", ConsoleColor.Green);
    }

    private async void BackupFile(FileSystemEventArgs e, FileInfo fi)
    {
        ConsoleWriteLine($"[File {e.ChangeType}] {e.FullPath}");
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
        {
            if (_directories.Contains(Path.GetFileName(e.OldName)))
                _directories.Remove(Path.GetFileName(e.OldName));

            ConsoleWriteLine($"[File {e.ChangeType}] {e.FullPath}{Environment.NewLine}Renamed mapped directory.", ConsoleColor.DarkMagenta);

            _directories.Add(e.Name);
            ConsoleWriteLine($"[Discovered Directory]: {Path.GetFileNameWithoutExtension(e.FullPath)}", ConsoleColor.Cyan);
        }
    }

    async Task<bool> CopyFileToBackupFolder(FileInfo fileInfo, bool deleteOriginal = false)
    {
        var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
        await Task.Delay(500);
        try
        {
            var backupFileName = $"{_backupFolderName}\\{fileName} {safeDateTime}{fileInfo.Extension}";

            File.Copy(fileInfo.Name, backupFileName, true);

            if (deleteOriginal)
                File.Delete(fileInfo.Name);

            ConsoleWriteLine($"[Backup Created]: {backupFileName}", ConsoleColor.Green);
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
