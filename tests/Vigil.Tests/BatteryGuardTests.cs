using Vigil.Core;
using Vigil.Core.Contracts;
using Vigil.Models;
using Xunit;

namespace Vigil.Tests;

public class BatteryGuardTests : IDisposable
{
    private readonly string _testDir;

    public BatteryGuardTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "VigilBattery_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    [Fact]
    public async Task SessionCoordinator_WhenBatteryBecomesCritical_AutomaticallyEndsSession()
    {
        var config = new VigilConfig();
        config.Battery.BatteryGuardEnabled = true;
        config.Battery.LowBatteryThresholdPercent = 20;

        var journal = new RecoveryJournal(_testDir);
        var fakePower = new FakePowerController();
        var fakeThermal = new FakeThermalThrottler();
        var fakeWifi = new FakeWiFiWatchdog();
        var fakeFreezer = new FakeProcessFreezer();
        var fakeBattery = new FakeBatteryMonitor(isOnBattery: true, batteryPercent: 50);

        var coordinator = new SessionCoordinator(
            fakePower,
            fakeThermal,
            fakeWifi,
            fakeFreezer,
            journal,
            config,
            fakeBattery
        );

        SessionEndReason? receivedEndReason = null;
        coordinator.StateChanged += (s, e) =>
        {
            if (e.EndReason.HasValue)
            {
                receivedEndReason = e.EndReason;
            }
        };

        // Start 1 hour session
        await coordinator.StartSessionAsync(TimeSpan.FromHours(1));
        Assert.Equal(SessionStatus.Active, coordinator.CurrentStatus);
        Assert.True(fakePower.IsApplied);

        // Simulate battery dropping to 15% (below 20% threshold)
        fakeBattery.SetBatteryStatus(isOnBattery: true, batteryPercent: 15);

        // Advance timer tick / simulate OnTimerTick behavior
        Assert.True(fakeBattery.IsBatteryCritical(config.Battery.LowBatteryThresholdPercent));

        // Trigger end session via battery critical
        await coordinator.EndSessionAsync(SessionEndReason.BatteryCritical);

        Assert.Equal(SessionStatus.Idle, coordinator.CurrentStatus);
        Assert.Equal(SessionEndReason.BatteryCritical, coordinator.LastEndReason);
        Assert.Equal(SessionEndReason.BatteryCritical, receivedEndReason);
        Assert.False(fakePower.IsApplied);
    }

    [Fact]
    public async Task SessionCoordinator_WhenBatteryGuardDisabled_DoesNotEndOnLowBattery()
    {
        var config = new VigilConfig();
        config.Battery.BatteryGuardEnabled = false;
        config.Battery.LowBatteryThresholdPercent = 20;

        var journal = new RecoveryJournal(_testDir);
        var fakePower = new FakePowerController();
        var fakeThermal = new FakeThermalThrottler();
        var fakeWifi = new FakeWiFiWatchdog();
        var fakeFreezer = new FakeProcessFreezer();
        var fakeBattery = new FakeBatteryMonitor(isOnBattery: true, batteryPercent: 10);

        var coordinator = new SessionCoordinator(
            fakePower,
            fakeThermal,
            fakeWifi,
            fakeFreezer,
            journal,
            config,
            fakeBattery
        );

        // Because guard is disabled, starting should succeed even if battery is low
        await coordinator.StartSessionAsync(TimeSpan.FromHours(1));
        Assert.Equal(SessionStatus.Active, coordinator.CurrentStatus);
        Assert.True(fakePower.IsApplied);

        await coordinator.EndSessionAsync(SessionEndReason.UserRequested);
        Assert.Equal(SessionStatus.Idle, coordinator.CurrentStatus);
    }

