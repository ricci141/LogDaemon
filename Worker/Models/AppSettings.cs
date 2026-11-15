namespace Worker.Models;

public sealed class AppSettings
{
    public readonly int CheckIntervalMs = 10000;

    public readonly string BackupDirectory = Path.Combine(AppContext.BaseDirectory, "SavedLogs");

    public readonly string HashIndexPath = Path.Combine(AppContext.BaseDirectory, "hashes.json");

    public string ProcessName { get; set; } = string.Empty;

    public string ChatLogPath { get; set; } = string.Empty;

    // public int MaxBackups { get; set; }

    // public bool AutoStart { get; set; }
}