namespace Vigil.Core.Contracts;

public enum SessionStatus
{
    Idle,
    Active,
    ThrottledEmergency,
    Recovering
}

public enum SessionEndReason
{
    UserRequested,
    TimerExpired,
    TargetProcessExited,
    BatteryCritical,
    AppExiting
}

public class SessionStateChangedEventArgs : EventArgs
{
    public SessionStatus Status { get; }
    public TimeSpan RemainingTime { get; }
    public string? TargetProcess { get; }

    public SessionStateChangedEventArgs(SessionStatus status, TimeSpan remainingTime, string? targetProcess)
    {
        Status = status;
        RemainingTime = remainingTime;
        TargetProcess = targetProcess;
    }
}

public interface ISessionCoordinator : IAsyncDisposable
{
    SessionStatus CurrentStatus { get; }
    TimeSpan RemainingTime { get; }
    bool IsInfiniteSession { get; }
    string? ActiveTargetProcess { get; }

    Task StartSessionAsync(TimeSpan? duration, string? targetProcess = null, CancellationToken ct = default);
    Task ExtendSessionAsync(TimeSpan additionalTime, CancellationToken ct = default);
    Task EndSessionAsync(SessionEndReason reason, CancellationToken ct = default);

    event EventHandler<SessionStateChangedEventArgs> StateChanged;
}
