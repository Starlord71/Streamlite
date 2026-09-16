namespace MediaConverter.Core.Models;

/// <summary>
/// A snapshot of progress for a long-running operation, delivered via <see cref="IProgress{T}"/>.
/// </summary>
/// <param name="Percentage">Completion percentage in the 0-100 range.</param>
/// <param name="Stage">Current lifecycle stage of the operation.</param>
/// <param name="Detail">Optional machine-readable detail (for example a file or binary name). Never user-facing text.</param>
public sealed record ProgressInfo(double Percentage, ProgressStage Stage, string? Detail = null);