using MediaConverter.Core.Interfaces;

namespace MediaConverter.Core.Models;

/// <summary>
/// Audio formats supported by <see cref="IAudioConverterService"/>.
/// </summary>
public enum AudioFormat
{
    /// <summary>MPEG-4 Audio (AAC).</summary>
    M4A,

    /// <summary>MPEG-1/2 Audio Layer 3.</summary>
    MP3,
}