using System;
using MediaConverter.Core.Models;

namespace MediaConverter.Core.Services;

/// <summary>
/// Maps the standard error text produced by a failed yt-dlp run to the closest
/// <see cref="ErrorCode"/>, so callers can distinguish an unsupported URL, a network
/// failure and any other download error.
/// </summary>
public static class YtDlpErrorClassifier
{
    private static readonly string[] InvalidUrlMarkers =
    {
        "unsupported url",
        "is not a valid url",
    };

    private static readonly string[] NetworkMarkers =
    {
        "unable to download",
        "http error",
        "connection",
        "timed out",
        "timeout",
        "temporary failure",
        "getaddrinfo",
        "network is unreachable",
        "remote end closed",
        "ssl",
        "certificate",
    };

    /// <summary>
    /// Classifies the captured output of a failed yt-dlp invocation.
    /// </summary>
    /// <param name="output">
    /// The standard error text captured from yt-dlp, or <see langword="null"/> or empty when nothing was captured.
    /// </param>
    /// <returns>
    /// <see cref="ErrorCode.InvalidUrl"/> for unsupported or malformed URLs,
    /// <see cref="ErrorCode.NetworkError"/> for connectivity failures, and
    /// <see cref="ErrorCode.DownloadFailed"/> otherwise.
    /// </returns>
    public static ErrorCode Classify(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return ErrorCode.DownloadFailed;
        }

        var text = output.ToLowerInvariant();

        if (ContainsAny(text, InvalidUrlMarkers))
        {
            return ErrorCode.InvalidUrl;
        }

        if (ContainsAny(text, NetworkMarkers))
        {
            return ErrorCode.NetworkError;
        }

        return ErrorCode.DownloadFailed;
    }

    /// <summary>
    /// Determines whether the lower-cased <paramref name="text"/> contains any of the markers.
    /// </summary>
    /// <param name="text">The lower-cased output to scan.</param>
    /// <param name="markers">The markers to look for.</param>
    /// <returns><see langword="true"/> when at least one marker is present.</returns>
    private static bool ContainsAny(string text, string[] markers)
    {
        foreach (var marker in markers)
        {
            if (text.Contains(marker, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
