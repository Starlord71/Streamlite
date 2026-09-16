using System;
using System.Text.Json;

namespace MediaConverter.Core.Services.Binaries;

/// <summary>
/// Extracts the <c>tag_name</c> of the latest release from a GitHub releases JSON document.
/// </summary>
public static class GitHubReleaseParser
{
    /// <summary>
    /// Attempts to read the <c>tag_name</c> property from a GitHub latest-release JSON document.
    /// </summary>
    /// <param name="json">The raw JSON document.</param>
    /// <param name="version">Receives the release tag, or <see langword="null"/> when it could not be read.</param>
    /// <returns><see langword="true"/> when a non-empty version was extracted.</returns>
    public static bool TryGetLatestVersion(string? json, out string? version)
    {
        version = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("tag_name", out var tagElement) ||
                tagElement.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            version = tagElement.GetString();
            return !string.IsNullOrWhiteSpace(version);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}