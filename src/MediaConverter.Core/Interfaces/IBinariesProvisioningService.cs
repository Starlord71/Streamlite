using MediaConverter.Core.Models;

namespace MediaConverter.Core.Interfaces;

/// <summary>
/// Ensures the external binaries (ffmpeg and yt-dlp) are available next to the application
/// and keeps them up to date.
/// </summary>
public interface IBinariesProvisioningService
{
    /// <summary>
    /// Ensures ffmpeg and yt-dlp are present, downloading them if they are missing.
    /// </summary>
    /// <param name="progress">Optional receiver of <see cref="ProgressInfo"/> updates.</param>
    /// <param name="cancellationToken">Optional token used to cancel the operation.</param>
    /// <returns>An <see cref="OperationResult"/> describing the outcome.</returns>
    Task<OperationResult> ProvisionAsync(
        IProgress<ProgressInfo>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks for newer versions of ffmpeg and yt-dlp and replaces the local binaries when available.
    /// </summary>
    /// <param name="progress">Optional receiver of <see cref="ProgressInfo"/> updates.</param>
    /// <param name="cancellationToken">Optional token used to cancel the operation.</param>
    /// <returns>An <see cref="OperationResult"/> describing the outcome.</returns>
    Task<OperationResult> UpdateAsync(
        IProgress<ProgressInfo>? progress = null,
        CancellationToken cancellationToken = default);
}