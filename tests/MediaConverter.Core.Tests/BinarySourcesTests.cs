using MediaConverter.Core.Services.Binaries;

namespace MediaConverter.Core.Tests;

/// <summary>
/// Locks the official download and version endpoints used for provisioning so a change
/// in the chosen sources is a conscious, reviewed decision.
/// </summary>
public class BinarySourcesTests
{
    [Fact]
    public void GetFfmpegDownloadUrl_ReturnsOfficialEssentialsZip()
    {
        Assert.Equal(
            "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip",
            BinarySources.GetFfmpegDownloadUrl().ToString());
    }

    [Fact]
    public void GetFfmpegVersionUrl_ReturnsOfficialReleaseVersionMarker()
    {
        Assert.Equal(
            "https://www.gyan.dev/ffmpeg/builds/release-version",
            BinarySources.GetFfmpegVersionUrl().ToString());
    }

    [Fact]
    public void GetYtDlpDownloadUrl_ReturnsOfficialGitHubLatestExecutable()
    {
        Assert.Equal(
            "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe",
            BinarySources.GetYtDlpDownloadUrl().ToString());
    }

    [Fact]
    public void GetYtDlpVersionUrl_ReturnsGitHubLatestReleaseApi()
    {
        Assert.Equal(
            "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest",
            BinarySources.GetYtDlpVersionUrl().ToString());
    }
}