using System;
using System.IO;

namespace MediaConverter.App.Services;

/// <summary>
/// Helpers for the video-to-audio screen: validating that a chosen file is a supported video
/// source. The MP3 output path is derived through <see cref="AudioFileSupport.BuildTargetPath"/>
/// so the non-overwriting naming rule is shared with the audio conversion screen.
/// </summary>
public static class VideoFileSupport
{
    /// <summary>
    /// Gets the only video input extension the video-to-audio feature accepts.
    /// </summary>
    public const string SupportedExtension = ".mp4";

    /// <summary>
    /// Determines whether a file path points to a video source the extractor can read.
    /// </summary>
    /// <param name="path">Path of the chosen file.</param>
    /// <returns><see langword="true"/> when the extension is a supported video extension.</returns>
    public static bool IsSupportedVideoSource(string path) =>
        string.Equals(Path.GetExtension(path), SupportedExtension, StringComparison.OrdinalIgnoreCase);
}
