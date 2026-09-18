using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaConverter.Core.Interfaces;
using MediaConverter.Core.Models;
using MediaConverter.Core.Services.Binaries;

namespace MediaConverter.Core.Services;

/// <summary>
/// Provisions ffmpeg and yt-dlp next to the application executable, downloading and
/// extracting them when missing and replacing them during an update when a newer version
/// is available. Downloads report real byte-based progress and support cancellation,
/// cleaning up partial files so nothing is left behind.
/// </summary>
public sealed class BinariesProvisioningService : IBinariesProvisioningService, IDisposable
{
    private readonly string _binariesDirectory;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="BinariesProvisioningService"/> class.
    /// </summary>
    /// <param name="binariesDirectory">
    /// Directory where binaries are installed, or <see langword="null"/> to use the application
    /// data folder (<see cref="BinaryPaths.GetApplicationDataDirectory"/>), which lives in the
    /// user's local application data folder so nothing is written next to the executable.
    /// Injecting a directory keeps tests isolated from the real application folder.
    /// </param>
    /// <param name="httpClient">
    /// Optional <see cref="HttpClient"/> used for downloads, injected so tests can substitute
    /// a stub handler. When omitted, the service creates and owns its own client.
    /// </param>
    public BinariesProvisioningService(string? binariesDirectory = null, HttpClient? httpClient = null)
    {
        _binariesDirectory = string.IsNullOrWhiteSpace(binariesDirectory)
            ? BinaryPaths.GetApplicationDataDirectory()
            : binariesDirectory;
        _httpClient = httpClient ?? CreateDefaultHttpClient();
        _ownsHttpClient = httpClient is null;
    }

    /// <inheritdoc/>
    public async Task<OperationResult> ProvisionAsync(
        IProgress<ProgressInfo>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return OperationResult.Fail(ErrorCode.Cancelled);
        }

        if (!TryCreateBinariesDirectory(_binariesDirectory, out var directoryErrorCode))
        {
            return OperationResult.Fail(directoryErrorCode);
        }

        progress?.Report(new ProgressInfo(0, ProgressStage.Starting));
        try
        {
            foreach (var binary in BinaryCatalog.All)
            {
                var targetPath = Path.Combine(_binariesDirectory, binary.FileName);
                if (IsUsableFile(targetPath))
                {
                    continue;
                }

                var downloader = new BinaryDownloader(_httpClient, progress, binary.Name);
                var result = await InstallBinaryAsync(downloader, binary, null, progress, cancellationToken);
                if (!result.Succeeded)
                {
                    return result;
                }
            }
        }
        catch (OperationCanceledException)
        {
            return OperationResult.Fail(ErrorCode.Cancelled);
        }
        catch (Exception exception)
        {
            return OperationResult.Fail(ProvisioningExceptionMapper.Map(exception));
        }

