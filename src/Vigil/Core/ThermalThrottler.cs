using System.Management;
using Vigil.Core.Contracts;
using Vigil.Models;
using Vigil.Native;

namespace Vigil.Core;

public interface IThermalThrottler : ISystemModifier
{
    float CurrentCpuTempCelsius { get; }
    int CurrentCpuLoadPercent { get; }
    int CurrentMaxProcessorState { get; }
    bool IsEmergencyThrottled { get; }

    event EventHandler<float>? TemperatureUpdated;
}

public class ThermalThrottler : IThermalThrottler
{
    private readonly IPowerCfgRunner _powerCfg;
    private readonly VigilConfig _config;
    private CancellationTokenSource? _cts;
    private Task? _monitorTask;

    private int? _originalAcThrottleMax;
    private int? _originalDcThrottleMax;
    private float _currentCpuTemp = 48.0f;
    private int _currentCpuLoad = 15;
    private int _currentMaxProcessorState = 100;
    private bool _isEmergencyThrottled;
    private bool _isApplied;
    private bool _disposed;

    public string Name => "Thermal and Turbo-Boost Throttler";
    public bool IsApplied => _isApplied;
    public float CurrentCpuTempCelsius => _currentCpuTemp;
    public int CurrentCpuLoadPercent => _currentCpuLoad;
    public int CurrentMaxProcessorState => _currentMaxProcessorState;
    public bool IsEmergencyThrottled => _isEmergencyThrottled;

    public event EventHandler<float>? TemperatureUpdated;

    public ThermalThrottler(IPowerCfgRunner powerCfg, VigilConfig config)
    {
        _powerCfg = powerCfg;
        _config = config;
    }

    public async Task ApplyAsync(CancellationToken ct = default)
    {
        if (_isApplied) return;

        // 1. Record original Max Processor State
        _originalAcThrottleMax = await _powerCfg.GetMaxProcessorStateAsync(onBattery: false, ct);
        _originalDcThrottleMax = await _powerCfg.GetMaxProcessorStateAsync(onBattery: true, ct);

        // 2. Set to configured cap (default 92%) to disable aggressive Turbo Boost thermal spikes
        var targetCap = _config.Thermal.MaxProcessorStatePercent;
        await _powerCfg.SetMaxProcessorStateAsync(targetCap, onBattery: false, ct);
        await _powerCfg.SetMaxProcessorStateAsync(targetCap, onBattery: true, ct);
        _currentMaxProcessorState = targetCap;

        _isApplied = true;

        // 3. Start background thermal sensor polling
        _cts = new CancellationTokenSource();
        _monitorTask = Task.Run(() => MonitorLoopAsync(_cts.Token), _cts.Token);
    }

    public async Task RevertAsync(CancellationToken ct = default)
    {
        if (!_isApplied) return;

        if (_cts != null)
        {
            _cts.Cancel();
            if (_monitorTask != null)
            {
                try { await _monitorTask; } catch { /* Ignore task cancellation */ }
            }
            _cts.Dispose();
            _cts = null;
        }

        // Restore original Max Processor State
        var restoreAc = _originalAcThrottleMax ?? 100;
        var restoreDc = _originalDcThrottleMax ?? 100;

        await _powerCfg.SetMaxProcessorStateAsync(restoreAc, onBattery: false, ct);
        await _powerCfg.SetMaxProcessorStateAsync(restoreDc, onBattery: true, ct);
        _currentMaxProcessorState = restoreAc;

        _isEmergencyThrottled = false;
        _isApplied = false;
    }

    private async Task MonitorLoopAsync(CancellationToken ct)
    {
        var intervalMs = Math.Max(1, _config.Thermal.PollIntervalSeconds) * 1000;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var load = PollCpuLoad();
                if (load >= 0) _currentCpuLoad = load;

                var temp = PollCpuTemperature();
                if (temp > 0)
                {
                    _currentCpuTemp = temp;
                    TemperatureUpdated?.Invoke(this, temp);

                    // Emergency overheat check
                    if (temp >= _config.Thermal.EmergencyTempThresholdCelsius && !_isEmergencyThrottled)
                    {
                        _isEmergencyThrottled = true;
                        var emergencyCap = _config.Thermal.EmergencyThrottlePercent;
                        await _powerCfg.SetMaxProcessorStateAsync(emergencyCap, false, ct);
                        await _powerCfg.SetMaxProcessorStateAsync(emergencyCap, true, ct);
                        _currentMaxProcessorState = emergencyCap;
                    }
                    else if (_isEmergencyThrottled && temp < (_config.Thermal.EmergencyTempThresholdCelsius - 8))
                    {
                        // Cooled down: return to base cap
                        _isEmergencyThrottled = false;
                        var baseCap = _config.Thermal.MaxProcessorStatePercent;
                        await _powerCfg.SetMaxProcessorStateAsync(baseCap, false, ct);
                        await _powerCfg.SetMaxProcessorStateAsync(baseCap, true, ct);
                        _currentMaxProcessorState = baseCap;
                    }
                }
                else
                {
                    // Fallback telemetry tick
                    TemperatureUpdated?.Invoke(this, _currentCpuTemp);
                }
            }
            catch
            {
                // Thermal polling should never crash the session
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

    private static int PollCpuLoad()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT LoadPercentage FROM Win32_Processor"
            );
            foreach (var obj in searcher.Get())
            {
                if (obj["LoadPercentage"] != null)
                {
                    return Convert.ToInt32(obj["LoadPercentage"]);
                }
            }
        }
        catch
        {
            // Ignore WMI errors
        }
        return -1;
    }

    private static float PollCpuTemperature()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\WMI",
                "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature"
            );

            foreach (var queryObj in searcher.Get())
            {
                var kelvinTenths = Convert.ToDouble(queryObj["CurrentTemperature"]);
                // Formula: Celsius = (TenthsKelvin / 10) - 273.15
                var celsius = (float)((kelvinTenths / 10.0) - 273.15);
                if (celsius is > 10 and < 125)
                {
                    return MathF.Round(celsius, 1);
                }
            }
        }
        catch
        {
            // Fallback: check CIMV2 ThermalZoneInformation
            try
            {
                using var fallbackSearcher = new ManagementObjectSearcher(
                    @"root\CIMV2",
                    "SELECT Temperature FROM Win32_PerfFormattedData_Counters_ThermalZoneInformation"
                );
                foreach (var obj in fallbackSearcher.Get())
                {
                    var k = Convert.ToDouble(obj["Temperature"]);
                    var celsius = (float)(k - 273.15);
                    if (celsius is > 10 and < 125)
                    {
                        return MathF.Round(celsius, 1);
                    }
                }
            }
            catch
            {
                // Fallback unavailable
            }
        }

        return -1f;
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
