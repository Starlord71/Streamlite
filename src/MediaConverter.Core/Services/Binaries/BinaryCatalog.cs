using System.Collections.Generic;

namespace MediaConverter.Core.Services.Binaries;

/// <summary>
/// The set of external binaries the application provisions, in install order.
/// </summary>
internal static class BinaryCatalog
{
    /// <summary>Gets all binaries that need provisioning, in install order.</summary>
    public static IReadOnlyList<BinaryDefinition> All { get; } = new[]
    {
        new BinaryDefinition(
            Name: "ffmpeg",
            FileName: "ffmpeg.exe",
            VersionFileName: "ffmpeg.version",
            DownloadUrl: BinarySources.GetFfmpegDownloadUrl(),
            VersionUrl: BinarySources.GetFfmpegVersionUrl(),
            IsArchive: true,
            ArchiveExecutableName: "ffmpeg.exe",
            UsesGitHubJson: false),
        new BinaryDefinition(
            Name: "yt-dlp",
            FileName: "yt-dlp.exe",
            VersionFileName: "yt-dlp.version",
            DownloadUrl: BinarySources.GetYtDlpDownloadUrl(),
            VersionUrl: BinarySources.GetYtDlpVersionUrl(),
            IsArchive: false,
            ArchiveExecutableName: null,
            UsesGitHubJson: true),
    };
}