using MediaConverter.Core.Models;

namespace MediaConverter.Core.Services;

/// <summary>
/// Builds the command-line arguments passed to yt-dlp for each supported
/// <see cref="DownloadFormat"/>. Kept public and free of process concerns so the
/// selection logic can be unit tested without spawning yt-dlp.
/// </summary>
public static class YtDlpArgumentBuilder
{
    /// <summary>
    /// Format selector used for <see cref="DownloadFormat.MP4"/>: prefer an MP4 video track
    /// with an M4A audio track (native streams that merge without re-encoding), fall back to a
    /// single progressive MP4, then to any best video plus audio.
    /// </summary>
    public const string Mp4FormatSelector = "bv*[ext=mp4]+ba[ext=m4a]/b[ext=mp4]/bv*+ba/b";

    /// <summary>
    /// Format selector used for <see cref="DownloadFormat.MP3"/>: best audio-only stream,
    /// falling back to the best combined stream when no audio-only format exists.
    /// </summary>
    public const string Mp3FormatSelector = "ba/b";

    /// <summary>
    /// Format selector used for <see cref="DownloadFormat.M4A"/>: prefer a native M4A audio
    /// stream so the extraction step can copy it, falling back to the best audio then best combined.
    /// </summary>
    public const string M4aFormatSelector = "ba[ext=m4a]/ba/b";

    /// <summary>
    /// Builds the full argument list for downloading <paramref name="url"/> into
    /// <paramref name="workDirectory"/> in the requested <paramref name="format"/>.
    /// </summary>
    /// <param name="url">Video URL passed to yt-dlp as the final positional argument.</param>
    /// <param name="workDirectory">Directory where yt-dlp writes both temporary and final files.</param>
    /// <param name="format">Requested output format.</param>
    /// <param name="ffmpegLocation">
    /// Path to the ffmpeg binary or to its containing directory, forwarded through
    /// <c>--ffmpeg-location</c> so merging and audio extraction use the provisioned ffmpeg.
    /// </param>
    /// <returns>The ordered list of arguments to pass to yt-dlp.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="format"/> is not defined.</exception>
    public static IReadOnlyList<string> Build(
        string url,
        string workDirectory,
        DownloadFormat format,
        string ffmpegLocation)
    {
        var arguments = new List<string>
        {
            "--no-playlist",
            "--newline",
            "--progress",
            "--no-warnings",
            "--ffmpeg-location",
            ffmpegLocation,
            "--paths",
            $"home:{workDirectory}",
            "--paths",
            $"temp:{workDirectory}",
        };

        switch (format)
        {
            case DownloadFormat.MP4:
                arguments.Add("--format");
                arguments.Add(Mp4FormatSelector);
                arguments.Add("--merge-output-format");
                arguments.Add("mp4");
                arguments.Add("--remux-video");
                arguments.Add("mp4");
                break;

            case DownloadFormat.MP3:
                arguments.Add("--format");
                arguments.Add(Mp3FormatSelector);
                arguments.Add("--extract-audio");
                arguments.Add("--audio-format");
                arguments.Add("mp3");
                arguments.Add("--audio-quality");
                arguments.Add("0");
                break;

            case DownloadFormat.M4A:
                arguments.Add("--format");
                arguments.Add(M4aFormatSelector);
                arguments.Add("--extract-audio");
                arguments.Add("--audio-format");
                arguments.Add("m4a");
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(format),
                    format,
                    "The requested download format is not supported.");
        }

        arguments.Add("--");
        arguments.Add(url);
        return arguments;
    }
}
