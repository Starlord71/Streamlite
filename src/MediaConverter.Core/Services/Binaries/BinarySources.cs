using System;

namespace MediaConverter.Core.Services.Binaries;

/// <summary>
/// Central catalog of the official download and version endpoints used to provision
/// the external binaries (ffmpeg and yt-dlp).
/// </summary>
public static class BinarySources
{
    /// <summary>Gets the URL of the latest gyan.dev ffmpeg essentials release zip.</summary>
    /// <returns>The download URL for ffmpeg.</returns>
    public static Uri GetFfmpegDownloadUrl() =>
        new("https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip");

    /// <summary>Gets the URL of the plain-text file holding the latest ffmpeg release version.</summary>
    /// <returns>The version marker URL for ffmpeg.</returns>
    public static Uri GetFfmpegVersionUrl() =>
        new("https://www.gyan.dev/ffmpeg/builds/release-version");

    /// <summary>Gets the URL of the latest yt-dlp Windows executable from its GitHub releases.</summary>
    /// <returns>The download URL for yt-dlp.</returns>
    public static Uri GetYtDlpDownloadUrl() =>
        new("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe");

    /// <summary>Gets the URL of the GitHub API endpoint describing the latest yt-dlp release.</summary>
    /// <returns>The version marker URL for yt-dlp.</returns>
    public static Uri GetYtDlpVersionUrl() =>
        new("https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest");
}