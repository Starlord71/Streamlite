using System.Globalization;
using System.Text.RegularExpressions;

namespace MediaConverter.Core.Services;

/// <summary>
/// Parses the output produced by yt-dlp when it is invoked with <c>--newline --progress</c>,
/// extracting the download percentage and the destination paths it reports for downloaded,
/// merged or extracted files. Lines that carry no relevant information are ignored.
/// Instances are not thread-safe.
/// </summary>
public sealed class YtDlpProgressParser
{
    private static readonly Regex ProgressLineRegex = new(
        @"^\[download\]\s+(?<percent>\d{1,3}(?:\.\d+)?)%",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex DestinationLineRegex = new(
        @"^\[(?:download|ExtractAudio)\]\s+Destination:\s*(?<path>.+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex MergerLineRegex = new(
        @"^\[Merger\]\s+Merging formats into\s+""(?<path>.+)""$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex AlreadyDownloadedLineRegex = new(
        @"^\[download\]\s+(?<path>.+?)\s+has already been downloaded$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Gets the latest download percentage in the 0-100 range, or 0 before the first progress line.
    /// </summary>
    public double Percentage { get; private set; }

    /// <summary>
    /// Gets the last destination path reported by yt-dlp (downloaded, merged or extracted file),
    /// or <see langword="null"/> when no destination line has been seen yet.
    /// </summary>
    public string? Destination { get; private set; }

    /// <summary>
    /// Gets a value indicating whether yt-dlp reported a fully downloaded file (100%).
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// Feeds a single line of yt-dlp output. Progress lines update <see cref="Percentage"/> and
    /// <see cref="IsCompleted"/>; destination lines update <see cref="Destination"/>; every other
    /// line (extractor banners, warnings, etc.) is ignored.
    /// </summary>
    /// <param name="line">A raw line from yt-dlp standard output or standard error.</param>
    /// <returns>
    /// <see langword="true"/> when the line carried download progress; otherwise <see langword="false"/>.
    /// </returns>
    public bool FeedLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var progressMatch = ProgressLineRegex.Match(line);
        if (progressMatch.Success
            && double.TryParse(
                progressMatch.Groups["percent"].Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var percentage))
        {
            Percentage = Math.Clamp(percentage, 0, 100);
            IsCompleted = Percentage >= 100;
            return true;
        }

        var destination = ExtractDestination(line);
        if (destination is not null)
        {
            Destination = destination.Trim().Trim('"');
        }

        return false;
    }

    /// <summary>
    /// Extracts the artifact path from any of the destination lines yt-dlp emits for downloads,
    /// merges and audio extraction.
    /// </summary>
    /// <param name="line">A single yt-dlp output line.</param>
    /// <returns>The reported path, or <see langword="null"/> when the line is not a destination line.</returns>
    private static string? ExtractDestination(string line)
    {
        var destinationMatch = DestinationLineRegex.Match(line);
        if (destinationMatch.Success)
        {
            return destinationMatch.Groups["path"].Value;
        }

        var mergerMatch = MergerLineRegex.Match(line);
        if (mergerMatch.Success)
        {
            return mergerMatch.Groups["path"].Value;
        }

        var alreadyDownloadedMatch = AlreadyDownloadedLineRegex.Match(line);
        if (alreadyDownloadedMatch.Success)
        {
            return alreadyDownloadedMatch.Groups["path"].Value;
        }

        return null;
    }
}
