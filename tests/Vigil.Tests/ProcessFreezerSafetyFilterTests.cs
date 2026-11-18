using Vigil.Core;
using Xunit;

namespace Vigil.Tests;

public class ProcessFreezerSafetyFilterTests
{
    [Theory]
    [InlineData("explorer")]
    [InlineData("explorer.exe")]
    [InlineData("csrss")]
    [InlineData("csrss.exe")]
    [InlineData("lsass")]
    [InlineData("services")]
    [InlineData("svchost")]
    [InlineData("winlogon")]
    [InlineData("dwm")]
    [InlineData("vigil")]
    [InlineData("Vigil.exe")]
    [InlineData("dotnet")]
    [InlineData("cmd")]
    [InlineData("powershell")]
    [InlineData("pwsh")]
    [InlineData("taskmgr")]
    public void IsBlacklisted_CriticalProcesses_AlwaysReturnsTrue(string processName)
    {
        Assert.True(ProcessFreezer.IsBlacklisted(processName));
    }

    [Theory]
    [InlineData("chrome")]
    [InlineData("slack.exe")]
    [InlineData("discord")]
    [InlineData("spotify.exe")]
    [InlineData("steam")]
    public void IsBlacklisted_NonCriticalApps_ReturnsFalse(string processName)
    {
        Assert.False(ProcessFreezer.IsBlacklisted(processName));
    }
}
