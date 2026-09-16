using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MediaConverter.Core.Services.Binaries;

/// <summary>
/// Compares version strings in a tolerant, numeric way. Handles dot-separated semver strings
/// (for example <c>9.0.1</c>) and date-based markers (for example <c>2025.09.26</c>) by
/// comparing the numeric runs present in each string, ignoring separators and text.
/// </summary>
public static class VersionComparer
{
    private static readonly Regex NumericRunsRegex = new(
        @"\d+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Compares two version strings and returns a value indicating their relative order.
    /// Empty or <see langword="null"/> versions are treated as older than any non-empty one.
    /// </summary>
    /// <param name="x">The first version string.</param>
    /// <param name="y">The second version string.</param>
    /// <returns>Less than zero when <paramref name="x"/> is older, zero when equal, greater than zero when newer.</returns>
    public static int Compare(string? x, string? y)
    {
        var xIsEmpty = string.IsNullOrWhiteSpace(x);
        var yIsEmpty = string.IsNullOrWhiteSpace(y);

        if (xIsEmpty && yIsEmpty)
        {
            return 0;
        }

        if (xIsEmpty)
        {
            return -1;
        }

        if (yIsEmpty)
        {
            return 1;
        }

        var xParts = GetNumericParts(x!);
        var yParts = GetNumericParts(y!);
        var maxCount = Math.Max(xParts.Length, yParts.Length);

        for (var i = 0; i < maxCount; i++)
        {
            var xPart = i < xParts.Length ? xParts[i] : 0;
            var yPart = i < yParts.Length ? yParts[i] : 0;
            if (xPart != yPart)
            {
                return xPart < yPart ? -1 : 1;
            }
        }

        return string.CompareOrdinal(x, y);
    }

    /// <summary>
    /// Determines whether the <paramref name="latest"/> version is newer than <paramref name="current"/>.
    /// An unknown (empty or <see langword="null"/>) current version is always considered older.
    /// </summary>
    /// <param name="latest">The candidate latest version.</param>
    /// <param name="current">The currently installed version.</param>
    /// <returns><see langword="true"/> when an update to <paramref name="latest"/> is needed.</returns>
    public static bool IsNewer(string? latest, string? current) => Compare(latest, current) > 0;

    /// <summary>
    /// Extracts the numeric runs of a version string, skipping separators and non-numeric text.
    /// </summary>
    /// <param name="value">The version string.</param>
    /// <returns>The numeric parts in order.</returns>
    private static long[] GetNumericParts(string value)
    {
        var parts = new List<long>();
        foreach (Match match in NumericRunsRegex.Matches(value))
        {
            if (long.TryParse(match.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var part))
            {
                parts.Add(part);
            }
        }

        return parts.ToArray();
    }
}