using MediaConverter.Core.Services.Binaries;

namespace MediaConverter.Core.Tests;

/// <summary>
/// Tests the tolerant numeric version comparison used to decide whether an update is needed.
/// </summary>
public class VersionComparerTests
{
    [Theory]
    [InlineData("1.0", "1.0", 0)]
    [InlineData("1.0.1", "1.0", 1)]
    [InlineData("1.0", "1.0.1", -1)]
    [InlineData("2.0", "1.9.9", 1)]
    [InlineData("7.1", "7.10", -1)]
    [InlineData("9.0", "9.0.1", -1)]
    [InlineData("2025.09.26", "2025.09.26", 0)]
    [InlineData("2025.09.26", "2025.09.23", 1)]
    [InlineData("2025.09.26", "2025.10.01", -1)]
    [InlineData("2025-09-24-git-0f6f00", "2025-09-20-git-aa11bb", 1)]
    public void Compare_VariousVersions_ReturnsExpectedOrdering(string x, string y, int expected)
    {
        var actual = Math.Sign(VersionComparer.Compare(x, y));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Compare_EmptyOrNull_AreOrderedAsLowest()
    {
        Assert.Equal(0, VersionComparer.Compare(null, null));
        Assert.Equal(0, VersionComparer.Compare(string.Empty, "  "));
        Assert.True(VersionComparer.Compare(null, "1.0") < 0);
        Assert.True(VersionComparer.Compare(string.Empty, "1.0") < 0);
        Assert.True(VersionComparer.Compare("1.0", null) > 0);
    }

    [Fact]
    public void IsNewer_ReturnsTrueOnlyWhenFirstVersionIsGreater()
    {
        Assert.True(VersionComparer.IsNewer("2025.09.26", "2025.09.23"));
        Assert.False(VersionComparer.IsNewer("2025.09.23", "2025.09.26"));
        Assert.False(VersionComparer.IsNewer("2025.09.26", "2025.09.26"));
        Assert.True(VersionComparer.IsNewer("1.0", null));
        Assert.False(VersionComparer.IsNewer(null, null));
    }
}