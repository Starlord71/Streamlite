namespace MediaConverter.Core.Models;

/// <summary>
/// Lifecycle stages of a long-running operation, reported through <see cref="ProgressInfo"/>.
/// Shared across all services so the UI can map a single set of stages to localized labels.
/// </summary>
public enum ProgressStage
{
    /// <summary>The operation is being initialized.</summary>
    Starting,

    /// <summary>Inputs are being inspected (probing the media or resolving the URL).</summary>
    Analyzing,

    /// <summary>Data is being downloaded (media or external binary).</summary>
    Downloading,

    /// <summary>Media is being transcoded or encoded.</summary>
    Converting,

    /// <summary>Downloaded binaries are being extracted or installed.</summary>
    Installing,

    /// <summary>The output is being finalized (moved, verified, cleaned up).</summary>
    Finalizing,

    /// <summary>The operation finished successfully.</summary>
    Completed,
}