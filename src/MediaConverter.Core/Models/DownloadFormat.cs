namespace MediaConverter.Core.Models;

/// <summary>
/// Output formats selectable when downloading media from a video URL.
/// </summary>
public enum DownloadFormat
{
    /// <summary>Video with audio in an MP4 container.</summary>
    MP4,

    /// <summary>Audio only, encoded as MP3.</summary>
    MP3,

    /// <summary>Audio only, encoded as AAC in an M4A container.</summary>
    M4A,
}