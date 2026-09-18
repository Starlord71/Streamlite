using System;
using System.IO;

namespace MediaConverter.App.Services;

/// <summary>
/// Splits a filesystem path into the segment worth emphasizing (a file name or the last folder
/// segment) and its containing directory, so long paths can be rendered on two lines instead of
/// breaking in the middle of a word.
/// </summary>
/// <param name="Name">The file name, or the last folder segment for a directory path.</param>
/// <param name="Directory">The containing directory, or an empty string when the path has none.</param>
public readonly record struct PathDisplayInfo(string Name, string Directory)
{
    /// <summary>
    /// Builds the display parts for a file path.
    /// </summary>
    /// <param name="path">Path of the file.</param>
    /// <returns>The file name and its containing directory.</returns>
    public static PathDisplayInfo FromFile(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return new PathDisplayInfo(string.Empty, string.Empty);
        }

        return new PathDisplayInfo(
            Path.GetFileName(path),
            Path.GetDirectoryName(path) ?? string.Empty);
    }

    /// <summary>
    /// Builds the display parts for a directory path. A trailing directory separator is ignored so
    /// the last folder segment becomes the name.
    /// </summary>
    /// <param name="path">Path of the directory.</param>
    /// <returns>The last folder segment and its parent directory.</returns>
    public static PathDisplayInfo FromDirectory(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return new PathDisplayInfo(string.Empty, string.Empty);
        }

        var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var name = trimmed.Length == 0 ? string.Empty : Path.GetFileName(trimmed);
        if (name.Length == 0)
        {
            // The path is a root (a drive such as "C:\"): there is no folder segment to name, so
            // keep the root itself as the name.
            return new PathDisplayInfo(path, string.Empty);
        }

        return new PathDisplayInfo(name, Path.GetDirectoryName(trimmed) ?? string.Empty);
    }
}
