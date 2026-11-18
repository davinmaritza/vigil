using System.Timers;
using Vigil.Core.Contracts;
using Vigil.Models;
using Timer = System.Timers.Timer;

namespace Vigil.Core;

public class SessionCoordinator : ISessionCoordinator
{
    private readonly IPowerController _powerController;
    private readonly IThermalThrottler _thermalThrottler;
    private readonly IWiFiWatchdog _wifiWatchdog;
    private readonly IProcessFreezer _processFreezer;
    private readonly IRecoveryJournal _recoveryJournal;
    private readonly VigilConfig _config;

    private readonly Timer _countdownTimer;
    private SessionStatus _status = SessionStatus.Idle;
    private TimeSpan _remainingTime = TimeSpan.Zero;
    private bool _isInfinite;
    private string? _activeTargetProcess;
    private bool _disposed;

    public SessionStatus CurrentStatus => _status;
    public TimeSpan RemainingTime => _remainingTime;
    public bool IsInfiniteSession => _isInfinite;
    public string? ActiveTargetProcess => _activeTargetProcess;

    public event EventHandler<SessionStateChangedEventArgs>? StateChanged;

    public SessionCoordinator(
        IPowerController powerController,
        IThermalThrottler thermalThrottler,
        IWiFiWatchdog wifiWatchdog,
        IProcessFreezer processFreezer,
        IRecoveryJournal recoveryJournal,
        VigilConfig config)
    {
        _powerController = powerController;
        _thermalThrottler = thermalThrottler;
        _wifiWatchdog = wifiWatchdog;
        _processFreezer = processFreezer;
        _recoveryJournal = recoveryJournal;
        _config = config;

        _countdownTimer = new Timer(1000);
        _countdownTimer.Elapsed += OnTimerTick;
    }

    public async Task StartSessionAsync(TimeSpan? duration, string? targetProcess = null, CancellationToken ct = default)
    {
        if (_status != SessionStatus.Idle)
        {
            // Already active; if new duration provided, extend it
            if (duration.HasValue)
            {
                await ExtendSessionAsync(duration.Value, ct);
            }
            return;
        }

        _activeTargetProcess = targetProcess;
        if (!duration.HasValue)
        {
            _isInfinite = true;
            _remainingTime = TimeSpan.Zero;
        }
        else
        {
            _isInfinite = false;
            _remainingTime = duration.Value;
        }

        var expectedEnd = _isInfinite ? (DateTime?)null : DateTime.UtcNow.Add(_remainingTime);

        // 1. Capture snapshot and write initial Recovery Journal (WAL)
        var originalSnapshot = await _powerController.CaptureSnapshotAsync(ct);
        var recoveryData = new RecoveryData
        {
            StartTimeUtc = DateTime.UtcNow,
            ExpectedEndTimeUtc = expectedEnd,
            OriginalSettings = originalSnapshot,
            AppliedSettings = new SystemPowerSnapshot
            {
                AcLidAction = _config.General.ClamshellMode ? 0 : originalSnapshot.AcLidAction,
                DcLidAction = _config.General.ClamshellMode ? 0 : originalSnapshot.DcLidAction,
                AcMaxProcessorState = _config.Thermal.MaxProcessorStatePercent,
                DcMaxProcessorState = _config.Thermal.MaxProcessorStatePercent
            },
            Status = "Active"
        };
        await _recoveryJournal.RecordPendingChangeAsync(recoveryData, ct);

        // 2. Schedule Task Scheduler Backstop
        if (expectedEnd.HasValue)
        {
            await _recoveryJournal.ScheduleBackstopWatchdogAsync(expectedEnd.Value.AddMinutes(1), ct);
        }

        // 3. Apply Modifiers
        await _powerController.ApplyAsync(ct);
        await _thermalThrottler.ApplyAsync(ct);
        await _processFreezer.ApplyAsync(ct);
        await _wifiWatchdog.ApplyAsync(ct);

        // Update recovery data with frozen PIDs
        recoveryData.FrozenProcessIds.AddRange(_processFreezer.SuspendedProcessIds);
        await _recoveryJournal.RecordPendingChangeAsync(recoveryData, ct);

        _status = SessionStatus.Active;
        _countdownTimer.Start();

        NotifyStateChanged();
    }

    public async Task ExtendSessionAsync(TimeSpan additionalTime, CancellationToken ct = default)
    {
        if (_status == SessionStatus.Idle) return;

        if (_isInfinite)
        {
            _isInfinite = false;
            _remainingTime = additionalTime;
        }
        else
        {
            _remainingTime = _remainingTime.Add(additionalTime);
        }

        var newEnd = DateTime.UtcNow.Add(_remainingTime);
        var activeData = await _recoveryJournal.ReadActiveJournalAsync(ct) ?? new RecoveryData();
        activeData.ExpectedEndTimeUtc = newEnd;
        await _recoveryJournal.RecordPendingChangeAsync(activeData, ct);
        await _recoveryJournal.ScheduleBackstopWatchdogAsync(newEnd.AddMinutes(1), ct);

        NotifyStateChanged();
    }

    public async Task EndSessionAsync(SessionEndReason reason, CancellationToken ct = default)
    {
        if (_status == SessionStatus.Idle) return;

        _countdownTimer.Stop();

        // 1. Revert Modifiers
        await _wifiWatchdog.RevertAsync(ct);
        await _processFreezer.RevertAsync(ct);
        await _thermalThrottler.RevertAsync(ct);
        await _powerController.RevertAsync(ct);

        // 2. Clean Up Safety Backstop & Journal
        await _recoveryJournal.CancelBackstopWatchdogAsync(ct);
        await _recoveryJournal.ClearJournalAsync(ct);

        _status = SessionStatus.Idle;
        _remainingTime = TimeSpan.Zero;
        _isInfinite = false;
        _activeTargetProcess = null;

        NotifyStateChanged();
    }

    private void OnTimerTick(object? sender, ElapsedEventArgs e)
    {
        if (_isInfinite) return;

        if (_remainingTime > TimeSpan.FromSeconds(1))
        {
            _remainingTime = _remainingTime.Subtract(TimeSpan.FromSeconds(1));
            NotifyStateChanged();
        }
        else
        {
            _remainingTime = TimeSpan.Zero;
            _countdownTimer.Stop();
            _ = EndSessionAsync(SessionEndReason.TimerExpired);
        }
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke(this, new SessionStateChangedEventArgs(_status, _remainingTime, _activeTargetProcess));
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _countdownTimer.Stop();
            _countdownTimer.Dispose();
            await EndSessionAsync(SessionEndReason.AppExiting, CancellationToken.None);
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
