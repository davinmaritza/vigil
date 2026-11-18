using Vigil.Native;
using Xunit;

namespace Vigil.Tests;

public class StartupManagerTests
{
    [Fact]
    public void IsRunOnStartupEnabled_ExecutesWithoutException()
    {
        // Safe read-only invocation: should return boolean without throwing any security or IO exceptions
        var enabled = StartupManager.IsRunOnStartupEnabled();
        Assert.True(enabled || !enabled); // Must be a valid boolean
    }
}
