using Worker.Models;
using Worker.Services;

namespace Worker;

public sealed class RageWorker(IRageProcessMonitor monitor, IChatLogBackupService backupService, AppSettings settings) 
    : BackgroundService
{
    private bool _wasRunning;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            bool running = monitor.IsRunning();

            if (_wasRunning && !running || !_wasRunning && running)
            {
                await backupService.BackupAsync();
            }

            _wasRunning = running;

            await Task.Delay(settings.CheckIntervalMs, cancellationToken);
        }
    }
}