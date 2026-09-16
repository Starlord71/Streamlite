using MediaConverter.Core.Models;
using MediaConverter.Core.Services;

namespace MediaConverter.Core.Tests;

/// <summary>
/// Tests the mapping performed by <see cref="YtDlpErrorClassifier"/> from captured yt-dlp
/// standard error text to machine-readable error codes.
/// </summary>
public class YtDlpErrorClassifierTests
{
    [Fact]
    public void Classify_UnsupportedUrl_ReturnsInvalidUrl()
    {
        Assert.Equal(
            ErrorCode.InvalidUrl,
            YtDlpErrorClassifier.Classify("ERROR: Unsupported URL: https://example.com/not-a-video"));
    }

    [Fact]
    public void Classify_UnableToDownload_ReturnsNetworkError()
    {
        Assert.Equal(
            ErrorCode.NetworkError,
            YtDlpErrorClassifier.Classify("ERROR: unable to download webpage: <urlopen error [Errno 11001]>"));
    }

    [Fact]
    public void Classify_HttpError_ReturnsNetworkError()
    {
        Assert.Equal(
            ErrorCode.NetworkError,
            YtDlpErrorClassifier.Classify("ERROR: HTTP Error 403: Forbidden"));
    }

    [Fact]
    public void Classify_TimedOut_ReturnsNetworkError()
    {
        Assert.Equal(
            ErrorCode.NetworkError,
            YtDlpErrorClassifier.Classify("ERROR: The read operation timed out"));
    }

    [Fact]
    public void Classify_UnrelatedError_ReturnsDownloadFailed()
    {
        Assert.Equal(
            ErrorCode.DownloadFailed,
            YtDlpErrorClassifier.Classify("ERROR: Video unavailable"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Classify_EmptyOutput_ReturnsDownloadFailed(string? output)
    {
        Assert.Equal(ErrorCode.DownloadFailed, YtDlpErrorClassifier.Classify(output));
    }
}
