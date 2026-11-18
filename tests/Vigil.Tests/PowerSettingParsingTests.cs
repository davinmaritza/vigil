using Xunit;

namespace Vigil.Tests;

public class PowerSettingParsingTests
{
    [Fact]
    public void ParseHexSetting_ValidAcOutput_ReturnsInteger()
    {
        const string mockPowercfgOutput = @"
Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (Balanced)
  GUID: 4f971e89-eebd-4455-a8de-9e59040e7347  (Buttons and lid)
    GUID: 5ca83367-6e45-459f-a27b-476b1d01c936  (Lid close action)
      Possible Setting Index: 000
      Possible Setting Friendly Name: Do nothing
      Possible Setting Index: 001
      Possible Setting Friendly Name: Sleep
    Current AC Power Setting Index: 0x00000001
    Current DC Power Setting Index: 0x00000001
";

        var acVal = ParseSample(mockPowercfgOutput, onBattery: false);
        var dcVal = ParseSample(mockPowercfgOutput, onBattery: true);

        Assert.Equal(1, acVal);
        Assert.Equal(1, dcVal);
    }

    private static int? ParseSample(string output, bool onBattery)
    {
        var targetKey = onBattery ? "Current DC Power Setting Index:" : "Current AC Power Setting Index:";
        using var reader = new StringReader(output);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.Contains(targetKey, StringComparison.OrdinalIgnoreCase))
            {
                var parts = line.Split(':');
                if (parts.Length > 1)
                {
                    var hexStr = parts[1].Trim();
                    if (hexStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        hexStr = hexStr[2..];
                    }

                    if (int.TryParse(hexStr, System.Globalization.NumberStyles.HexNumber, null, out var val))
                    {
                        return val;
                    }
                }
            }
        }
        return null;
    }
}
