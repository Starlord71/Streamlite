using MediaConverter.Core.Services.Binaries;

namespace MediaConverter.Core.Tests;

/// <summary>
/// Tests extraction of the <c>tag_name</c> from a GitHub latest-release JSON document.
/// </summary>
public class GitHubReleaseParserTests
{
    [Fact]
    public void TryGetLatestVersion_ValidReleaseJson_ReturnsTagName()
    {
        const string json = """{"tag_name":"2025.09.26","name":"yt-dlp 2025.09.26","assets":[]}""";

        var success = GitHubReleaseParser.TryGetLatestVersion(json, out var version);

        Assert.True(success);
        Assert.Equal("2025.09.26", version);
    }

    [Fact]
    public void TryGetLatestVersion_WithoutTagName_ReturnsFalse()
    {
        const string json = """{"name":"yt-dlp"}""";

        Assert.False(GitHubReleaseParser.TryGetLatestVersion(json, out _));
    }

    [Fact]
    public void TryGetLatestVersion_InvalidJson_ReturnsFalse()
    {
        Assert.False(GitHubReleaseParser.TryGetLatestVersion("not json", out _));
    }

    [Fact]
    public void TryGetLatestVersion_NonObjectRoot_ReturnsFalse()
    {
        Assert.False(GitHubReleaseParser.TryGetLatestVersion("[1,2,3]", out _));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryGetLatestVersion_NullOrWhitespace_ReturnsFalse(string? json)
    {
        Assert.False(GitHubReleaseParser.TryGetLatestVersion(json, out _));
    }
}