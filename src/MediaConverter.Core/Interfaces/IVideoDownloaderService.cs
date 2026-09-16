using MediaConverter.Core.Models;

namespace MediaConverter.Core.Interfaces;

/// <summary>
/// Downloads videos as MP4 from a URL (for example a YouTube video) using yt-dlp.
/// </summary>
public interface IVideoDownloaderService
{
    /// <summary>
    /// Downloads the video at <paramref name="url"/> into <paramref name="outputDirectory"/>.
    /// The output file name is determined by the service.
    /// </summary>
    /// <param name="url">URL of the video to download.</param>
    /// <param name="outputDirectory">Directory where the downloaded MP4 will be saved.</param>
    /// <param name="progress">Optional receiver of <see cref="ProgressInfo"/> updates.</param>
    /// <param name="cancellationToken">Optional token used to cancel the operation.</param>
    /// <returns>An <see cref="OperationResult"/> describing the outcome.</returns>
    Task<OperationResult> DownloadAsync(
        string url,
        string outputDirectory,
        IProgress<ProgressInfo>? progress = null,
        CancellationToken cancellationToken = default);
}