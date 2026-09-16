using System.ComponentModel;
using System.Diagnostics;
using MediaConverter.Core.Interfaces;
using MediaConverter.Core.Models;
using MediaConverter.Core.Services.Binaries;

namespace MediaConverter.Core.Services;

/// <summary>
/// Downloads media from a video URL by invoking the yt-dlp executable directly, honoring the
/// requested <see cref="DownloadFormat"/>. Real download progress is parsed from yt-dlp's
/// <c>--newline --progress</c> output, and cancellation kills the spawned process tree and
/// removes every partial artifact.
/// </summary>
/// <remarks>
/// yt-dlp is pointed at an isolated working directory (both its <c>home</c> and <c>temp</c> paths)
/// inside the requested output directory. Once it succeeds, the single final file it produced is
/// moved to the output directory under the name yt-dlp chose, and the working directory is removed.
/// This guarantees that the operation leaves exactly one file behind and that a cancelled run
/// never leaks <c>.part</c> or intermediate files into the user's folder.
/// </remarks>
public sealed class VideoDownloaderService : IVideoDownloaderService
{
    private const string WorkDirectoryPrefix = ".mediaconverter-";

    private readonly string _ytDlpPath;
    private readonly string _ffmpegPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoDownloaderService"/> class.
    /// </summary>
    /// <param name="ytDlpPath">
    /// Full path to the yt-dlp executable, or <see langword="null"/> to look for it next to the
    /// application executable.
    /// </param>
    /// <param name="ffmpegPath">
    /// Full path to the ffmpeg executable used by yt-dlp for merging and audio extraction, or
    /// <see langword="null"/> to look for it next to the application executable.
    /// </param>
    public VideoDownloaderService(string? ytDlpPath = null, string? ffmpegPath = null)
    {
        _ytDlpPath = string.IsNullOrWhiteSpace(ytDlpPath)
            ? ResolveDefaultBinaryPath("yt-dlp")
            : ytDlpPath;
        _ffmpegPath = string.IsNullOrWhiteSpace(ffmpegPath)
            ? ResolveDefaultBinaryPath("ffmpeg")
            : ffmpegPath;
    }

