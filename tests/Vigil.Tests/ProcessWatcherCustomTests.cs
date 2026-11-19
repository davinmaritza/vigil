using Vigil.Core;
using Vigil.Models;
using Xunit;

namespace Vigil.Tests;

public class ProcessWatcherCustomTests
{
    [Fact]
    public void ProcessWatcher_ReflectsCustomProcessesAddedToConfig()
    {
        var config = new VigilConfig();
        config.AutoPilot.WatchProcesses.Clear();
        config.AutoPilot.WatchProcesses.Add("gta5.exe");
        config.AutoPilot.WatchProcesses.Add("unreal.exe");

        var watcher = new ProcessWatcher(config);

        Assert.Equal(2, watcher.MonitoredProcesses.Count);
        Assert.Contains("gta5.exe", watcher.MonitoredProcesses);
        Assert.Contains("unreal.exe", watcher.MonitoredProcesses);

        // Dynamically add another custom process
        config.AutoPilot.WatchProcesses.Add("steam.exe");
        Assert.Equal(3, watcher.MonitoredProcesses.Count);
        Assert.Contains("steam.exe", watcher.MonitoredProcesses);
    }

    [Fact]
    public void VigilConfig_SaveAndLoad_PreservesCustomProcessesAndBatteryGuard()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "vigil_cfg_" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var config = new VigilConfig();
            config.Battery.BatteryGuardEnabled = true;
            config.Battery.LowBatteryThresholdPercent = 25;
            config.AutoPilot.WatchProcesses.Add("gta5_enhanced.exe");

            var json = System.Text.Json.JsonSerializer.Serialize(config);
            File.WriteAllText(tempFile, json);

            var loadedJson = File.ReadAllText(tempFile);
            var loadedConfig = System.Text.Json.JsonSerializer.Deserialize<VigilConfig>(loadedJson);

            Assert.NotNull(loadedConfig);
            Assert.True(loadedConfig.Battery.BatteryGuardEnabled);
            Assert.Equal(25, loadedConfig.Battery.LowBatteryThresholdPercent);
            Assert.Contains("gta5_enhanced.exe", loadedConfig.AutoPilot.WatchProcesses);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
