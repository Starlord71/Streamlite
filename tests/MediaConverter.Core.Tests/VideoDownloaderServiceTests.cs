using MediaConverter.Core.Models;
using MediaConverter.Core.Services;

namespace MediaConverter.Core.Tests;

/// <summary>
/// Tests the pure validation and error-code logic of <see cref="VideoDownloaderService"/>.
/// These tests never spawn yt-dlp, so they run on any machine.
/// </summary>
public class VideoDownloaderServiceTests
{
    private const string ValidUrl = "https://example.com/watch?v=abc";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DownloadAsync_NullOrWhitespaceUrl_ReturnsInvalidInput(string? url)
    {
        var service = new VideoDownloaderService();
        var result = await service.DownloadAsync(url!, "output", DownloadFormat.MP4);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.InvalidInput, result.ErrorCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DownloadAsync_NullOrWhitespaceOutputDirectory_ReturnsInvalidInput(string? outputDirectory)
    {
        var service = new VideoDownloaderService();
        var result = await service.DownloadAsync(ValidUrl, outputDirectory!, DownloadFormat.MP4);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.InvalidInput, result.ErrorCode);
    }

    [Fact]
    public async Task DownloadAsync_AlreadyCancelled_ReturnsCancelled()
    {
        var service = new VideoDownloaderService();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await service.DownloadAsync(ValidUrl, "output", DownloadFormat.MP4, cancellationToken: cts.Token);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.Cancelled, result.ErrorCode);
    }

    [Fact]
    public async Task DownloadAsync_UndefinedFormat_ReturnsUnsupportedFormat()
    {
        var service = new VideoDownloaderService();
        var result = await service.DownloadAsync(ValidUrl, "output", (DownloadFormat)999);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.UnsupportedFormat, result.ErrorCode);
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://example.com/video")]
    [InlineData("https://")]
    public async Task DownloadAsync_MalformedOrUnsupportedUrl_ReturnsInvalidUrl(string url)
    {
        var service = new VideoDownloaderService();
        var result = await service.DownloadAsync(url, "output", DownloadFormat.MP4);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.InvalidUrl, result.ErrorCode);
    }

    [Fact]
    public async Task DownloadAsync_MissingYtDlp_ReturnsBinaryNotFound()
    {
        var workDir = TestHelpers.CreateTempDirectory();
        try
        {
            var missingYtDlp = Path.Combine(workDir, "does-not-exist", "yt-dlp.exe");
            var service = new VideoDownloaderService(missingYtDlp);

            var result = await service.DownloadAsync(ValidUrl, workDir, DownloadFormat.MP4);

            Assert.False(result.Succeeded);
            Assert.Equal(ErrorCode.BinaryNotFound, result.ErrorCode);
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }

    [Fact]
    public async Task DownloadAsync_MissingFfmpeg_ReturnsBinaryNotFound()
    {
        var workDir = TestHelpers.CreateTempDirectory();
        try
        {
            var ytDlp = Path.Combine(workDir, "yt-dlp.exe");
            File.WriteAllText(ytDlp, "dummy");
            var missingFfmpeg = Path.Combine(workDir, "does-not-exist", "ffmpeg.exe");
            var service = new VideoDownloaderService(ytDlp, missingFfmpeg);

            var result = await service.DownloadAsync(ValidUrl, workDir, DownloadFormat.MP4);

            Assert.False(result.Succeeded);
            Assert.Equal(ErrorCode.BinaryNotFound, result.ErrorCode);
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }

    [Fact]
    public async Task DownloadAsync_OutputDirectoryBlockedByFile_ReturnsOutputPathInvalid()
    {
        var workDir = TestHelpers.CreateTempDirectory();
        try
        {
            var blocker = Path.Combine(workDir, "blocker.txt");
            File.WriteAllText(blocker, "blocking");
            var service = new VideoDownloaderService();

            var result = await service.DownloadAsync(ValidUrl, blocker, DownloadFormat.MP4);

            Assert.False(result.Succeeded);
            Assert.Equal(ErrorCode.OutputPathInvalid, result.ErrorCode);
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }
}
