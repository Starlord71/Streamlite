using System.Diagnostics;
using MediaConverter.Core.Models;

namespace MediaConverter.Core.Tests;

/// <summary>
/// Shared helpers for tests that optionally exercise the real ffmpeg binary.
/// </summary>
internal static class TestHelpers
{
    private const string FfmpegEnvironmentVariable = "MEDIACONVERTER_FFMPEG";

    /// <summary>
    /// Locates a usable ffmpeg binary, either from the <c>MEDIACONVERTER_FFMPEG</c>
    /// environment variable or next to the test runner, or returns <see langword="null"/>
    /// when none is available.
    /// </summary>
    /// <returns>The full path to ffmpeg, or <see langword="null"/> if it cannot be found.</returns>
    internal static string? LocateFfmpeg()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(FfmpegEnvironmentVariable);
        if (!string.IsNullOrEmpty(fromEnvironment) && File.Exists(fromEnvironment))
        {
            return fromEnvironment;
        }

        var directories = new[]
        {
            !string.IsNullOrEmpty(Environment.ProcessPath) ? Path.GetDirectoryName(Environment.ProcessPath) : null,
            AppContext.BaseDirectory,
        };

        foreach (var directory in directories)
        {
            if (directory is null)
            {
                continue;
            }

            var candidate = Path.Combine(directory, OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>Creates a unique temporary directory for a test.</summary>
    /// <returns>The path of the created directory.</returns>
    internal static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"mediaconverter-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    /// <summary>
    /// Generates an audio fixture file with ffmpeg's lavfi source, asserting that generation succeeds.
    /// </summary>
    /// <param name="ffmpegPath">Path to the ffmpeg executable.</param>
    /// <param name="outputPath">Path where the generated file will be written.</param>
    /// <param name="format">Format of the generated file.</param>
    /// <param name="lavfiSource">The lavfi filter graph used as input.</param>
    internal static async Task GenerateAudioAsync(string ffmpegPath, string outputPath, AudioFormat format, string lavfiSource)
    {
        var codec = format == AudioFormat.M4A ? "aac" : "libmp3lame";
        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add("-y");
        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add("lavfi");
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(lavfiSource);
        startInfo.ArgumentList.Add("-c:a");
        startInfo.ArgumentList.Add(codec);
        startInfo.ArgumentList.Add(outputPath);

        using var process = Process.Start(startInfo);
        Assert.NotNull(process);
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        Assert.True(process.ExitCode == 0, $"Fixture generation failed: {stderr}");
    }
}