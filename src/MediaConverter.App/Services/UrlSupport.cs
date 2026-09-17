using System;

namespace MediaConverter.App.Services;

/// <summary>
/// Pure validation helpers for the video download screen. They let the UI reject an empty or
/// malformed URL before calling Core, so the user gets immediate feedback instead of waiting for a
/// failed operation. The type has no UI dependency and is safe to unit test.
/// </summary>
public static class UrlSupport
{
    /// <summary>
    /// Determines whether a string is a well-formed absolute HTTP or HTTPS URL.
    /// </summary>
    /// <param name="url">The candidate URL typed by the user.</param>
    /// <returns>
    /// <see langword="true"/> when the value is a non-empty absolute <c>http</c> or <c>https</c>
    /// URL with a host; otherwise <see langword="false"/>.
    /// </returns>
    public static bool IsWellFormedVideoUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        var isHttp = uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        return isHttp && !string.IsNullOrEmpty(uri.Host);
    }
}
