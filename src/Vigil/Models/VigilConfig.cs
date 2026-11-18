using System.Text.Json.Serialization;

namespace Vigil.Models;

public class VigilConfig
{
    [JsonPropertyName("general")]
    public GeneralSettings General { get; set; } = new();

    [JsonPropertyName("thermal")]
    public ThermalSettings Thermal { get; set; } = new();

    [JsonPropertyName("battery")]
    public BatterySettings Battery { get; set; } = new();

    [JsonPropertyName("wifi")]
    public WiFiSettings WiFi { get; set; } = new();

    [JsonPropertyName("freezer")]
    public FreezerSettings Freezer { get; set; } = new();

    [JsonPropertyName("autoPilot")]
    public AutoPilotSettings AutoPilot { get; set; } = new();
}

public class GeneralSettings
{
    [JsonPropertyName("startWithWindows")]
    public bool StartWithWindows { get; set; } = false;

    [JsonPropertyName("defaultDurationSeconds")]
    public int DefaultDurationSeconds { get; set; } = 7200; // 2 hours

    [JsonPropertyName("clamshellMode")]
    public bool ClamshellMode { get; set; } = true;

    [JsonPropertyName("turnDisplayOffOnSession")]
    public bool TurnDisplayOffOnSession { get; set; } = true;

    [JsonPropertyName("muteAudioOnLidClose")]
    public bool MuteAudioOnLidClose { get; set; } = true;
}

public class ThermalSettings
{
    [JsonPropertyName("maxProcessorStatePercent")]
    public int MaxProcessorStatePercent { get; set; } = 92; // Disables turbo boost thermal spike

    [JsonPropertyName("emergencyTempThresholdCelsius")]
    public int EmergencyTempThresholdCelsius { get; set; } = 80;

    [JsonPropertyName("emergencyThrottlePercent")]
    public int EmergencyThrottlePercent { get; set; } = 80;

    [JsonPropertyName("pollIntervalSeconds")]
    public int PollIntervalSeconds { get; set; } = 5;
}

public class BatterySettings
{
    [JsonPropertyName("ecoQoSFloorPercent")]
    public int EcoQoSFloorPercent { get; set; } = 40;

    [JsonPropertyName("safeHibernateFloorPercent")]
    public int SafeHibernateFloorPercent { get; set; } = 15;
}

public class WiFiSettings
{
    [JsonPropertyName("watchdogEnabled")]
    public bool WatchdogEnabled { get; set; } = true;

    [JsonPropertyName("watchdogIntervalSeconds")]
    public int WatchdogIntervalSeconds { get; set; } = 15;

    [JsonPropertyName("pingTarget")]
    public string PingTarget { get; set; } = "1.1.1.1";

    [JsonPropertyName("priorityProfiles")]
    public List<string> PriorityProfiles { get; set; } = new()
    {
        "Home_Studio_5G",
        "Phone_Hotspot"
    };
}

public class FreezerSettings
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("processNames")]
    public List<string> ProcessNames { get; set; } = new()
    {
        "slack",
        "discord",
        "whatsapp",
        "spotify"
    };
}

public class AutoPilotSettings
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("watchProcesses")]
    public List<string> WatchProcesses { get; set; } = new()
    {
        "blender.exe",
        "ollama.exe",
        "python.exe",
        "claude.exe",
        "antigravity.exe"
    };
}
