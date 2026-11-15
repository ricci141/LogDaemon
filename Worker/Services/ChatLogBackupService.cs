using System.Security.Cryptography;
using System.Text.Json;
using Worker.Models;

namespace Worker.Services;

public interface IChatLogBackupService
{
    Task BackupAsync();
}

public sealed class ChatLogBackupService(AppSettings settings, ILogger<ChatLogBackupService> logger) : IChatLogBackupService
{
    private readonly object _lock = new();

    public async Task BackupAsync()
    {
        try
        {
            if (!File.Exists(settings.ChatLogPath))
                return;

            string hash = await ComputeMd5Async(settings.ChatLogPath);

            if (IsDuplicate(hash))
            {
                logger.LogInformation("Duplicate file detected (hash: {Hash}), skipping backup", hash);
                return;
            }

            Directory.CreateDirectory(settings.BackupDirectory);

            string fileName = $"chatlog_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

            string destination = Path.Combine(settings.BackupDirectory, fileName);

            await using var source = await OpenFileWithRetryAsync(settings.ChatLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            await using var target = File.Create(destination);

            await source.CopyToAsync(target);

            AddHashEntry(hash, fileName);

            logger.LogInformation("Backup created: {File}", destination);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Backup failed");
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
        if (File.Exists(settings.HashIndexPath))
        {
            try
            {
                string json = File.ReadAllText(settings.HashIndexPath);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to read hash index, starting fresh");
            }
        }

        return new Dictionary<string, string>();
    }

    private void SaveHashIndex(Dictionary<string, string> index)
    {
        try
        {
            string json = JsonSerializer.Serialize(index, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(settings.HashIndexPath, json);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save hash index");
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