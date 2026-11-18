using Vigil.Core.Contracts;
using Vigil.Models;
using Vigil.Native;

namespace Vigil.Core;

public interface IPowerController : ISystemModifier
{
    Task<SystemPowerSnapshot> CaptureSnapshotAsync(CancellationToken ct = default);
    void BlankScreen();
    void WakeScreen();
}

public class PowerController : IPowerController
{
    private readonly IPowerCfgRunner _powerCfg;
    private readonly VigilConfig _config;
    private int? _originalAcLidAction;
    private int? _originalDcLidAction;
    private bool _isApplied;
    private bool _disposed;

    public string Name => "Power and Clamshell Sleep Controller";
    public bool IsApplied => _isApplied;

    public PowerController(IPowerCfgRunner powerCfg, VigilConfig config)
    {
        _powerCfg = powerCfg;
        _config = config;
    }

    public async Task<SystemPowerSnapshot> CaptureSnapshotAsync(CancellationToken ct = default)
    {
        var acLid = await _powerCfg.GetLidActionAsync(onBattery: false, ct);
        var dcLid = await _powerCfg.GetLidActionAsync(onBattery: true, ct);
        var acCpu = await _powerCfg.GetMaxProcessorStateAsync(onBattery: false, ct);
        var dcCpu = await _powerCfg.GetMaxProcessorStateAsync(onBattery: true, ct);

        return new SystemPowerSnapshot
        {
            AcLidAction = acLid,
            DcLidAction = dcLid,
            AcMaxProcessorState = acCpu,
            DcMaxProcessorState = dcCpu
        };
    }

    public async Task ApplyAsync(CancellationToken ct = default)
    {
        if (_isApplied) return;

        // 1. Prevent system sleep using Windows ExecutionState
        Win32.SetThreadExecutionState(
            Win32.ExecutionState.EsContinuous |
            Win32.ExecutionState.EsSystemRequired |
            Win32.ExecutionState.EsAwayModeRequired
        );

        // 2. Clamshell Mode: configure lid action to "Do Nothing" (0)
        if (_config.General.ClamshellMode)
        {
            _originalAcLidAction = await _powerCfg.GetLidActionAsync(onBattery: false, ct);
            _originalDcLidAction = await _powerCfg.GetLidActionAsync(onBattery: true, ct);

            await _powerCfg.SetLidActionAsync(0, onBattery: false, ct);
            await _powerCfg.SetLidActionAsync(0, onBattery: true, ct);
        }

        // 3. Turn display off if configured
        if (_config.General.TurnDisplayOffOnSession)
        {
            BlankScreen();
        }

        _isApplied = true;
    }

    public async Task RevertAsync(CancellationToken ct = default)
    {
        if (!_isApplied) return;

        // 1. Reset ThreadExecutionState
        Win32.SetThreadExecutionState(Win32.ExecutionState.EsContinuous);

        // 2. Restore Lid Actions
        if (_originalAcLidAction.HasValue)
        {
            await _powerCfg.SetLidActionAsync(_originalAcLidAction.Value, onBattery: false, ct);
        }
        if (_originalDcLidAction.HasValue)
        {
            await _powerCfg.SetLidActionAsync(_originalDcLidAction.Value, onBattery: true, ct);
        }

        // 3. Wake screen and synthesize user input signal
        WakeScreen();

        _isApplied = false;
    }

    public void BlankScreen()
    {
        Win32.SendMessage(
            Win32.HwndBroadcast,
            Win32.WmSysCommand,
            (IntPtr)Win32.ScMonitorPower,
            (IntPtr)Win32.MonitorOff
        );
    }

    public void WakeScreen()
    {
        try
        {
            Win32.SendMessage(
                Win32.HwndBroadcast,
                Win32.WmSysCommand,
                (IntPtr)Win32.ScMonitorPower,
                (IntPtr)Win32.MonitorOn
            );
            // Universal Windows display wakeup via 1px mouse nudge
            Win32.mouse_event(Win32.MouseEventfMove, 0, 1, 0, 0);
            Win32.mouse_event(Win32.MouseEventfMove, 0, unchecked((uint)-1), 0, 0);
        }
        catch { }
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
