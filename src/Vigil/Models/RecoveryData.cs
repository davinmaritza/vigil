using System.Text.Json.Serialization;

namespace Vigil.Models;

public class RecoveryData
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("startTimeUtc")]
    public DateTime StartTimeUtc { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("expectedEndTimeUtc")]
    public DateTime? ExpectedEndTimeUtc { get; set; }

    [JsonPropertyName("originalSettings")]
    public SystemPowerSnapshot OriginalSettings { get; set; } = new();

    [JsonPropertyName("appliedSettings")]
    public SystemPowerSnapshot AppliedSettings { get; set; } = new();

    [JsonPropertyName("frozenProcessIds")]
    public List<int> FrozenProcessIds { get; set; } = new();

    [JsonPropertyName("mutedEndpoints")]
    public List<string> MutedEndpoints { get; set; } = new();

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Active"; // Active, CleanedUp, Crashed
}

public class SystemPowerSnapshot
{
    [JsonPropertyName("acLidAction")]
    public int? AcLidAction { get; set; } // 0 = Do nothing, 1 = Sleep, 2 = Hibernate, 3 = Shutdown

    [JsonPropertyName("dcLidAction")]
    public int? DcLidAction { get; set; }

    [JsonPropertyName("acMaxProcessorState")]
    public int? AcMaxProcessorState { get; set; } // Percentage, e.g. 92

    [JsonPropertyName("dcMaxProcessorState")]
    public int? DcMaxProcessorState { get; set; }
}