    /// <inheritdoc/>
    public async Task<OperationResult> DownloadAsync(
        string url,
        string outputDirectory,
        DownloadFormat format,
        IProgress<ProgressInfo>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return OperationResult.Fail(ErrorCode.Cancelled);
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            return OperationResult.Fail(ErrorCode.InvalidInput);
        }

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            return OperationResult.Fail(ErrorCode.InvalidInput);
        }

        if (!IsDefinedFormat(format))
        {
            return OperationResult.Fail(ErrorCode.UnsupportedFormat);
        }

        if (!IsSupportedUrl(url))
        {
            return OperationResult.Fail(ErrorCode.InvalidUrl);
        }

        if (!TryCreateDirectory(outputDirectory, out var directoryErrorCode))
        {
            return OperationResult.Fail(directoryErrorCode);
        }

        if (!File.Exists(_ytDlpPath) || !File.Exists(_ffmpegPath))
        {
            return OperationResult.Fail(ErrorCode.BinaryNotFound);
        }

        progress?.Report(new ProgressInfo(0, ProgressStage.Starting));
        progress?.Report(new ProgressInfo(0, ProgressStage.Analyzing));

        var workDirectory = Path.Combine(outputDirectory, WorkDirectoryPrefix + Guid.NewGuid().ToString("N"));
        if (!TryCreateDirectory(workDirectory, out var workDirectoryErrorCode))
        {
            return OperationResult.Fail(workDirectoryErrorCode);
        }

        var parser = new YtDlpProgressParser();
        var errorLines = new List<string>();
        Process? process = null;

        try
        {
            progress?.Report(new ProgressInfo(0, ProgressStage.Downloading));

            var startInfo = BuildStartInfo(url, workDirectory, format);
            process = Process.Start(startInfo);
            if (process is null)
            {
                return OperationResult.Fail(ErrorCode.DownloadFailed);
            }

            var outputReader = DrainAsync(process.StandardOutput, parser, progress, null, cancellationToken);
            var errorReader = DrainAsync(process.StandardError, parser, null, errorLines, cancellationToken);

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

                await WaitForExitQuietlyAsync(process);
                return OperationResult.Fail(ErrorCode.Cancelled);
            }

            if (process.ExitCode != 0)
            {
                return OperationResult.Fail(YtDlpErrorClassifier.Classify(string.Join('\n', errorLines)));
            }

            progress?.Report(new ProgressInfo(100, ProgressStage.Finalizing));

            var finalFile = DownloadArtifactLocator.FindFinalFile(workDirectory, format);
            if (finalFile is null)
            {
                return OperationResult.Fail(ErrorCode.DownloadFailed);
            }

            var destinationPath = Path.Combine(outputDirectory, Path.GetFileName(finalFile));
            var moveResult = TryMoveToOutput(finalFile, destinationPath);
            if (!moveResult.Succeeded)
            {
                return moveResult;
            }

            progress?.Report(new ProgressInfo(100, ProgressStage.Completed));
            return OperationResult.Ok();
        }
        catch (Win32Exception)
        {
            return OperationResult.Fail(ErrorCode.BinaryNotFound);
        }
        catch (InvalidOperationException)
        {
            return OperationResult.Fail(ErrorCode.DownloadFailed);
        }
        catch (IOException)
        {
            return OperationResult.Fail(ErrorCode.OutputPathInvalid);
        }
        catch (UnauthorizedAccessException)
        {
            return OperationResult.Fail(ErrorCode.AccessDenied);
        }
        catch
        {
            return OperationResult.Fail(ErrorCode.Unknown);
        }
        finally
        {
            process?.Dispose();
            CleanupWorkDirectory(workDirectory);
        }
    }

    /// <summary>
    /// Resolves the full path to a binary located next to the application executable, using
    /// <see cref="BinaryPaths.GetExecutableDirectory"/> so single-file publish works correctly.
    /// </summary>
    /// <param name="binaryName">The platform-independent binary name, for example <c>yt-dlp</c>.</param>
    /// <returns>The full path of the binary for the current platform.</returns>
    private static string ResolveDefaultBinaryPath(string binaryName)
    {
        var fileName = OperatingSystem.IsWindows() ? binaryName + ".exe" : binaryName;
        return Path.Combine(BinaryPaths.GetExecutableDirectory(), fileName);
    }

    private static bool IsDefinedFormat(DownloadFormat format) =>
        format is DownloadFormat.MP4 or DownloadFormat.MP3 or DownloadFormat.M4A;

    /// <summary>
    /// Determines whether the URL is an absolute HTTP or HTTPS URL, which yt-dlp can handle.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <returns><see langword="true"/> when the URL is well formed and supported.</returns>
    private static bool IsSupportedUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (string.IsNullOrEmpty(uri.Host))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }

    /// <summary>
    /// Creates a directory when it does not exist yet, mapping failures to error codes.
    /// </summary>
    /// <param name="directory">The directory to create.</param>
    /// <param name="errorCode">Receives the <see cref="ErrorCode"/> describing a failure.</param>
    /// <returns><see langword="true"/> when the directory is ready; otherwise <see langword="false"/>.</returns>
    private static bool TryCreateDirectory(string directory, out ErrorCode errorCode)
    {
        try
        {
            Directory.CreateDirectory(directory);
            errorCode = ErrorCode.Unknown;
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            errorCode = ErrorCode.AccessDenied;
            return false;
        }
        catch (Exception exception) when (exception is IOException or ArgumentException or NotSupportedException or PathTooLongException)
        {
            errorCode = ErrorCode.OutputPathInvalid;
            return false;
        }
    }

    /// <summary>
    /// Builds the yt-dlp process configuration for a download.
    /// </summary>
    /// <param name="url">The URL to download.</param>
    /// <param name="workDirectory">The isolated working directory for the download.</param>
    /// <param name="format">The requested output format.</param>
    /// <returns>The <see cref="ProcessStartInfo"/> used to launch yt-dlp.</returns>
    private ProcessStartInfo BuildStartInfo(string url, string workDirectory, DownloadFormat format)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _ytDlpPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var argument in YtDlpArgumentBuilder.Build(url, workDirectory, format, ResolveFfmpegLocation()))
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    /// <summary>
    /// Resolves the value passed to yt-dlp's <c>--ffmpeg-location</c>, preferring the directory that
    /// contains ffmpeg and falling back to the binary path when it has no directory component.
    /// </summary>
    /// <returns>The ffmpeg location accepted by yt-dlp.</returns>
    private string ResolveFfmpegLocation()
    {
        var directory = Path.GetDirectoryName(_ffmpegPath);
        return string.IsNullOrEmpty(directory) ? _ffmpegPath : directory;
    }

    /// <summary>
    /// Reads a redirected process stream line by line, feeding the progress parser and reporting
    /// <see cref="ProgressStage.Downloading"/> updates when a line carried progress. Optionally
    /// collects lines so a failed run can be classified.
    /// </summary>
    /// <param name="reader">The redirected stream to drain.</param>
    /// <param name="parser">The parser that accumulates progress state.</param>
    /// <param name="progress">Optional receiver of progress updates.</param>
    /// <param name="collectedLines">Optional list that receives every raw line for later classification.</param>
    /// <param name="cancellationToken">Token used to cancel the read operation.</param>
    /// <returns>A task that completes when the stream is drained.</returns>
    private static async Task DrainAsync(
        StreamReader reader,
        YtDlpProgressParser parser,
        IProgress<ProgressInfo>? progress,
        List<string>? collectedLines,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                return;
            }

            if (parser.FeedLine(line))
            {
                progress?.Report(new ProgressInfo(parser.Percentage, ProgressStage.Downloading));
            }

            collectedLines?.Add(line);
        }
    }

    /// <summary>
    /// Moves the final artifact into the output directory, mapping move failures to error codes.
    /// </summary>
    /// <param name="sourcePath">Full path of the file produced inside the working directory.</param>
    /// <param name="destinationPath">Full path where the file must end up.</param>
    /// <returns>An <see cref="OperationResult"/> describing the outcome.</returns>
    private static OperationResult TryMoveToOutput(string sourcePath, string destinationPath)
    {
        try
        {
            File.Move(sourcePath, destinationPath, overwrite: true);
            return OperationResult.Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return OperationResult.Fail(ErrorCode.AccessDenied);
        }
        catch (IOException)
        {
            return OperationResult.Fail(ErrorCode.DownloadFailed);
        }
    }

    /// <summary>
    /// Waits for a terminating process to fully exit, ignoring failures, so its file handles are
    /// released before the working directory is removed.
    /// </summary>
    /// <param name="process">The process to wait for.</param>
    /// <returns>A task that completes when the process has exited or waiting is no longer possible.</returns>
    private static async Task WaitForExitQuietlyAsync(Process process)
    {
        try
        {
            await process.WaitForExitAsync();
        }
        catch (InvalidOperationException)
        {
        }
    }

    /// <summary>
    /// Kills the given process together with its child processes, swallowing errors raised when the
    /// process has already exited.
    /// </summary>
    /// <param name="process">The process tree to terminate.</param>
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

    /// <summary>
    /// Removes the isolated working directory and everything yt-dlp may have left inside it
    /// (partial downloads, intermediate formats, metadata files), retrying briefly while the
    /// just-killed process releases its file handles.
    /// </summary>
    /// <param name="workDirectory">The working directory to delete.</param>
    private static void CleanupWorkDirectory(string workDirectory)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                if (Directory.Exists(workDirectory))
                {
                    Directory.Delete(workDirectory, recursive: true);
                }

                return;
            }
            catch (IOException)
            {
                Thread.Sleep(50);
            }
            catch (UnauthorizedAccessException)
            {
                Thread.Sleep(50);
            }
        }
    }
}
