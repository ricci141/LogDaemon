namespace Workers.Models;

public sealed class AppSettings
{
    public string ProcessName { get; set; } = string.Empty;
    
    public string ChatLogPath { get; set; } = string.Empty;
    
    public string BackupDirectory { get; set; } = string.Empty;
    
    public string HashIndexPath { get; set; } = string.Empty;
    
    public int CheckIntervalMs { get; set; }
    
    public int MaxBackups { get; set; }
    
    public bool AutoStart { get; set; }
}