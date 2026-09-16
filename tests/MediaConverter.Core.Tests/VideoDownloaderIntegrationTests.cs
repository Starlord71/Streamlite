using MediaConverter.Core.Models;
using MediaConverter.Core.Services;

namespace MediaConverter.Core.Tests;

/// <summary>
/// End-to-end download tests that require the real yt-dlp and ffmpeg binaries. Each test
/// early-returns (without asserting) when the binaries or a downloadable test URL are missing,
/// so the suite never fails on machines without them and never depends on a live video site
/// by default.
/// </summary>
public class VideoDownloaderIntegrationTests
{
    private const string TestUrlEnvironmentVariable = "MEDIACONVERTER_YTDLP_TEST_URL";

    private static readonly string? YtDlpPath = TestHelpers.LocateYtDlp();
    private static readonly string? FfmpegPath = TestHelpers.LocateFfmpeg();

    [Fact]
    public async Task DownloadAsync_RealYtDlp_DownloadsSingleAudioFile()
    {
        var ytDlpPath = YtDlpPath;
        var ffmpegPath = FfmpegPath;
        var url = Environment.GetEnvironmentVariable(TestUrlEnvironmentVariable);
        if (ytDlpPath is null || ffmpegPath is null || string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        var workDir = TestHelpers.CreateTempDirectory();
        try
        {
            var reports = new List<ProgressInfo>();
            var service = new VideoDownloaderService(ytDlpPath, ffmpegPath);

            var result = await service.DownloadAsync(
                url,
                workDir,
                DownloadFormat.MP3,
                new Progress<ProgressInfo>(reports.Add));

            Assert.True(result.Succeeded, $"Expected success, got {result.ErrorCode}.");

            var files = Directory.GetFiles(workDir);
            Assert.Single(files);
            Assert.Equal(".mp3", Path.GetExtension(files[0]), ignoreCase: true);
            Assert.True(new FileInfo(files[0]).Length > 0, "Downloaded file is empty.");
            Assert.Contains(reports, r => r.Stage == ProgressStage.Downloading);
            Assert.Equal(ProgressStage.Completed, reports[^1].Stage);
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }
}