        progress?.Report(new ProgressInfo(100, ProgressStage.Completed));
        return OperationResult.Ok();
    }

    /// <inheritdoc/>
    public async Task<OperationResult> UpdateAsync(
        IProgress<ProgressInfo>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return OperationResult.Fail(ErrorCode.Cancelled);
        }

        if (!TryCreateBinariesDirectory(_binariesDirectory, out var directoryErrorCode))
        {
            return OperationResult.Fail(directoryErrorCode);
        }

        progress?.Report(new ProgressInfo(0, ProgressStage.Starting));
        try
        {
            foreach (var binary in BinaryCatalog.All)
            {
                var downloader = new BinaryDownloader(_httpClient, progress, binary.Name);
                var targetPath = Path.Combine(_binariesDirectory, binary.FileName);
                var markerPath = Path.Combine(_binariesDirectory, binary.VersionFileName);

                if (!IsUsableFile(targetPath))
                {
                    var result = await InstallBinaryAsync(downloader, binary, null, progress, cancellationToken);
                    if (!result.Succeeded)
                    {
                        return result;
                    }

                    continue;
                }

                var localVersion = ReadMarker(markerPath);
                if (string.IsNullOrWhiteSpace(localVersion))
                {
                    // Without a known local version the comparison is not reliable;
                    // leave the existing binary untouched.
                    continue;
                }

                var latestVersion = await downloader.FetchLatestVersionAsync(binary, cancellationToken);
                if (latestVersion is not null && VersionComparer.IsNewer(latestVersion, localVersion))
                {
                    var result = await InstallBinaryAsync(downloader, binary, latestVersion, progress, cancellationToken);
                    if (!result.Succeeded)
                    {
                        return result;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            return OperationResult.Fail(ErrorCode.Cancelled);
        }
        catch (Exception exception)
        {
            return OperationResult.Fail(ProvisioningExceptionMapper.Map(exception));
        }

        progress?.Report(new ProgressInfo(100, ProgressStage.Completed));
        return OperationResult.Ok();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    /// <summary>
    /// Downloads and installs a single binary, writing its version marker when known.
    /// Any partial file or temporary extraction folder is removed in all exit paths.
    /// </summary>
    /// <param name="downloader">The downloader bound to the binary's progress reporting.</param>
    /// <param name="binary">The binary to install.</param>
    /// <param name="version">The known latest version, or <see langword="null"/> to fetch it best-effort.</param>
    /// <param name="progress">Optional receiver of progress updates.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>An <see cref="OperationResult"/> describing the outcome.</returns>
    private async Task<OperationResult> InstallBinaryAsync(
        BinaryDownloader downloader,
        BinaryDefinition binary,
        string? version,
        IProgress<ProgressInfo>? progress,
        CancellationToken cancellationToken)
    {
        if (version is null)
        {
            version = await TryFetchLatestVersionAsync(downloader, binary, cancellationToken);
        }

        var targetPath = Path.Combine(_binariesDirectory, binary.FileName);
        var markerPath = Path.Combine(_binariesDirectory, binary.VersionFileName);
        string? partPath = null;
        string? extractDirectory = null;

        try
        {
            partPath = await downloader.DownloadAsync(binary.DownloadUrl, targetPath, cancellationToken);
            progress?.Report(new ProgressInfo(100, ProgressStage.Installing, binary.Name));

            if (binary.IsArchive)
            {
                extractDirectory = Path.Combine(Path.GetTempPath(), $"MediaConverter-{binary.Name}-{Guid.NewGuid():N}");
                Directory.CreateDirectory(extractDirectory);
                ZipFile.ExtractToDirectory(partPath, extractDirectory);

                var executablePath = FindFileRecursive(extractDirectory, binary.ArchiveExecutableName!);
                if (executablePath is null)
                {
                    throw new InvalidDataException(
                        $"Archive for '{binary.Name}' did not contain '{binary.ArchiveExecutableName}'.");
                }

                File.Move(executablePath, targetPath, overwrite: true);
            }
            else
            {
                File.Move(partPath, targetPath, overwrite: true);
            }

            if (!string.IsNullOrWhiteSpace(version))
            {
                WriteMarker(markerPath, version);
            }

            return OperationResult.Ok();
        }
        finally
        {
            TryDeleteFile(partPath);
            if (extractDirectory is not null)
            {
                TryDeleteDirectory(extractDirectory);
            }
        }
    }

    /// <summary>
    /// Fetches the latest version marker, swallowing non-cancellation failures so a missing
    /// marker never blocks provisioning the binary itself.
    /// </summary>
    private static async Task<string?> TryFetchLatestVersionAsync(
        BinaryDownloader downloader,
        BinaryDefinition binary,
        CancellationToken cancellationToken)
    {
        try
        {
            return await downloader.FetchLatestVersionAsync(binary, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Creates the binaries directory when it does not exist yet, mapping failures to codes.
    /// </summary>
    private static bool TryCreateBinariesDirectory(string directory, out ErrorCode errorCode)
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
    /// Determines whether an installed binary file is usable, treating zero-byte leftovers
    /// from interrupted runs as missing.
    /// </summary>
    private static bool IsUsableFile(string path)
    {
        return File.Exists(path) && new FileInfo(path).Length > 0;
    }

    private static string? ReadMarker(string markerPath)
    {
        return File.Exists(markerPath) ? File.ReadAllText(markerPath).Trim() : null;
    }

    private static void WriteMarker(string markerPath, string version)
    {
        File.WriteAllText(markerPath, version.Trim());
    }

    /// <summary>
    /// Finds the first file with the given name under a directory tree, used to locate the
    /// executable inside an extracted archive whose root folder name varies.
    /// </summary>
    private static string? FindFileRecursive(string directory, string fileName)
    {
        foreach (var path in Directory.EnumerateFiles(directory, fileName, SearchOption.AllDirectories))
        {
            return path;
        }

        return null;
    }

    private static void TryDeleteFile(string? path)
    {
        if (path is null)
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static HttpClient CreateDefaultHttpClient()
    {
        return new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5),
        };
    }
}