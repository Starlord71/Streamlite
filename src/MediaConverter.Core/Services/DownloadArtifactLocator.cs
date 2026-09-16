using System;
using System.IO;
using System.Linq;
using MediaConverter.Core.Models;

namespace MediaConverter.Core.Services;

/// <summary>
/// Locates the single final artifact produced by a yt-dlp download inside its isolated
/// working directory and maps each <see cref="DownloadFormat"/> to its expected extension.
/// </summary>
public static class DownloadArtifactLocator
{
    /// <summary>
    /// Gets the file extension (including the leading dot) expected for a download format.
    /// </summary>
    /// <param name="format">The requested download format.</param>
    /// <returns>The expected extension, for example <c>.mp4</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="format"/> is not defined.</exception>
    public static string GetExpectedExtension(DownloadFormat format) =>
        format switch
        {
            DownloadFormat.MP4 => ".mp4",
            DownloadFormat.MP3 => ".mp3",
            DownloadFormat.M4A => ".m4a",
            _ => throw new ArgumentOutOfRangeException(
                nameof(format),
                format,
                "The requested download format is not supported."),
        };

    /// <summary>
    /// Finds the final file produced by yt-dlp in <paramref name="workDirectory"/> by matching the
    /// expected extension: intermediate formats (for example the native M4A or WEBM stream kept
    /// during audio extraction) and partial downloads (<c>.part</c>) are ignored. When more than one
    /// candidate remains, the most recently written one wins.
    /// </summary>
    /// <param name="workDirectory">Directory used exclusively for the download.</param>
    /// <param name="format">The format that was requested.</param>
    /// <returns>The full path of the final file, or <see langword="null"/> when none is present.</returns>
    public static string? FindFinalFile(string workDirectory, DownloadFormat format)
    {
        var extension = GetExpectedExtension(format);
        var candidates = Directory
            .EnumerateFiles(workDirectory, "*", SearchOption.AllDirectories)
            .Where(path => string.Equals(Path.GetExtension(path), extension, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates
            .OrderByDescending(GetLastWriteTimeUtcSafe)
            .First();
    }

    /// <summary>
    /// Reads a file's last write time, returning <see cref="DateTime.MinValue"/> when the file
    /// disappears between enumeration and the read.
    /// </summary>
    /// <param name="path">The file whose timestamp is read.</param>
    /// <returns>The last write time in UTC, or <see cref="DateTime.MinValue"/>.</returns>
    private static DateTime GetLastWriteTimeUtcSafe(string path)
    {
        try
        {
            return File.GetLastWriteTimeUtc(path);
        }
        catch (IOException)
        {
            return DateTime.MinValue;
        }
    }
}
