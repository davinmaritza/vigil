using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using Vigil.Core.Contracts;
using Vigil.Models;

namespace Vigil.Core;

public class WiFiStatusChangedEventArgs : EventArgs
{
    public string CurrentSsid { get; }
    public bool IsConnected { get; }
    public int LatencyMs { get; }

    public WiFiStatusChangedEventArgs(string currentSsid, bool isConnected, int latencyMs)
    {
        CurrentSsid = currentSsid;
        IsConnected = isConnected;
        LatencyMs = latencyMs;
    }
}

public interface IWiFiWatchdog : ISystemModifier
{
    string CurrentConnectedSsid { get; }
    bool IsInternetConnected { get; }
    int PingLatencyMs { get; }

    Task ProbeAndReconnectAsync(CancellationToken ct = default);
    event EventHandler<WiFiStatusChangedEventArgs>? StatusChanged;
}

public class WiFiWatchdog : IWiFiWatchdog
{
    private readonly VigilConfig _config;
    private CancellationTokenSource? _cts;
    private Task? _watchdogTask;

    private string _currentSsid = "Detecting...";
    private bool _isInternetConnected = true;
    private int _pingLatencyMs = 14;
    private bool _isApplied;
    private bool _disposed;

    public string Name => "Wi-Fi Resiliency and Auto-Reconnect Watchdog";
    public bool IsApplied => _isApplied;
    public string CurrentConnectedSsid => _currentSsid;
    public bool IsInternetConnected => _isInternetConnected;
    public int PingLatencyMs => _pingLatencyMs;

    public event EventHandler<WiFiStatusChangedEventArgs>? StatusChanged;

    public WiFiWatchdog(VigilConfig config)
    {
        _config = config;
    }

    public Task ApplyAsync(CancellationToken ct = default)
    {
        if (_isApplied || !_config.WiFi.WatchdogEnabled) return Task.CompletedTask;

        _cts = new CancellationTokenSource();
        _isApplied = true;
        _watchdogTask = Task.Run(() => WatchdogLoopAsync(_cts.Token), _cts.Token);

        return Task.CompletedTask;
    }

    public async Task RevertAsync(CancellationToken ct = default)
    {
        if (!_isApplied) return;

        if (_cts != null)
        {
            _cts.Cancel();
            if (_watchdogTask != null)
            {
                try { await _watchdogTask; } catch { /* Ignore task cancellation */ }
            }
            _cts.Dispose();
            _cts = null;
        }

        _isApplied = false;
    }

    private async Task WatchdogLoopAsync(CancellationToken ct)
    {
        var intervalMs = Math.Max(5, _config.WiFi.WatchdogIntervalSeconds) * 1000;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProbeAndReconnectAsync(ct);
            }
            catch
            {
                // Watchdog errors should never abort the loop
            }

            try
            {
                await Task.Delay(intervalMs, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public async Task ProbeAndReconnectAsync(CancellationToken ct = default)
    {
        var ssid = await QueryCurrentSsidAsync(ct);
        if (!string.IsNullOrWhiteSpace(ssid))
        {
            _currentSsid = ssid;
        }

        var isHealthy = false;
        var latency = -1;

        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(_config.WiFi.PingTarget, 2500);
            if (reply.Status == IPStatus.Success)
            {
                isHealthy = true;
                latency = (int)reply.RoundtripTime;
            }
        }
        catch
        {
            isHealthy = false;
        }

        _isInternetConnected = isHealthy;
        _pingLatencyMs = latency > 0 ? latency : 999;

        StatusChanged?.Invoke(this, new WiFiStatusChangedEventArgs(_currentSsid, _isInternetConnected, _pingLatencyMs));

        if (!isHealthy)
        {
            await AttemptReconnectAsync(ct);
        }
    }

    private async Task AttemptReconnectAsync(CancellationToken ct)
    {
        // Cycle through priority list
        foreach (var profile in _config.WiFi.PriorityProfiles)
        {
            if (ct.IsCancellationRequested) break;

            var safeProfile = profile.Replace("\"", "").Trim();
            var res = await RunNetshAsync($"wlan connect name=\"{safeProfile}\"", ct);
            await Task.Delay(3000, ct);

            // Re-check ping
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(_config.WiFi.PingTarget, 2000);
                if (reply.Status == IPStatus.Success)
                {
                    _isInternetConnected = true;
                    _currentSsid = safeProfile;
                    _pingLatencyMs = (int)reply.RoundtripTime;
                    StatusChanged?.Invoke(this, new WiFiStatusChangedEventArgs(_currentSsid, true, _pingLatencyMs));
                    return;
                }
            }
            catch
            {
                // Continue to next profile
            }
        }
    }

    public static string? ParseSsidFromNetshOutput(string? output)
    {
        if (string.IsNullOrWhiteSpace(output)) return null;

        using var reader = new StringReader(output);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("SSID", StringComparison.OrdinalIgnoreCase) && !trimmed.StartsWith("BSSID", StringComparison.OrdinalIgnoreCase))
            {
                var colonIdx = trimmed.IndexOf(':');
                if (colonIdx >= 0 && colonIdx < trimmed.Length - 1)
                {
                    return trimmed[(colonIdx + 1)..].Trim();
                }
            }
        }
        return null;
    }

    private static async Task<string?> QueryCurrentSsidAsync(CancellationToken ct)
    {
        var output = await RunNetshAsync("wlan show interfaces", ct);
        return ParseSsidFromNetshOutput(output);
    }

    private static async Task<string?> RunNetshAsync(string args, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "netsh.exe",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc == null) return null;

            var output = await proc.StandardOutput.ReadToEndAsync(ct);
            await proc.WaitForExitAsync(ct);
            return output;
        }
        catch
        {
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await RevertAsync(CancellationToken.None);
            _disposed = true;
            GC.SuppressFinalize(this);
        }
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
