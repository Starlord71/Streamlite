using System.Globalization;
using System.Text.RegularExpressions;

namespace MediaConverter.Core.Services;

/// <summary>
/// Parses the machine-readable output produced by ffmpeg's <c>-progress pipe:1</c>
/// switch (standard output) together with the <c>Duration</c> line written to
/// standard error, to derive real transcoding progress. Instances are not thread-safe.
/// </summary>
public sealed class FfmpegProgressParser
{
    private static readonly Regex DurationLineRegex = new(
        @"Duration:\s*(?<hours>\d+):(?<minutes>\d{1,2}):(?<seconds>\d{1,2}(?:\.\d+)?)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex KeyValueLineRegex = new(
        @"^(?<key>[A-Za-z0-9_]+)=(?<value>.+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Gets the input duration, once ffmpeg reports it, or <see langword="null"/> before that.
    /// </summary>
    public TimeSpan? TotalDuration { get; private set; }

    /// <summary>
    /// Gets the latest output timestamp in microseconds reported by ffmpeg, or <see langword="null"/>.
    /// </summary>
    public double? OutTimeUs { get; private set; }

    /// <summary>
    /// Gets a value indicating whether ffmpeg signaled the end of the conversion (<c>progress=end</c>).
    /// </summary>
    public bool IsEnded { get; private set; }

    /// <summary>
    /// Feeds a single line of ffmpeg output. Lines that carry no duration or progress
    /// information are ignored, so both standard output and standard error can be routed here.
    /// </summary>
    /// <param name="line">A raw line from ffmpeg standard output or standard error.</param>
    public void FeedLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        var durationMatch = DurationLineRegex.Match(line);
        if (durationMatch.Success)
        {
            TotalDuration = ParseDuration(durationMatch);
            return;
        }

        var keyValueMatch = KeyValueLineRegex.Match(line);
        if (!keyValueMatch.Success)
        {
            return;
        }

        var key = keyValueMatch.Groups["key"].Value;
        var value = keyValueMatch.Groups["value"].Value;
        switch (key)
        {
            case "out_time_us":
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var outTimeUs))
                {
                    OutTimeUs = outTimeUs;
                }

                break;

            case "progress":
                IsEnded = string.Equals(value, "end", StringComparison.Ordinal);
                break;
        }
    }

    /// <summary>
    /// Gets the conversion percentage in the 0-100 range, or 0 until both the
    /// duration and an output timestamp are known.
    /// </summary>
    public double Percentage
    {
        get
        {
            if (TotalDuration is null || OutTimeUs is null)
            {
                return 0;
            }

            var totalMicroseconds = TotalDuration.Value.Ticks / 10.0;
            if (totalMicroseconds <= 0)
            {
                return 0;
            }

            var percentage = OutTimeUs.Value / totalMicroseconds * 100;
            return Math.Clamp(percentage, 0, 100);
        }
    }

    private static TimeSpan ParseDuration(Match match)
    {
        var hours = double.Parse(match.Groups["hours"].Value, CultureInfo.InvariantCulture);
        var minutes = double.Parse(match.Groups["minutes"].Value, CultureInfo.InvariantCulture);
        var seconds = double.Parse(match.Groups["seconds"].Value, CultureInfo.InvariantCulture);
        return TimeSpan.FromSeconds((hours * 3600) + (minutes * 60) + seconds);
    }
}