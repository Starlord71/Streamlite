using System;
using System.Collections.Generic;
using System.IO;
using MediaConverter.Core.Models;

namespace MediaConverter.App.Services;

/// <summary>
/// Helpers for the audio conversion screen: detecting the format of a chosen file, mapping formats
/// to file extensions and building a non-colliding output path next to the source file.
/// </summary>
public static class AudioFileSupport
{
    /// <summary>
    /// Gets the audio formats the converter can handle, used to render the target format options.
    /// </summary>
    public static IReadOnlyList<AudioFormat> Formats { get; } = new[] { AudioFormat.M4A, AudioFormat.MP3 };

    /// <summary>
    /// Detects the audio format from a file path's extension.
    /// </summary>
    /// <param name="path">Path of the audio file.</param>
    /// <param name="format">Receives the detected format when the extension is supported.</param>
    /// <returns><see langword="true"/> when the extension maps to a supported format.</returns>
    public static bool TryDetectFormat(string path, out AudioFormat format)
    {
        var extension = Path.GetExtension(path);
        if (string.Equals(extension, ".m4a", StringComparison.OrdinalIgnoreCase))
        {
            format = AudioFormat.M4A;
            return true;
        }

        if (string.Equals(extension, ".mp3", StringComparison.OrdinalIgnoreCase))
        {
            format = AudioFormat.MP3;
            return true;
        }

        format = default;
        return false;
    }

    /// <summary>
    /// Returns the file extension (including the dot) that matches an audio format.
    /// </summary>
    /// <param name="format">The audio format.</param>
    /// <returns>The matching extension, or an empty string for an undefined format.</returns>
    public static string GetExtension(AudioFormat format) => format switch
    {
        AudioFormat.M4A => ".m4a",
        AudioFormat.MP3 => ".mp3",
        _ => string.Empty,
    };

    /// <summary>
    /// Builds the output path next to the source file using the source base name and the target
    /// extension. When that file already exists, a numeric suffix (<c> (1)</c>, <c> (2)</c>, ...)
    /// is appended before the extension until a free name is found, so an existing file is never
    /// overwritten.
    /// </summary>
    /// <param name="sourcePath">Path of the source audio file.</param>
    /// <param name="targetFormat">Format the file will be converted to.</param>
    /// <returns>A full output path that does not exist yet.</returns>
    public static string BuildTargetPath(string sourcePath, AudioFormat targetFormat)
    {
        var directory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
        var baseName = Path.GetFileNameWithoutExtension(sourcePath);
        var extension = GetExtension(targetFormat);

        var candidate = Path.Combine(directory, baseName + extension);
        var suffix = 1;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(directory, $"{baseName} ({suffix}){extension}");
            suffix++;
        }

        return candidate;
    }
}
