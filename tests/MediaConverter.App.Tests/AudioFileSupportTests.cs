using System.IO;
using MediaConverter.App.Services;
using MediaConverter.Core.Models;

namespace MediaConverter.App.Tests;

/// <summary>
/// Tests audio format detection and the non-overwriting output-path naming shared by the audio and
/// video-to-audio tabs.
/// </summary>
public class AudioFileSupportTests : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "mc-app-tests-" + Guid.NewGuid().ToString("N"));

    public AudioFileSupportTests() => Directory.CreateDirectory(_directory);

    [Theory]
    [InlineData("song.m4a", AudioFormat.M4A)]
    [InlineData("SONG.M4A", AudioFormat.M4A)]
    [InlineData("song.mp3", AudioFormat.MP3)]
    [InlineData("SONG.MP3", AudioFormat.MP3)]
    public void TryDetectFormat_SupportedExtensions_ReturnsTrue(string fileName, AudioFormat expected)
    {
        Assert.True(AudioFileSupport.TryDetectFormat(fileName, out var format));
        Assert.Equal(expected, format);
    }

    [Theory]
    [InlineData("song.wav")]
    [InlineData("song.txt")]
    [InlineData("song")]
    [InlineData("song.m4a.bak")]
    public void TryDetectFormat_UnsupportedExtensions_ReturnsFalse(string fileName)
    {
        Assert.False(AudioFileSupport.TryDetectFormat(fileName, out _));
    }

    [Fact]
    public void GetExtension_MapsEachFormat()
    {
        Assert.Equal(".m4a", AudioFileSupport.GetExtension(AudioFormat.M4A));
        Assert.Equal(".mp3", AudioFileSupport.GetExtension(AudioFormat.MP3));
    }

    [Fact]
    public void BuildTargetPath_WhenTargetDoesNotExist_UsesSourceBaseName()
    {
        var source = Path.Combine(_directory, "song.mp3");

        var target = AudioFileSupport.BuildTargetPath(source, AudioFormat.M4A);

        Assert.Equal(Path.Combine(_directory, "song.m4a"), target);
    }

    [Fact]
    public void BuildTargetPath_WhenTargetExists_AppendsNumericSuffix()
    {
        var source = Path.Combine(_directory, "song.mp3");
        File.WriteAllText(Path.Combine(_directory, "song.m4a"), "existing");
        File.WriteAllText(Path.Combine(_directory, "song (1).m4a"), "existing");

        var target = AudioFileSupport.BuildTargetPath(source, AudioFormat.M4A);

        Assert.Equal(Path.Combine(_directory, "song (2).m4a"), target);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
