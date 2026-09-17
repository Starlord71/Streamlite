using MediaConverter.App.Services;

namespace MediaConverter.App.Tests;

/// <summary>
/// Tests the validation that rejects a non-MP4 file before the video-to-audio extraction starts.
/// </summary>
public class VideoFileSupportTests
{
    [Theory]
    [InlineData("movie.mp4")]
    [InlineData("MOVIE.MP4")]
    [InlineData(@"C:\videos\movie.Mp4")]
    public void IsSupportedVideoSource_Mp4Files_ReturnsTrue(string path)
    {
        Assert.True(VideoFileSupport.IsSupportedVideoSource(path));
    }

    [Theory]
    [InlineData("movie.mkv")]
    [InlineData("movie.avi")]
    [InlineData("movie")]
    [InlineData("movie.mp3")]
    public void IsSupportedVideoSource_OtherFiles_ReturnsFalse(string path)
    {
        Assert.False(VideoFileSupport.IsSupportedVideoSource(path));
    }
}
