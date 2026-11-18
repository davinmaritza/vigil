using System.Diagnostics;
using Vigil.Core.Contracts;
using Vigil.Models;
using Vigil.Native;

namespace Vigil.Core;

public interface IProcessFreezer : ISystemModifier
{
    IReadOnlyList<int> SuspendedProcessIds { get; }
    IReadOnlyList<string> SuspendedProcessNames { get; }
}

public class ProcessFreezer : IProcessFreezer
{
    private readonly VigilConfig _config;
    private readonly List<int> _suspendedPids = new();
    private readonly List<string> _suspendedNames = new();
    private bool _isApplied;
    private bool _disposed;

    // Safety blacklist: NEVER suspend essential Windows processes or development / terminal harnesses
    private static readonly HashSet<string> SystemBlacklist = new(StringComparer.OrdinalIgnoreCase)
    {
        "explorer", "csrss", "lsass", "services", "svchost", "smss", "winlogon",
        "dwm", "taskhostw", "sihost", "ctfmon", "System", "Idle", "Registry",
        "vigil", "dotnet", "conhost", "cmd", "powershell", "pwsh", "devenv", "taskmgr"
    };

    public static bool IsBlacklisted(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return false;
        var clean = processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) 
            ? processName[..^4] 
            : processName;
        return SystemBlacklist.Contains(clean);
    }

    public string Name => "Non-Essential Background Process Freezer";
    public bool IsApplied => _isApplied;
    public IReadOnlyList<int> SuspendedProcessIds => _suspendedPids;
    public IReadOnlyList<string> SuspendedProcessNames => _suspendedNames;

    public ProcessFreezer(VigilConfig config)
    {
        _config = config;
    }

    public Task ApplyAsync(CancellationToken ct = default)
    {
        if (_isApplied || !_config.Freezer.Enabled) return Task.CompletedTask;

        var targetNames = new HashSet<string>(_config.Freezer.ProcessNames, StringComparer.OrdinalIgnoreCase);

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.Id == Environment.ProcessId) continue;

                var name = process.ProcessName;
                if (IsBlacklisted(name)) continue;

                if (targetNames.Contains(name))
                {
                    var hProc = Win32.OpenProcess(Win32.ProcessSuspendResume, false, process.Id);
                    if (hProc != IntPtr.Zero)
                    {
                        var status = Win32.NtSuspendProcess(hProc);
                        Win32.CloseHandle(hProc);

                        if (status == 0) // STATUS_SUCCESS
                        {
                            _suspendedPids.Add(process.Id);
                            if (!_suspendedNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                            {
                                _suspendedNames.Add(name);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Access denied or process exited
            }
            finally
            {
                process.Dispose();
            }
        }

        _isApplied = true;
        return Task.CompletedTask;
    }

    public Task RevertAsync(CancellationToken ct = default)
    {
        if (!_isApplied) return Task.CompletedTask;

        ResumePids(_suspendedPids);
        _suspendedPids.Clear();
        _suspendedNames.Clear();
        _isApplied = false;

        return Task.CompletedTask;
    }

    public static void ResumePids(IEnumerable<int> pids)
    {
        foreach (var pid in pids)
        {
            try
            {
                var hProc = Win32.OpenProcess(Win32.ProcessSuspendResume, false, pid);
                if (hProc != IntPtr.Zero)
                {
                    Win32.NtResumeProcess(hProc);
                    Win32.CloseHandle(hProc);
                }
            }
            catch
            {
                // Process may have already exited
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            RevertAsync(CancellationToken.None);
            _disposed = true;
            GC.SuppressFinalize(this);
        }
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            RevertAsync(CancellationToken.None).GetAwaiter().GetResult();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
