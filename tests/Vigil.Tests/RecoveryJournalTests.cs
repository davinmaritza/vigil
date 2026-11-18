using Vigil.Core;
using Vigil.Models;
using Xunit;

namespace Vigil.Tests;

public class RecoveryJournalTests : IDisposable
{
    private readonly string _testDir;

    public RecoveryJournalTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "VigilTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    [Fact]
    public async Task WriteAndReadJournal_RoundtripMatches()
    {
        var journal = new RecoveryJournal(_testDir);

        var data = new RecoveryData
        {
            SessionId = "test-session-123",
            StartTimeUtc = DateTime.UtcNow,
            ExpectedEndTimeUtc = DateTime.UtcNow.AddHours(2),
            FrozenProcessIds = new List<int> { 1234, 5678 },
            OriginalSettings = new SystemPowerSnapshot
            {
                AcLidAction = 1,
                DcLidAction = 1,
                AcMaxProcessorState = 100
            }
        };

        await journal.RecordPendingChangeAsync(data);

        var readBack = await journal.ReadActiveJournalAsync();
        Assert.NotNull(readBack);
        Assert.Equal("test-session-123", readBack.SessionId);
        Assert.Equal(2, readBack.FrozenProcessIds.Count);
        Assert.Equal(1, readBack.OriginalSettings.AcLidAction);
        Assert.Equal(100, readBack.OriginalSettings.AcMaxProcessorState);

        await journal.ClearJournalAsync();
        var afterClear = await journal.ReadActiveJournalAsync();
        Assert.Null(afterClear);
    }

    [Fact]
    public async Task ReadActiveJournal_CorruptedOrTruncatedJson_ReturnsNullWithoutThrowing()
    {
        var journal = new RecoveryJournal(_testDir);
        var journalFile = Path.Combine(_testDir, "recovery.json");

        // Write corrupt JSON
        await File.WriteAllTextAsync(journalFile, "{ \"sessionId\": \"test-123\", \"version\": 1, unclosed_corrupted_content...");

        var result = await journal.ReadActiveJournalAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task ReadActiveJournal_NonExistentFile_ReturnsNull()
    {
        var emptyDir = Path.Combine(Path.GetTempPath(), "VigilEmpty_" + Guid.NewGuid().ToString("N"));
        try
        {
            var journal = new RecoveryJournal(emptyDir);
            var result = await journal.ReadActiveJournalAsync();
            Assert.Null(result);
        }
        finally
        {
            if (Directory.Exists(emptyDir)) Directory.Delete(emptyDir, true);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, true); } catch { }
        }
    }
}
