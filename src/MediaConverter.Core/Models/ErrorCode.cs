namespace MediaConverter.Core.Models;

/// <summary>
/// Machine-readable error codes returned by Core operations.
/// Core never returns user-facing strings; the UI layer maps each
/// code to the active language (ES/EN).
/// </summary>
public enum ErrorCode
{
    /// <summary>An unexpected error occurred.</summary>
    Unknown,

    /// <summary>The operation was cancelled by the user.</summary>
    Cancelled,

    /// <summary>A required argument was null, empty or malformed.</summary>
    InvalidInput,

    /// <summary>The input file does not exist or cannot be accessed.</summary>
    FileNotFound,

    /// <summary>The destination path is invalid or its directory cannot be created.</summary>
    OutputPathInvalid,

    /// <summary>The input or requested output format is not supported.</summary>
    UnsupportedFormat,

    /// <summary>The provided URL is not a valid or supported video URL.</summary>
    InvalidUrl,

    /// <summary>A network request failed (no connection, timeout, HTTP error).</summary>
    NetworkError,

    /// <summary>Access to a file, folder or resource was denied.</summary>
    AccessDenied,

    /// <summary>There is not enough free disk space to write the output.</summary>
    InsufficientDiskSpace,

    /// <summary>A required external binary (ffmpeg, yt-dlp) is missing.</summary>
    BinaryNotFound,

    /// <summary>An external binary could not be downloaded.</summary>
    BinaryDownloadFailed,

    /// <summary>The external binaries could not be provisioned or extracted.</summary>
    ProvisioningFailed,

    /// <summary>An audio conversion failed (ffmpeg exited with an error).</summary>
    ConversionFailed,

    /// <summary>A video download failed (yt-dlp exited with an error).</summary>
    DownloadFailed,
}