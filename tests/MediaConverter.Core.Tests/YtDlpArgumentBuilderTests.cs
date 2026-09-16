using MediaConverter.Core.Models;
using MediaConverter.Core.Services;

namespace MediaConverter.Core.Tests;

/// <summary>
/// Tests the argument construction performed by <see cref="YtDlpArgumentBuilder"/> for each
/// supported download format. No process is spawned.
/// </summary>
public class YtDlpArgumentBuilderTests
{
    private const string Url = "https://example.com/watch?v=abc";
    private const string WorkDirectory = @"C:\work\download";
    private const string FfmpegLocation = @"C:\bin";

    [Fact]
    public void Build_Mp4_SelectsMp4AndMergesWithoutExtractingAudio()
    {
        var arguments = YtDlpArgumentBuilder.Build(Url, WorkDirectory, DownloadFormat.MP4, FfmpegLocation);

        var list = arguments.ToList();
        Assert.Equal(YtDlpArgumentBuilder.Mp4FormatSelector, list[list.IndexOf("--format") + 1]);
        Assert.Equal("mp4", list[list.IndexOf("--merge-output-format") + 1]);
        Assert.Equal("mp4", list[list.IndexOf("--remux-video") + 1]);
        Assert.DoesNotContain("--extract-audio", arguments);
    }

    [Fact]
    public void Build_Mp3_ExtractsAudioAsMp3()
    {
        var arguments = YtDlpArgumentBuilder.Build(Url, WorkDirectory, DownloadFormat.MP3, FfmpegLocation);

        var list = arguments.ToList();
        Assert.Equal(YtDlpArgumentBuilder.Mp3FormatSelector, list[list.IndexOf("--format") + 1]);
        Assert.Contains("--extract-audio", arguments);
        Assert.Equal("mp3", list[list.IndexOf("--audio-format") + 1]);
        Assert.DoesNotContain("--merge-output-format", arguments);
    }

    [Fact]
    public void Build_M4a_ExtractsAudioAsM4a()
    {
        var arguments = YtDlpArgumentBuilder.Build(Url, WorkDirectory, DownloadFormat.M4A, FfmpegLocation);

        var list = arguments.ToList();
        Assert.Equal(YtDlpArgumentBuilder.M4aFormatSelector, list[list.IndexOf("--format") + 1]);
        Assert.Contains("--extract-audio", arguments);
        Assert.Equal("m4a", list[list.IndexOf("--audio-format") + 1]);
    }

    [Fact]
    public void Build_AlwaysIncludesProgressAndFfmpegLocationAndPaths()
    {
        var arguments = YtDlpArgumentBuilder.Build(Url, WorkDirectory, DownloadFormat.MP3, FfmpegLocation);

        Assert.Contains("--newline", arguments);
        Assert.Contains("--progress", arguments);
        Assert.Contains("--no-playlist", arguments);

        var list = arguments.ToList();
        Assert.Equal(FfmpegLocation, list[list.IndexOf("--ffmpeg-location") + 1]);
        Assert.Contains($"home:{WorkDirectory}", arguments);
        Assert.Contains($"temp:{WorkDirectory}", arguments);
    }

    [Fact]
    public void Build_EndsWithSeparatorThenUrl()
    {
        var arguments = YtDlpArgumentBuilder.Build(Url, WorkDirectory, DownloadFormat.MP4, FfmpegLocation);

        Assert.Equal("--", arguments[^2]);
        Assert.Equal(Url, arguments[^1]);
    }

    [Fact]
    public void Build_UndefinedFormat_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            YtDlpArgumentBuilder.Build(Url, WorkDirectory, (DownloadFormat)999, FfmpegLocation));
    }
}