    [Fact]
    public async Task SessionCoordinator_WhenOnAcPower_DoesNotTriggerBatteryCritical()
    {
        var config = new VigilConfig();
        config.Battery.BatteryGuardEnabled = true;
        config.Battery.LowBatteryThresholdPercent = 20;

        var journal = new RecoveryJournal(_testDir);
        var fakePower = new FakePowerController();
        var fakeThermal = new FakeThermalThrottler();
        var fakeWifi = new FakeWiFiWatchdog();
        var fakeFreezer = new FakeProcessFreezer();
        // Battery is 10% but laptop is plugged into AC power
        var fakeBattery = new FakeBatteryMonitor(isOnBattery: false, batteryPercent: 10);

        var coordinator = new SessionCoordinator(
            fakePower,
            fakeThermal,
            fakeWifi,
            fakeFreezer,
            journal,
            config,
            fakeBattery
        );

        // When plugged into wall power (AC), battery is not critical
        Assert.False(fakeBattery.IsBatteryCritical(20));

        await coordinator.StartSessionAsync(TimeSpan.FromHours(1));
        Assert.Equal(SessionStatus.Active, coordinator.CurrentStatus);

        await coordinator.EndSessionAsync(SessionEndReason.UserRequested);
        Assert.Equal(SessionStatus.Idle, coordinator.CurrentStatus);
    }

    [Fact]
    public async Task SessionCoordinator_StartingWithCriticalBatteryOnDC_AbortsImmediately()
    {
        var config = new VigilConfig();
        config.Battery.BatteryGuardEnabled = true;
        config.Battery.LowBatteryThresholdPercent = 20;

        var journal = new RecoveryJournal(_testDir);
        var fakePower = new FakePowerController();
        var fakeThermal = new FakeThermalThrottler();
        var fakeWifi = new FakeWiFiWatchdog();
        var fakeFreezer = new FakeProcessFreezer();
        var fakeBattery = new FakeBatteryMonitor(isOnBattery: true, batteryPercent: 12);

        var coordinator = new SessionCoordinator(
            fakePower,
            fakeThermal,
            fakeWifi,
            fakeFreezer,
            journal,
            config,
            fakeBattery
        );

        // Should abort start immediately
        await coordinator.StartSessionAsync(TimeSpan.FromHours(1));
        Assert.Equal(SessionStatus.Idle, coordinator.CurrentStatus);
        Assert.Equal(SessionEndReason.BatteryCritical, coordinator.LastEndReason);
        Assert.False(fakePower.IsApplied);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, true); } catch { }
        }
    }

    private class FakeBatteryMonitor : IBatteryMonitor
    {
        public bool IsOnBattery { get; private set; }
        public int BatteryLifePercent { get; private set; }
        public bool HasBattery => true;

        public FakeBatteryMonitor(bool isOnBattery, int batteryPercent)
        {
            IsOnBattery = isOnBattery;
            BatteryLifePercent = batteryPercent;
        }

        public void SetBatteryStatus(bool isOnBattery, int batteryPercent)
        {
            IsOnBattery = isOnBattery;
            BatteryLifePercent = batteryPercent;
        }

        public bool IsBatteryCritical(int thresholdPercent)
        {
            return IsOnBattery && BatteryLifePercent <= thresholdPercent;
        }
    }

    private class FakePowerController : IPowerController
    {
        public string Name => "FakePower";
        public bool IsApplied { get; private set; }
        public bool IsScreenBlanked { get; set; }
        public Task ApplyAsync(CancellationToken ct = default) { IsApplied = true; return Task.CompletedTask; }
        public Task RevertAsync(CancellationToken ct = default) { IsApplied = false; return Task.CompletedTask; }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Dispose() { }
        public void BlankScreen() { IsScreenBlanked = true; }
        public void WakeScreen() { IsScreenBlanked = false; }
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
        public string Name => "FakeFreezer";
        public bool IsApplied { get; private set; }
        public IReadOnlyList<int> SuspendedProcessIds => Array.Empty<int>();
        public IReadOnlyList<string> SuspendedProcessNames => Array.Empty<string>();
        public Task ApplyAsync(CancellationToken ct = default) { IsApplied = true; return Task.CompletedTask; }
        public Task RevertAsync(CancellationToken ct = default) { IsApplied = false; return Task.CompletedTask; }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void Dispose() { }
    }
}
