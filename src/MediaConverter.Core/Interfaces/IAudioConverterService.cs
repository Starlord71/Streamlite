using MediaConverter.Core.Models;

namespace MediaConverter.Core.Interfaces;

/// <summary>
/// Converts audio files between supported formats (M4A and MP3) using ffmpeg.
/// </summary>
public interface IAudioConverterService
{
    /// <summary>
    /// Converts the audio file at <paramref name="sourcePath"/> to <paramref name="targetFormat"/>
    /// and writes the result to <paramref name="outputPath"/>.
    /// </summary>
    /// <param name="sourcePath">Full path to the input audio file.</param>
    /// <param name="outputPath">Full path where the converted file will be written. Its extension should match <paramref name="targetFormat"/>.</param>
    /// <param name="targetFormat">Format to convert to.</param>
    /// <param name="progress">Optional receiver of <see cref="ProgressInfo"/> updates.</param>
    /// <param name="cancellationToken">Optional token used to cancel the operation.</param>
    /// <returns>An <see cref="OperationResult"/> describing the outcome.</returns>
    Task<OperationResult> ConvertAsync(
        string sourcePath,
        string outputPath,
        AudioFormat targetFormat,
        IProgress<ProgressInfo>? progress = null,
        CancellationToken cancellationToken = default);
}