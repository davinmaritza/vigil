using Vigil.Core;
using Xunit;

namespace Vigil.Tests;

public class WiFiWatchdogTests
{
    [Fact]
    public void ParseSsid_StandardNetshOutput_ExtractsSsidSuccessfully()
    {
        const string mockOutput = @"
There is 1 interface on the system:

    Name                   : Wi-Fi
    Description            : Realtek 8822CE Wireless LAN 802.11ac PCI-E NIC
    GUID                   : 4e99f578-cae4-4cb6-a05e-88c9df1765c8
    Physical address       : 14:13:33:9a:12:ef
    Interface type         : Primary
    State                  : connected
    SSID                   : Davin-Maritza
    BSSID                  : 9c:c9:eb:32:86:7d
    Network type           : Infrastructure
    Radio type             : 802.11ac
    Authentication         : WPA2-Personal
    Cipher                 : CCMP
    Connection mode        : Auto Connect
    Channel                : 44
    Receive rate (Mbps)    : 866.7
    Transmit rate (Mbps)   : 866.7
    Signal                 : 99% 
";

        var ssid = WiFiWatchdog.ParseSsidFromNetshOutput(mockOutput);
        Assert.Equal("Davin-Maritza", ssid);
    }

    [Fact]
    public void ParseSsid_SsidWithColonsAndSpaces_ExtractsFullSsid()
    {
        const string mockOutput = @"
    State                  : connected
    SSID                   : My-Home:5GHz_Ultra-Fast
    BSSID                  : 12:34:56:78:90:ab
";
        var ssid = WiFiWatchdog.ParseSsidFromNetshOutput(mockOutput);
        Assert.Equal("My-Home:5GHz_Ultra-Fast", ssid);
    }

    [Fact]
    public void ParseSsid_DisconnectedOrEmpty_ReturnsNull()
    {
        const string mockOutput = @"
    Name                   : Wi-Fi
    State                  : disconnected
";
        var ssid = WiFiWatchdog.ParseSsidFromNetshOutput(mockOutput);
        Assert.Null(ssid);

        Assert.Null(WiFiWatchdog.ParseSsidFromNetshOutput(null));
        Assert.Null(WiFiWatchdog.ParseSsidFromNetshOutput("   "));
    }
}
