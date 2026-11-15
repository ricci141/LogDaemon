using System.Diagnostics;

namespace Worker.Services;

public interface IRageProcessMonitor
{
    bool IsRunning();
}

public sealed class RageProcessMonitor(string processName) : IRageProcessMonitor
{
    public bool IsRunning()
    {
        return Process.GetProcessesByName(processName).Length > 0;
    }
}