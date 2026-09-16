using MediaConverter.Core.Services.Binaries;

namespace MediaConverter.Core.Tests;

/// <summary>
/// Tests the resolution of the folder where external binaries are stored.
/// </summary>
public class BinaryPathsTests
{
    [Fact]
    public void GetExecutableDirectory_ReturnsNonEmptyExistingDirectory()
    {
        var directory = BinaryPaths.GetExecutableDirectory();

        Assert.False(string.IsNullOrWhiteSpace(directory));
        Assert.True(Directory.Exists(directory));
    }

    [Fact]
    public void GetExecutableDirectory_MatchesProcessPathDirectory_WhenAvailable()
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(processPath))
        {
            return;
        }

        var expected = Path.GetDirectoryName(processPath);

        Assert.Equal(expected, BinaryPaths.GetExecutableDirectory());
    }
}