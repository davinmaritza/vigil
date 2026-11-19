using System.Diagnostics;
using Vigil.Core.Contracts;
using Vigil.Models;

namespace Vigil.Core;

public class ProcessWatcher : IProcessWatcher
{
    private readonly VigilConfig _config;
    private readonly CancellationTokenSource _cts = new();
    private Task? _watcherTask;
    private string? _currentTargetProcess;
    private bool _disposed;

    public IReadOnlyList<string> MonitoredProcesses => _config.AutoPilot.WatchProcesses;
    public bool IsTargetProcessRunning => !string.IsNullOrEmpty(_currentTargetProcess);
    public string? CurrentTargetProcess => _currentTargetProcess;

    public event EventHandler<string>? TargetProcessStarted;
    public event EventHandler<string>? TargetProcessExited;

    public ProcessWatcher(VigilConfig config)
    {
        _config = config;
    }

    public void Start()
    {
        if (!_config.AutoPilot.Enabled) return;
        _watcherTask = Task.Run(() => WatcherLoopAsync(_cts.Token), _cts.Token);
    }

    private async Task WatcherLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var watchedSet = new HashSet<string>(
                    _config.AutoPilot.WatchProcesses.Select(p => p.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? p[..^4] : p),
                    StringComparer.OrdinalIgnoreCase
                );

                var running = Process.GetProcesses();
                string? detectedProcess = null;

                foreach (var p in running)
                {
                    try
                    {
                        if (watchedSet.Contains(p.ProcessName))
                        {
                            detectedProcess = p.ProcessName + ".exe";
                            break;
                        }
                    }
                    catch
                    {
                        // Ignore process inspection errors
                    }
                    finally
                    {
                        p.Dispose();
                    }
                }

                if (detectedProcess != null && _currentTargetProcess == null)
                {
                    // Newly detected process started
                    _currentTargetProcess = detectedProcess;
                    TargetProcessStarted?.Invoke(this, detectedProcess);
                }
                else if (detectedProcess == null && _currentTargetProcess != null)
                {
                    // Monitored process exited
                    var exited = _currentTargetProcess;
                    _currentTargetProcess = null;
                    TargetProcessExited?.Invoke(this, exited);
                }
            }
            catch
            {
                // Watcher should never crash
            }

            try
            {
                await Task.Delay(3000, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _cts.Cancel();
            if (_watcherTask != null)
            {
                try { await _watcherTask; } catch { /* Ignore task cancel */ }
            }
            _cts.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
