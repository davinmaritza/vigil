using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Vigil.Models;

namespace Vigil.Core;

public interface IRecoveryJournal
{
    string JournalFilePath { get; }
    Task<RecoveryData?> ReadActiveJournalAsync(CancellationToken ct = default);
    Task RecordPendingChangeAsync(RecoveryData data, CancellationToken ct = default);
    Task ClearJournalAsync(CancellationToken ct = default);
    Task ScheduleBackstopWatchdogAsync(DateTime triggerTimeUtc, CancellationToken ct = default);
    Task CancelBackstopWatchdogAsync(CancellationToken ct = default);
}

public class RecoveryJournal : IRecoveryJournal
{
    private readonly string _storageDir;
    private readonly string _journalPath;
    private readonly string _tempJournalPath;

    public string JournalFilePath => _journalPath;

    public RecoveryJournal(string? customDir = null)
    {
        _storageDir = customDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Vigil"
        );
        Directory.CreateDirectory(_storageDir);

        _journalPath = Path.Combine(_storageDir, "recovery.json");
        _tempJournalPath = Path.Combine(_storageDir, "recovery.json.tmp");
    }

    public async Task<RecoveryData?> ReadActiveJournalAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_journalPath)) return null;

        try
        {
            await using var stream = File.OpenRead(_journalPath);
            var data = await JsonSerializer.DeserializeAsync<RecoveryData>(stream, cancellationToken: ct);
            return data;
        }
        catch
        {
            return null;
        }
    }

    public async Task RecordPendingChangeAsync(RecoveryData data, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_tempJournalPath, json, ct);

            // Atomic file replace
            if (File.Exists(_journalPath))
            {
                File.Replace(_tempJournalPath, _journalPath, null);
            }
            else
            {
                File.Move(_tempJournalPath, _journalPath);
            }
        }
        catch
        {
            // Fallback direct write
            try
            {
                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_journalPath, json, ct);
            }
            catch
            {
                // Ignore transient I/O exceptions
            }
        }
    }

    public Task ClearJournalAsync(CancellationToken ct = default)
    {
        try
        {
            if (File.Exists(_journalPath)) File.Delete(_journalPath);
            if (File.Exists(_tempJournalPath)) File.Delete(_tempJournalPath);
        }
        catch
        {
            // Best effort
        }
        return Task.CompletedTask;
    }

    public Task ScheduleBackstopWatchdogAsync(DateTime triggerTimeUtc, CancellationToken ct = default)
    {
        try
        {
            var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath)) return Task.CompletedTask;

            var localTime = triggerTimeUtc.ToLocalTime();
            var timeStr = localTime.ToString("HH:mm:ss");
            var datePattern = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
            var dateStr = localTime.ToString(datePattern);

            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/create /tn \"VigilRecoveryWatchdog\" /tr \"\\\"{exePath}\\\" --recover\" /sc once /st {timeStr} /sd {dateStr} /f",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(3000);
        }
        catch
        {
            // Ignore if Task Scheduler is unavailable
        }
        return Task.CompletedTask;
    }

    public Task CancelBackstopWatchdogAsync(CancellationToken ct = default)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = "/delete /tn \"VigilRecoveryWatchdog\" /f",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(3000);
        }
        catch
        {
            // Ignore if task does not exist
        }
        return Task.CompletedTask;
    }
}
