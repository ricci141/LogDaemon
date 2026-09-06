using System.Diagnostics;

namespace Workers.Services;

public interface IRageProcessMonitor
{
    bool IsRunning();
}

public sealed class RageProcessMonitor : IRageProcessMonitor
{
    private readonly string _processName;

    public RageProcessMonitor(string processName)
    {
        _processName = processName;
    }

    public bool IsRunning()
    {
        return Process.GetProcessesByName(_processName).Length > 0;
    }
}