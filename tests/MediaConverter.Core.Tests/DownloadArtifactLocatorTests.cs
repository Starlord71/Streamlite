using MediaConverter.Core.Models;
using MediaConverter.Core.Services;

namespace MediaConverter.Core.Tests;

/// <summary>
/// Tests <see cref="DownloadArtifactLocator"/>, the pure logic that maps a download format to an
/// extension and picks the single final file yt-dlp produced inside a working directory.
/// </summary>
public class DownloadArtifactLocatorTests
{
    [Theory]
    [InlineData(DownloadFormat.MP4, ".mp4")]
    [InlineData(DownloadFormat.MP3, ".mp3")]
    [InlineData(DownloadFormat.M4A, ".m4a")]
    public void GetExpectedExtension_ReturnsExtension(DownloadFormat format, string expected)
    {
        Assert.Equal(expected, DownloadArtifactLocator.GetExpectedExtension(format));
    }

    [Fact]
    public void GetExpectedExtension_UndefinedFormat_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DownloadArtifactLocator.GetExpectedExtension((DownloadFormat)999));
    }

    [Fact]
    public void FindFinalFile_MultipleFiles_IgnoresIntermediatesAndPartials()
    {
        var directory = TestHelpers.CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, "audio.webm"), "intermediate");
            File.WriteAllText(Path.Combine(directory, "audio.mp3.part"), "partial");
            var final = Path.Combine(directory, "audio.mp3");
            File.WriteAllText(final, "final");

            Assert.Equal(final, DownloadArtifactLocator.FindFinalFile(directory, DownloadFormat.MP3));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void FindFinalFile_NoMatch_ReturnsNull()
    {
        var directory = TestHelpers.CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(directory, "audio.webm"), "intermediate");

            Assert.Null(DownloadArtifactLocator.FindFinalFile(directory, DownloadFormat.MP3));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void FindFinalFile_MultipleMatches_ReturnsMostRecentlyWritten()
    {
        var directory = TestHelpers.CreateTempDirectory();
        try
        {
            var older = Path.Combine(directory, "older.mp3");
            File.WriteAllText(older, "older");
            var newer = Path.Combine(directory, "newer.mp3");
            File.WriteAllText(newer, "newer");

            File.SetLastWriteTimeUtc(older, DateTime.UtcNow.AddMinutes(-5));
            File.SetLastWriteTimeUtc(newer, DateTime.UtcNow);

            Assert.Equal(newer, DownloadArtifactLocator.FindFinalFile(directory, DownloadFormat.MP3));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
