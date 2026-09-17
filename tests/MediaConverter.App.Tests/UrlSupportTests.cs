using MediaConverter.App.Services;

namespace MediaConverter.App.Tests;

/// <summary>
/// Tests the preventive URL validation used to keep the Download button disabled for empty or
/// malformed links.
/// </summary>
public class UrlSupportTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=abc123")]
    [InlineData("http://example.com/video")]
    [InlineData("https://example.com")]
    [InlineData("  https://example.com/path  ")]
    public void IsWellFormedVideoUrl_ValidHttpUrls_ReturnsTrue(string url)
    {
        Assert.True(UrlSupport.IsWellFormedVideoUrl(url));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    [InlineData("www.example.com")]
    [InlineData("ftp://example.com/file")]
    [InlineData("https://")]
    public void IsWellFormedVideoUrl_EmptyOrMalformed_ReturnsFalse(string? url)
    {
        Assert.False(UrlSupport.IsWellFormedVideoUrl(url));
    }
}
