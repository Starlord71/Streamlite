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

    [Fact]
    public void GetApplicationDataDirectory_ReturnsMediaConverterFolderUnderLocalApplicationData()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrEmpty(localApplicationData))
        {
            // Documented fallback: when the local application data folder is unavailable, the
            // binaries and settings fall back to the executable directory.
            return;
        }

        var expected = Path.Combine(localApplicationData, "MediaConverter");

        Assert.Equal(expected, BinaryPaths.GetApplicationDataDirectory());
    }

    [Fact]
    public void GetApplicationDataDirectory_DoesNotWriteNextToTheExecutable()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrEmpty(localApplicationData))
        {
            return;
        }

        // The released application is a single file: user data must never land next to the exe.
        Assert.NotEqual(BinaryPaths.GetExecutableDirectory(), BinaryPaths.GetApplicationDataDirectory());
    }
}