using System.Net.Http.Headers;
using MediaConverter.Core.Services;
using MediaConverter.Core.Services.Binaries;

namespace MediaConverter.Core.Tests;

/// <summary>
/// Real-network tests for <see cref="BinariesProvisioningService"/>. Each test returns
/// without asserting when the network or the version sources are unavailable, so the suite
/// never fails offline. The test that downloads real binaries is additionally gated behind
/// the <c>MEDIACONVERTER_REAL_DOWNLOAD=1</c> environment variable to avoid downloading
/// roughly 120 MB on every test run.
/// </summary>
public class BinariesProvisioningIntegrationTests
{
    [Fact]
    public async Task ProvisionAsync_RealNetwork_ProvisionsBinaries()
    {
        if (Environment.GetEnvironmentVariable("MEDIACONVERTER_REAL_DOWNLOAD") != "1" ||
            !await HasReachableVersionSourcesAsync())
        {
            return;
        }

        var workDir = TestHelpers.CreateTempDirectory();
        try
        {
            var service = new BinariesProvisioningService(workDir);
            var result = await service.ProvisionAsync();

            Assert.True(result.Succeeded, $"Expected success, got {result.ErrorCode}.");
            Assert.True(File.Exists(Path.Combine(workDir, "ffmpeg.exe")));
            Assert.True(File.Exists(Path.Combine(workDir, "yt-dlp.exe")));
            Assert.True(new FileInfo(Path.Combine(workDir, "ffmpeg.exe")).Length > 100_000);
            Assert.True(new FileInfo(Path.Combine(workDir, "yt-dlp.exe")).Length > 1_000_000);
            Assert.True(File.Exists(Path.Combine(workDir, "ffmpeg.version")));
            Assert.True(File.Exists(Path.Combine(workDir, "yt-dlp.version")));
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }

    [Fact]
    public async Task UpdateAsync_RealNetwork_WhenLocalVersionIsCurrent_DoesNotDownload()
    {
        if (!await HasReachableVersionSourcesAsync())
        {
            return;
        }

        var workDir = TestHelpers.CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(workDir, "ffmpeg.exe"), "dummy ffmpeg");
            File.WriteAllText(Path.Combine(workDir, "ffmpeg.version"), "9999.99.99");
            File.WriteAllText(Path.Combine(workDir, "yt-dlp.exe"), "dummy yt-dlp");
            File.WriteAllText(Path.Combine(workDir, "yt-dlp.version"), "9999.99.99");

            var service = new BinariesProvisioningService(workDir);
            var result = await service.UpdateAsync();

            Assert.True(result.Succeeded, $"Expected success, got {result.ErrorCode}.");
            Assert.Equal("dummy ffmpeg", File.ReadAllText(Path.Combine(workDir, "ffmpeg.exe")));
            Assert.Equal("dummy yt-dlp", File.ReadAllText(Path.Combine(workDir, "yt-dlp.exe")));
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }

    /// <summary>
    /// Verifies both version endpoints (gyan.dev and the GitHub API) are reachable before
    /// running a test that relies on them. Returns <see langword="false"/> when the network
    /// or a source is unavailable, which makes the caller skip.
    /// </summary>
    private static async Task<bool> HasReachableVersionSourcesAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MediaConverterTests", "1.0"));

            using var ytDlp = await client.GetAsync(BinarySources.GetYtDlpVersionUrl());
            using var ffmpeg = await client.GetAsync(BinarySources.GetFfmpegVersionUrl());

            return ytDlp.IsSuccessStatusCode && ffmpeg.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}