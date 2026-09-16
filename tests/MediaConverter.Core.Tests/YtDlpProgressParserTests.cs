using MediaConverter.Core.Services;

namespace MediaConverter.Core.Tests;

/// <summary>
/// Tests the pure parsing logic of <see cref="YtDlpProgressParser"/> against the output
/// produced by yt-dlp with <c>--newline --progress</c>.
/// </summary>
public class YtDlpProgressParserTests
{
    [Fact]
    public void FeedLine_DownloadProgressLine_SetsPercentageAndReturnsTrue()
    {
        var parser = new YtDlpProgressParser();

        var recognized = parser.FeedLine("[download]  45.2% of   10.00MiB at    2.00MiB/s ETA 00:05");

        Assert.True(recognized);
        Assert.Equal(45.2, parser.Percentage, 2);
        Assert.False(parser.IsCompleted);
    }

    [Fact]
    public void FeedLine_FragmentedProgressLineWithTilde_SetsPercentage()
    {
        var parser = new YtDlpProgressParser();

        var recognized = parser.FeedLine("[download]  12.5% of ~  50.00MiB at    1.00MiB/s ETA 00:40");

        Assert.True(recognized);
        Assert.Equal(12.5, parser.Percentage, 2);
    }

    [Fact]
    public void FeedLine_CompleteLine_SetsPercentageToHundredAndCompleted()
    {
        var parser = new YtDlpProgressParser();

        var recognized = parser.FeedLine("[download] 100% of   10.00MiB in 00:05");

        Assert.True(recognized);
        Assert.Equal(100, parser.Percentage);
        Assert.True(parser.IsCompleted);
    }

    [Fact]
    public void FeedLine_DestinationLine_SetsDestinationAndReturnsFalse()
    {
        var parser = new YtDlpProgressParser();

        var recognized = parser.FeedLine(@"[download] Destination: C:\media\My Video.mp4");

        Assert.False(recognized);
        Assert.Equal(@"C:\media\My Video.mp4", parser.Destination);
    }

    [Fact]
    public void FeedLine_ExtractAudioDestinationLine_SetsDestination()
    {
        var parser = new YtDlpProgressParser();

        parser.FeedLine(@"[ExtractAudio] Destination: C:\media\My Song.mp3");

        Assert.Equal(@"C:\media\My Song.mp3", parser.Destination);
    }

    [Fact]
    public void FeedLine_MergerLine_SetsDestinationFromQuotedPath()
    {
        var parser = new YtDlpProgressParser();

        parser.FeedLine(@"[Merger] Merging formats into ""C:\media\My Video.mp4""");

        Assert.Equal(@"C:\media\My Video.mp4", parser.Destination);
    }

    [Fact]
    public void FeedLine_AlreadyDownloadedLine_SetsDestination()
    {
        var parser = new YtDlpProgressParser();

        parser.FeedLine(@"[download] C:\media\My Video.mp4 has already been downloaded");

        Assert.Equal(@"C:\media\My Video.mp4", parser.Destination);
    }

    [Fact]
    public void FeedLine_NoiseLines_AreIgnored()
    {
        var parser = new YtDlpProgressParser();

        Assert.False(parser.FeedLine("[youtube] Extracting URL: https://example.com/watch"));
        Assert.False(parser.FeedLine("WARNING: Falling back on generic information extractor"));
        Assert.False(parser.FeedLine("   "));
        Assert.False(parser.FeedLine(string.Empty));
        Assert.False(parser.FeedLine(null));

        Assert.Equal(0, parser.Percentage);
        Assert.Null(parser.Destination);
        Assert.False(parser.IsCompleted);
    }

    [Fact]
    public void FeedLine_LastProgressWins()
    {
        var parser = new YtDlpProgressParser();
        parser.FeedLine("[download]  10.0% of 10.00MiB at 1.00MiB/s ETA 00:09");
        parser.FeedLine("[download]  80.0% of 10.00MiB at 1.00MiB/s ETA 00:02");

        Assert.Equal(80, parser.Percentage, 2);
    }
}
