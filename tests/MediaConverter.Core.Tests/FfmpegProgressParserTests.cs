using MediaConverter.Core.Services;

namespace MediaConverter.Core.Tests;

/// <summary>
/// Tests the pure parsing logic of <see cref="FfmpegProgressParser"/> against ffmpeg's
/// <c>-progress pipe:1</c> key/value lines and the <c>Duration</c> line on standard error.
/// </summary>
public class FfmpegProgressParserTests
{
    [Fact]
    public void FeedLine_DurationLine_SetsTotalDuration()
    {
        var parser = new FfmpegProgressParser();
        parser.FeedLine("  Duration: 00:01:05.50, start: 0.000000, bitrate: 192 kb/s");

        Assert.NotNull(parser.TotalDuration);
        Assert.Equal(TimeSpan.FromSeconds(65.5), parser.TotalDuration);
    }

    [Fact]
    public void FeedLine_OutTimeUsLine_SetsOutTimeUs()
    {
        var parser = new FfmpegProgressParser();
        parser.FeedLine("out_time_us=1234567");

        Assert.Equal(1234567, parser.OutTimeUs);
    }

    [Fact]
    public void FeedLine_ProgressContinueThenEnd_SetsIsEnded()
    {
        var parser = new FfmpegProgressParser();
        parser.FeedLine("progress=continue");

        Assert.False(parser.IsEnded);

        parser.FeedLine("progress=end");

        Assert.True(parser.IsEnded);
    }

    [Fact]
    public void FeedLine_NoiseLines_AreIgnored()
    {
        var parser = new FfmpegProgressParser();
        parser.FeedLine("ffmpeg version 7.1 Copyright (c) 2000-2025 the FFmpeg developers");
        parser.FeedLine("frame=  100 fps= 50 q=28.0 size=     123kB");
        parser.FeedLine("  ");
        parser.FeedLine(null);

        Assert.Null(parser.TotalDuration);
        Assert.Null(parser.OutTimeUs);
        Assert.False(parser.IsEnded);
    }

    [Fact]
    public void Percentage_WithoutDuration_ReturnsZero()
    {
        var parser = new FfmpegProgressParser();
        parser.FeedLine("out_time_us=5000000");

        Assert.Equal(0, parser.Percentage);
    }

    [Fact]
    public void Percentage_WithDurationAndOutTime_ComputesRatio()
    {
        var parser = new FfmpegProgressParser();
        parser.FeedLine("  Duration: 00:00:10.00, start: 0.000000, bitrate: 128 kb/s");
        parser.FeedLine("out_time_us=2500000");

        Assert.Equal(25, parser.Percentage, 2);
    }

    [Fact]
    public void Percentage_IsClampedToHundred()
    {
        var parser = new FfmpegProgressParser();
        parser.FeedLine("  Duration: 00:00:05.00, start: 0.000000, bitrate: 128 kb/s");
        parser.FeedLine("out_time_us=6000000");

        Assert.Equal(100, parser.Percentage);
    }
}