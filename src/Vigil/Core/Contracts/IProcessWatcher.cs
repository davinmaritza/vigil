namespace Vigil.Core.Contracts;

public interface IProcessWatcher : IAsyncDisposable
{
    IReadOnlyList<string> MonitoredProcesses { get; }
    bool IsTargetProcessRunning { get; }
    string? CurrentTargetProcess { get; }

    void Start();

    event EventHandler<string>? TargetProcessStarted;
    event EventHandler<string>? TargetProcessExited;
}
