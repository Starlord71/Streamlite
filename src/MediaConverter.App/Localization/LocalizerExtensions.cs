using MediaConverter.Core.Models;
using Microsoft.Extensions.Localization;

namespace MediaConverter.App.Localization;

/// <summary>
/// Extension methods that translate Core's machine-readable enums into localized, user-facing
/// text. They live in the App layer (never in Core) so every component shares a single mapping
/// instead of repeating the same switch statements.
/// </summary>
public static class LocalizerExtensions
{
    /// <summary>
    /// Returns the localized label for a progress <paramref name="stage"/>.
    /// </summary>
    /// <param name="localizer">The string localizer used to resolve the resource.</param>
    /// <param name="stage">The stage to describe, or <see langword="null"/> for the generic working label.</param>
    /// <returns>The localized stage label.</returns>
    public static string DescribeStage(this IStringLocalizer localizer, ProgressStage? stage) => stage switch
    {
        ProgressStage.Starting => localizer["StageStarting"],
        ProgressStage.Analyzing => localizer["StageAnalyzing"],
        ProgressStage.Downloading => localizer["StageDownloading"],
        ProgressStage.Converting => localizer["StageConverting"],
        ProgressStage.Installing => localizer["StageInstalling"],
        ProgressStage.Finalizing => localizer["StageFinalizing"],
        ProgressStage.Completed => localizer["StageCompleted"],
        _ => localizer["StageWorking"],
    };

    /// <summary>
    /// Returns the localized message for an <paramref name="errorCode"/>. Undefined numeric values
    /// fall back to a generic message that includes the raw code.
    /// </summary>
    /// <param name="localizer">The string localizer used to resolve the resource.</param>
    /// <param name="errorCode">The error code returned by a Core operation.</param>
    /// <returns>The localized error message.</returns>
    public static string DescribeErrorCode(this IStringLocalizer localizer, ErrorCode errorCode) => errorCode switch
    {
        ErrorCode.Cancelled => localizer["ErrorCancelled"],
        ErrorCode.InvalidInput => localizer["ErrorInvalidInput"],
        ErrorCode.FileNotFound => localizer["ErrorFileNotFound"],
        ErrorCode.OutputPathInvalid => localizer["ErrorOutputPathInvalid"],
        ErrorCode.UnsupportedFormat => localizer["ErrorUnsupportedFormat"],
        ErrorCode.InvalidUrl => localizer["ErrorInvalidUrl"],
        ErrorCode.NetworkError => localizer["ErrorNetworkError"],
        ErrorCode.AccessDenied => localizer["ErrorAccessDenied"],
        ErrorCode.InsufficientDiskSpace => localizer["ErrorInsufficientDiskSpace"],
        ErrorCode.BinaryNotFound => localizer["ErrorBinaryNotFound"],
        ErrorCode.BinaryDownloadFailed => localizer["ErrorBinaryDownloadFailed"],
        ErrorCode.ProvisioningFailed => localizer["ErrorProvisioningFailed"],
        ErrorCode.ConversionFailed => localizer["ErrorConversionFailed"],
        ErrorCode.DownloadFailed => localizer["ErrorDownloadFailed"],
        ErrorCode.Unknown => localizer["ErrorUnknown"],
        _ => localizer["ErrorFallback", errorCode],
    };
}
