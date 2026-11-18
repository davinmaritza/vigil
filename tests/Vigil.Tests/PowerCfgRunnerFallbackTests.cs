using Vigil.Native;
using Xunit;

namespace Vigil.Tests;

public class PowerCfgRunnerFallbackTests
{
    [Theory]
    [InlineData("Current AC Power Setting Index: 0x00000000", false, 0)]
    [InlineData("Current AC Power Setting Index: 0x00000001", false, 1)]
    [InlineData("Current AC Power Setting Index: 0x0000005C", false, 92)]
    [InlineData("Current AC Power Setting Index: 0x00000064", false, 100)]
    [InlineData("Current DC Power Setting Index: 0x00000001", true, 1)]
    [InlineData("Current DC Power Setting Index: 0x0000005C", true, 92)]
    public void ParsePowerSettingHex_ValidOutputs_ReturnsExpectedIntegers(string output, bool onBattery, int expected)
    {
        var result = PowerCfgRunner.ParsePowerSettingHex(output, onBattery);
        Assert.NotNull(result);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public void ParsePowerSettingHex_WhenIndexIsMissingOrAttributesHidden_ReturnsNull()
    {
        // When OEM / Windows sets Attributes = 1, powercfg /query does NOT return Current AC/DC Power Setting Index lines
        const string hiddenOutput = @"
Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (Balanced)
  GUID: 4f971e89-eebd-4455-a8de-9e59040e7347  (Buttons and lid)
    GUID: 5ca83367-6e45-459f-a27b-476b1d01c936  (Lid close action)
";

        var acResult = PowerCfgRunner.ParsePowerSettingHex(hiddenOutput, onBattery: false);
        var dcResult = PowerCfgRunner.ParsePowerSettingHex(hiddenOutput, onBattery: true);

        Assert.Null(acResult);
        Assert.Null(dcResult);
    }

    [Fact]
    public void ParsePowerSettingHex_MalformedAndNull_ReturnsNull()
    {
        Assert.Null(PowerCfgRunner.ParsePowerSettingHex(null, false));
        Assert.Null(PowerCfgRunner.ParsePowerSettingHex("", false));
        Assert.Null(PowerCfgRunner.ParsePowerSettingHex("Current AC Power Setting Index: NotAHexNumber", false));
    }
}
