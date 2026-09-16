using System.ComponentModel;
using System.Diagnostics;
using MediaConverter.Core.Interfaces;
using MediaConverter.Core.Models;

namespace MediaConverter.Core.Services;

/// <summary>
/// Converts audio files between M4A and MP3 by invoking the ffmpeg executable directly.
/// Reports real transcoding progress parsed from ffmpeg's <c>-progress pipe:1</c> output
/// and supports cooperative cancellation by killing the spawned process tree.
/// </summary>
public sealed class AudioConverterService : IAudioConverterService
{
    private readonly string _ffmpegPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioConverterService"/> class.
    /// </summary>
    /// <param name="ffmpegPath">
    /// Full path to the ffmpeg executable, or <see langword="null"/> to look for
    /// <c>ffmpeg.exe</c> next to the application executable.
    /// </param>
    public AudioConverterService(string? ffmpegPath = null)
    {
        _ffmpegPath = ffmpegPath ?? ResolveDefaultFfmpegPath();
    }

    /// <inheritdoc/>
    public async Task<OperationResult> ConvertAsync(
        string sourcePath,
        string outputPath,
        AudioFormat targetFormat,
        IProgress<ProgressInfo>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return OperationResult.Fail(ErrorCode.Cancelled);
        }

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return OperationResult.Fail(ErrorCode.InvalidInput);
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return OperationResult.Fail(ErrorCode.InvalidInput);
        }

        if (!IsDefinedFormat(targetFormat))
        {
            return OperationResult.Fail(ErrorCode.UnsupportedFormat);
        }

        if (!string.Equals(
                Path.GetExtension(outputPath),
                GetExtension(targetFormat),
                StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult.Fail(ErrorCode.InvalidInput);
        }

        if (!File.Exists(sourcePath))
        {
            return OperationResult.Fail(ErrorCode.FileNotFound);
        }

        if (!TryCreateOutputDirectory(outputPath, out var outputErrorCode))
        {
            return OperationResult.Fail(outputErrorCode);
        }

        if (!File.Exists(_ffmpegPath))
        {
            return OperationResult.Fail(ErrorCode.BinaryNotFound);
        }

        progress?.Report(new ProgressInfo(0, ProgressStage.Starting));
        progress?.Report(new ProgressInfo(0, ProgressStage.Analyzing));

        var parser = new FfmpegProgressParser();
        Process? process = null;

        try
        {
            var startInfo = BuildStartInfo(sourcePath, outputPath, targetFormat);
            process = Process.Start(startInfo);
            if (process is null)
            {
                return OperationResult.Fail(ErrorCode.ConversionFailed);
            }

            var outputReader = DrainOutputAsync(process.StandardOutput, parser, progress, cancellationToken);
            var errorReader = DrainErrorAsync(process.StandardError, parser, cancellationToken);

            try
            {
                await process.WaitForExitAsync(cancellationToken);
                await Task.WhenAll(outputReader, errorReader);
            }
            catch (OperationCanceledException)
            {
                KillProcessTree(process);
                try
                {
                    await Task.WhenAll(outputReader, errorReader);
                }
                catch
                {
                }

                return OperationResult.Fail(ErrorCode.Cancelled);
            }

            if (process.ExitCode != 0 || !File.Exists(outputPath))
            {
                return OperationResult.Fail(ErrorCode.ConversionFailed);
            }

            progress?.Report(new ProgressInfo(100, ProgressStage.Finalizing));
            progress?.Report(new ProgressInfo(100, ProgressStage.Completed));
            return OperationResult.Ok();
        }
        catch (Win32Exception)
        {
            return OperationResult.Fail(ErrorCode.BinaryNotFound);
        }
        catch (InvalidOperationException)
        {
            return OperationResult.Fail(ErrorCode.ConversionFailed);
        }
        catch
        {
            return OperationResult.Fail(ErrorCode.Unknown);
        }
        finally
        {
            process?.Dispose();
        }
    }

    /// <summary>
    /// Resolves the full path to the ffmpeg executable located next to the application
    /// executable, using <see cref="Environment.ProcessPath"/> first and falling back to
    /// <see cref="AppContext.BaseDirectory"/> when the process path is unavailable.
    /// </summary>
    /// <returns>The full path of the ffmpeg binary for the current platform.</returns>
    private static string ResolveDefaultFfmpegPath()
    {
        var baseDirectory = !string.IsNullOrEmpty(Environment.ProcessPath)
            ? Path.GetDirectoryName(Environment.ProcessPath)
            : null;
        baseDirectory ??= AppContext.BaseDirectory;
        var binaryName = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        return Path.Combine(baseDirectory, binaryName);
    }

    private static bool IsDefinedFormat(AudioFormat format) => format is AudioFormat.M4A or AudioFormat.MP3;

    private static string GetExtension(AudioFormat format) =>
        format switch
        {
            AudioFormat.M4A => ".m4a",
            AudioFormat.MP3 => ".mp3",
            _ => string.Empty,
        };

    /// <summary>
    /// Creates the directory that will contain the output file if it does not exist yet.
    /// </summary>
    /// <param name="outputPath">Full path of the output file.</param>
    /// <param name="errorCode">Receives the <see cref="ErrorCode"/> describing a failure.</param>
    /// <returns><see langword="true"/> when the directory is ready; otherwise <see langword="false"/>.</returns>
    private static bool TryCreateOutputDirectory(string outputPath, out ErrorCode errorCode)
    {
        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (string.IsNullOrEmpty(outputDirectory))
        {
            errorCode = ErrorCode.Unknown;
            return true;
        }

        try
        {
            Directory.CreateDirectory(outputDirectory);
            errorCode = ErrorCode.Unknown;
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            errorCode = ErrorCode.AccessDenied;
            return false;
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or NotSupportedException or PathTooLongException)
        {
            errorCode = ErrorCode.OutputPathInvalid;
            return false;
        }
    }

    private ProcessStartInfo BuildStartInfo(string sourcePath, string outputPath, AudioFormat targetFormat)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        startInfo.ArgumentList.Add("-y");
        startInfo.ArgumentList.Add("-hide_banner");
        startInfo.ArgumentList.Add("-nostats");
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(sourcePath);
        startInfo.ArgumentList.Add("-map_metadata");
        startInfo.ArgumentList.Add("0");
        startInfo.ArgumentList.Add("-vn");
        startInfo.ArgumentList.Add("-progress");
        startInfo.ArgumentList.Add("pipe:1");

        switch (targetFormat)
        {
            case AudioFormat.MP3:
                startInfo.ArgumentList.Add("-c:a");
                startInfo.ArgumentList.Add("libmp3lame");
                startInfo.ArgumentList.Add("-q:a");
                startInfo.ArgumentList.Add("2");
                break;

            case AudioFormat.M4A:
                startInfo.ArgumentList.Add("-c:a");
                startInfo.ArgumentList.Add("aac");
                startInfo.ArgumentList.Add("-b:a");
                startInfo.ArgumentList.Add("192k");
                break;
        }

        startInfo.ArgumentList.Add(outputPath);
        return startInfo;
    }

    /// <summary>
    /// Reads ffmpeg standard output line by line, feeding the progress parser and reporting
    /// converting progress until the stream is closed or the operation is cancelled.
    /// </summary>
    /// <param name="reader">The redirected standard output stream.</param>
    /// <param name="parser">The parser that accumulates progress state.</param>
    /// <param name="progress">Optional receiver of progress updates.</param>
    /// <param name="cancellationToken">Token used to cancel the read operation.</param>
    /// <returns>A task that completes when the stream is drained.</returns>
    private static async Task DrainOutputAsync(
        StreamReader reader,
        FfmpegProgressParser parser,
        IProgress<ProgressInfo>? progress,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                return;
            }

            parser.FeedLine(line);
            progress?.Report(new ProgressInfo(parser.Percentage, ProgressStage.Converting));
        }
    }

    /// <summary>
    /// Reads ffmpeg standard error line by line, feeding the progress parser so the input
    /// duration can be detected, until the stream is closed or the operation is cancelled.
    /// </summary>
    /// <param name="reader">The redirected standard error stream.</param>
    /// <param name="parser">The parser that accumulates progress state.</param>
    /// <param name="cancellationToken">Token used to cancel the read operation.</param>
    /// <returns>A task that completes when the stream is drained.</returns>
    private static async Task DrainErrorAsync(
        StreamReader reader,
        FfmpegProgressParser parser,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                return;
            }

            parser.FeedLine(line);
        }
    }

    private static void KillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (Win32Exception)
        {
        }
    }
}