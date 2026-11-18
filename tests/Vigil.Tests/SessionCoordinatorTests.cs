using Vigil.Core;
using Vigil.Core.Contracts;
using Vigil.Models;
using Xunit;

namespace Vigil.Tests;

public class SessionCoordinatorTests : IDisposable
{
    private readonly string _testDir;

    public SessionCoordinatorTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "VigilCoord_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    [Fact]
    public async Task SessionCoordinator_FullLifecycle_TransitionsCorrectly()
    {
        var config = new VigilConfig();
        var journal = new RecoveryJournal(_testDir);
        var fakePower = new FakePowerController();
        var fakeThermal = new FakeThermalThrottler();
        var fakeWifi = new FakeWiFiWatchdog();
        var fakeFreezer = new FakeProcessFreezer();

        var coordinator = new SessionCoordinator(fakePower, fakeThermal, fakeWifi, fakeFreezer, journal, config);

        Assert.Equal(SessionStatus.Idle, coordinator.CurrentStatus);

        // 1. Start Session (30 minutes)
        await coordinator.StartSessionAsync(TimeSpan.FromMinutes(30));

        Assert.Equal(SessionStatus.Active, coordinator.CurrentStatus);
        Assert.False(coordinator.IsInfiniteSession);
        Assert.True(coordinator.RemainingTime > TimeSpan.FromMinutes(29));
        Assert.True(fakePower.IsApplied);
        Assert.True(fakeThermal.IsApplied);

        // Verify Journal exists with Active status
        var activeJournal = await journal.ReadActiveJournalAsync();
        Assert.NotNull(activeJournal);
        Assert.Equal("Active", activeJournal.Status);
        Assert.NotNull(activeJournal.OriginalSettings);
        Assert.Equal(1, activeJournal.OriginalSettings.AcLidAction);

        // 2. Extend Session (+15 minutes)
        await coordinator.ExtendSessionAsync(TimeSpan.FromMinutes(15));
        Assert.True(coordinator.RemainingTime > TimeSpan.FromMinutes(40));

        // 3. End Session
        await coordinator.EndSessionAsync(SessionEndReason.UserRequested);
        Assert.Equal(SessionStatus.Idle, coordinator.CurrentStatus);
        Assert.False(fakePower.IsApplied);
        Assert.False(fakeThermal.IsApplied);

        // Verify Journal cleared
        var clearedJournal = await journal.ReadActiveJournalAsync();
        Assert.Null(clearedJournal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, true); } catch { }
        }
    }

    private class FakePowerController : IPowerController
    {
        public string Name => "FakePower";
        public bool IsApplied { get; private set; }
        public Task ApplyAsync(CancellationToken ct = default) { IsApplied = true; return Task.CompletedTask; }
        public Task RevertAsync(CancellationToken ct = default) { IsApplied = false; return Task.CompletedTask; }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Dispose() { }
        public void BlankScreen() { }
        public void WakeScreen() { }
        public Task<SystemPowerSnapshot> CaptureSnapshotAsync(CancellationToken ct = default)
        {
            return Task.FromResult(new SystemPowerSnapshot { AcLidAction = 1, DcLidAction = 1, AcMaxProcessorState = 100 });
        }
    }

    private class FakeThermalThrottler : IThermalThrottler
    {
        public string Name => "FakeThermal";
        public bool IsApplied { get; private set; }
        public float CurrentCpuTempCelsius => 45.0f;
        public int CurrentCpuLoadPercent => 10;
        public int CurrentMaxProcessorState => 92;
        public bool IsEmergencyThrottled => false;
        public event EventHandler<float>? TemperatureUpdated { add { } remove { } }
        public Task ApplyAsync(CancellationToken ct = default) { IsApplied = true; return Task.CompletedTask; }
        public Task RevertAsync(CancellationToken ct = default) { IsApplied = false; return Task.CompletedTask; }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Dispose() { }
    }

    private class FakeWiFiWatchdog : IWiFiWatchdog
    {
        public string Name => "FakeWiFi";
        public bool IsApplied { get; private set; }
        public string CurrentConnectedSsid => "TestSSID";
        public bool IsInternetConnected => true;
        public int PingLatencyMs => 15;
        public event EventHandler<WiFiStatusChangedEventArgs>? StatusChanged { add { } remove { } }
        public Task ProbeAndReconnectAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task ApplyAsync(CancellationToken ct = default) { IsApplied = true; return Task.CompletedTask; }
        public Task RevertAsync(CancellationToken ct = default) { IsApplied = false; return Task.CompletedTask; }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Dispose() { }
    }

    private class FakeProcessFreezer : IProcessFreezer
    {
        private static readonly List<int> EmptyPids = new();
        private static readonly List<string> EmptyNames = new();

        public string Name => "FakeFreezer";
        public bool IsApplied { get; private set; }
        public IReadOnlyList<int> SuspendedProcessIds => EmptyPids;
        public IReadOnlyList<string> SuspendedProcessNames => EmptyNames;
        public Task ApplyAsync(CancellationToken ct = default) { IsApplied = true; return Task.CompletedTask; }
        public Task RevertAsync(CancellationToken ct = default) { IsApplied = false; return Task.CompletedTask; }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Dispose() { }
    }
}
