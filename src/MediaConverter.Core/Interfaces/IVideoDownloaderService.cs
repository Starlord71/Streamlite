using MediaConverter.Core.Models;

namespace MediaConverter.Core.Interfaces;

/// <summary>
/// Downloads media from a video URL (for example a YouTube video) using yt-dlp,
/// honoring the <see cref="DownloadFormat"/> chosen by the user.
/// </summary>
public interface IVideoDownloaderService
{
    /// <summary>
    /// Downloads the media at <paramref name="url"/> into <paramref name="outputDirectory"/>
    /// in the requested <paramref name="format"/>. The output file name is determined by the service.
    /// </summary>
    /// <param name="url">URL of the video to download.</param>
    /// <param name="outputDirectory">Directory where the downloaded file will be saved.</param>
    /// <param name="format">Output format: MP4 video, or MP3/M4A audio-only.</param>
    /// <param name="progress">Optional receiver of <see cref="ProgressInfo"/> updates.</param>
    /// <param name="cancellationToken">Optional token used to cancel the operation.</param>
    /// <returns>An <see cref="OperationResult"/> describing the outcome.</returns>
    Task<OperationResult> DownloadAsync(
        string url,
        string outputDirectory,
        DownloadFormat format,
        IProgress<ProgressInfo>? progress = null,
        CancellationToken cancellationToken = default);
}