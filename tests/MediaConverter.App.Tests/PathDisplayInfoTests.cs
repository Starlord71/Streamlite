using System.IO;
using MediaConverter.App.Services;

namespace MediaConverter.App.Tests;

/// <summary>
/// Tests how a full path is split into the emphasized name and the muted containing directory used
/// by the path display component.
/// </summary>
public class PathDisplayInfoTests
{
    [Fact]
    public void FromFile_WithDirectory_SplitsNameAndDirectory()
    {
        var path = Path.Combine(@"C:\videos", "clip.mp4");

        var info = PathDisplayInfo.FromFile(path);

        Assert.Equal("clip.mp4", info.Name);
        Assert.Equal(@"C:\videos", info.Directory);
    }

    [Fact]
    public void FromFile_BareFileName_HasEmptyDirectory()
    {
        var info = PathDisplayInfo.FromFile("clip.mp4");

        Assert.Equal("clip.mp4", info.Name);
        Assert.Equal(string.Empty, info.Directory);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void FromFile_Empty_ReturnsEmptyParts(string? path)
    {
        var info = PathDisplayInfo.FromFile(path);

        Assert.Equal(string.Empty, info.Name);
        Assert.Equal(string.Empty, info.Directory);
    }

    [Fact]
    public void FromFile_NameWithSolidus_KeepsSolidusAsPartOfTheName()
    {
        // yt-dlp replaces a slash in a title with the big solidus, which is not a path separator.
        var fileName = "The Ink Spots - Maybe - Subt\u00edtulos en espa\u00f1ol\u29f8\u29f8Lyrics.mp4";
        var path = Path.Combine(@"C:\videos", fileName);

        var info = PathDisplayInfo.FromFile(path);

        Assert.Equal(fileName, info.Name);
        Assert.Equal(@"C:\videos", info.Directory);
    }

    [Fact]
    public void FromDirectory_WithTrailingSeparator_UsesLastSegmentAsName()
    {
        var path = Path.Combine(@"C:\videos", "output") + Path.DirectorySeparatorChar;

        var info = PathDisplayInfo.FromDirectory(path);

        Assert.Equal("output", info.Name);
        Assert.Equal(@"C:\videos", info.Directory);
    }

    [Fact]
    public void FromDirectory_WithoutTrailingSeparator_UsesLastSegmentAsName()
    {
        var path = Path.Combine(@"C:\videos", "output");

        var info = PathDisplayInfo.FromDirectory(path);

        Assert.Equal("output", info.Name);
        Assert.Equal(@"C:\videos", info.Directory);
    }

    [Fact]
    public void FromDirectory_Root_KeepsRootAsName()
    {
        var path = @"C:\";

        var info = PathDisplayInfo.FromDirectory(path);

        Assert.Equal(path, info.Name);
        Assert.Equal(string.Empty, info.Directory);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void FromDirectory_Empty_ReturnsEmptyParts(string? path)
    {
        var info = PathDisplayInfo.FromDirectory(path);

        Assert.Equal(string.Empty, info.Name);
        Assert.Equal(string.Empty, info.Directory);
    }
}
