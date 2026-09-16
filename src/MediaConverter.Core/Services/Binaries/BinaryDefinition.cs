using System;

namespace MediaConverter.Core.Services.Binaries;

/// <summary>
/// Describes how one external binary (ffmpeg or yt-dlp) is provisioned: its file names,
/// its download and version endpoints, and whether it is delivered as an archive.
/// </summary>
/// <param name="Name">Short machine-readable name used in progress details.</param>
/// <param name="FileName">Name of the installed executable file.</param>
/// <param name="VersionFileName">Name of the sidecar file holding the installed version marker.</param>
/// <param name="DownloadUrl">URL the binary is downloaded from.</param>
/// <param name="VersionUrl">URL the latest version marker is fetched from.</param>
/// <param name="IsArchive"><see langword="true"/> when the download is a zip archive that must be extracted.</param>
/// <param name="ArchiveExecutableName">Executable file name to extract from the archive, when <paramref name="IsArchive"/>.</param>
/// <param name="UsesGitHubJson"><see langword="true"/> when the version marker is parsed from a GitHub releases JSON document.</param>
internal sealed record BinaryDefinition(
    string Name,
    string FileName,
    string VersionFileName,
    Uri DownloadUrl,
    Uri VersionUrl,
    bool IsArchive,
    string? ArchiveExecutableName,
    bool UsesGitHubJson);