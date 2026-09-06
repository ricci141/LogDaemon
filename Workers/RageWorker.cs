using Workers.Services;

namespace Workers;

public sealed class RageWorker : BackgroundService
{
    private readonly IRageProcessMonitor _monitor;
    private readonly IChatLogBackupService _backupService;
    private readonly IConfiguration _config;

    private bool _wasRunning;

    public RageWorker(
        IRageProcessMonitor monitor,
        IChatLogBackupService backupService,
        IConfiguration config)
    {
        _monitor = monitor;
        _backupService = backupService;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // можно в опции потом вынести
        int interval = _config.GetValue<int>("RageSettings:CheckIntervalMs");

        while (!cancellationToken.IsCancellationRequested)
        {
            bool running = _monitor.IsRunning();

            if (_wasRunning && !running || !_wasRunning && running)
            {
                await _backupService.BackupAsync();
            }

            _wasRunning = running;

            await Task.Delay(interval, cancellationToken);
        }
    }
}