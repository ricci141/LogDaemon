using System.Security.Cryptography;
using System.Text.Json;
using Workers.Models;

namespace Workers.Services;

public interface IChatLogBackupService
{
    Task BackupAsync();
}

public sealed class ChatLogBackupService : IChatLogBackupService
{
    private readonly AppSettings _settings;
    private readonly ILogger<ChatLogBackupService> _logger;
    private readonly string _hashIndexPath;
    private readonly object _lock = new();

    public ChatLogBackupService(
        AppSettings settings,
        ILogger<ChatLogBackupService> logger)
    {
        _settings = settings;
        _logger = logger;
        _hashIndexPath = settings.HashIndexPath;
    }

    public async Task BackupAsync()
    {
        try
        {
            if (!File.Exists(_settings.ChatLogPath))
                return;

            string hash = await ComputeMd5Async(_settings.ChatLogPath);

            if (IsDuplicate(hash))
            {
                _logger.LogInformation("Duplicate file detected (hash: {Hash}), skipping backup", hash);
                return;
            }

            Directory.CreateDirectory(_settings.BackupDirectory);

            string fileName = $"chatlog_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

            string destination = Path.Combine(_settings.BackupDirectory, fileName);

            await using var source = await OpenFileWithRetryAsync(_settings.ChatLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            await using var target = File.Create(destination);

            await source.CopyToAsync(target);

            AddHashEntry(hash, fileName);

            _logger.LogInformation("Backup created: {File}", destination);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup failed");
        }
    }

    private static async Task<FileStream> OpenFileWithRetryAsync(string path, FileMode mode, FileAccess access, FileShare share)
    {
        const int maxRetries = 5;
        const int delayMs = 500;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                return File.Open(path, mode, access, share);
            }
            catch (IOException) when (i < maxRetries - 1)
            {
                await Task.Delay(delayMs);
            }
        }

        return File.Open(path, mode, access, share);
    }

    private static async Task<string> ComputeMd5Async(string filePath)
    {
        await using var stream = await OpenFileWithRetryAsync(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        using var md5 = MD5.Create();

        byte[] hash = await md5.ComputeHashAsync(stream);

        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private Dictionary<string, string> LoadHashIndex()
    {
        if (File.Exists(_hashIndexPath))
        {
            try
            {
                string json = File.ReadAllText(_hashIndexPath);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read hash index, starting fresh");
            }
        }

        return new Dictionary<string, string>();
    }

    private void SaveHashIndex(Dictionary<string, string> index)
    {
        try
        {
            string json = JsonSerializer.Serialize(index, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_hashIndexPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save hash index");
        }
    }

    private bool IsDuplicate(string hash)
    {
        lock (_lock)
        {
            var index = LoadHashIndex();
            bool exists = index.ContainsKey(hash);
            return exists;
        }
    }

    private void AddHashEntry(string hash, string fileName)
    {
        lock (_lock)
        {
            var index = LoadHashIndex();
            index[hash] = fileName;
            SaveHashIndex(index);
        }
    }
}