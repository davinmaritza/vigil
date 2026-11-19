using Vigil.Core;
using Vigil.Core.Contracts;
using Vigil.Models;
using Vigil.Native;
using Xunit;

namespace Vigil.Tests;

public class GlobalEscapeWatcherTests
{
    private class MockPowerController : IPowerController
    {
        public string Name => "MockPower";
        public bool IsApplied { get; private set; }
        public bool IsScreenBlanked { get; private set; }
        public bool WakeScreenCalled { get; private set; }

        public Task ApplyAsync(CancellationToken ct = default)
        {
            IsApplied = true;
            return Task.CompletedTask;
        }

        public Task RevertAsync(CancellationToken ct = default)
        {
            IsApplied = false;
            WakeScreen();
            return Task.CompletedTask;
        }

        public void BlankScreen() => IsScreenBlanked = true;

        public void WakeScreen()
        {
            IsScreenBlanked = false;
            WakeScreenCalled = true;
        }

        public Task<SystemPowerSnapshot> CaptureSnapshotAsync(CancellationToken ct = default)
            => Task.FromResult(new SystemPowerSnapshot());

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Dispose() { }
    }

    private class MockSessionCoordinator : ISessionCoordinator
    {
        public SessionStatus CurrentStatus { get; set; } = SessionStatus.Idle;
        public TimeSpan RemainingTime => TimeSpan.Zero;
        public bool IsInfiniteSession => false;
        public string? ActiveTargetProcess => null;
        public SessionEndReason? LastEndReason { get; private set; }

        public event EventHandler<SessionStateChangedEventArgs>? StateChanged { add { } remove { } }

        public Task StartSessionAsync(TimeSpan? duration, string? targetProcess = null, CancellationToken ct = default)
        {
            CurrentStatus = SessionStatus.Active;
            return Task.CompletedTask;
        }

        public Task ExtendSessionAsync(TimeSpan additionalTime, CancellationToken ct = default) => Task.CompletedTask;

        public Task EndSessionAsync(SessionEndReason reason, CancellationToken ct = default)
        {
            CurrentStatus = SessionStatus.Idle;
            LastEndReason = reason;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact]
    public void BlankScreen_SetsIsScreenBlankedTrue()
    {
        var power = new MockPowerController();
        Assert.False(power.IsScreenBlanked);

        power.BlankScreen();
        Assert.True(power.IsScreenBlanked);

        power.WakeScreen();
        Assert.False(power.IsScreenBlanked);
    }

    [Fact]
    public async Task HandleEscapeCancel_WakesScreenAndEndsSession()
    {
        var coordinator = new MockSessionCoordinator();
        var power = new MockPowerController();
        var callbackFired = false;

        await coordinator.StartSessionAsync(TimeSpan.FromHours(1));
        power.BlankScreen();

        using var watcher = new GlobalEscapeWatcher(coordinator, power, () => callbackFired = true);
        watcher.IsDisplayOff = true;

        watcher.HandleEscapeCancel();

        // Allow task to finish
        await Task.Delay(50);

        Assert.True(power.WakeScreenCalled);
        Assert.False(power.IsScreenBlanked);
        Assert.False(watcher.IsDisplayOff);
        Assert.Equal(SessionStatus.Idle, coordinator.CurrentStatus);
        Assert.Equal(SessionEndReason.UserRequested, coordinator.LastEndReason);
        Assert.True(callbackFired);
    }
}
