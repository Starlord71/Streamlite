using System.Diagnostics;
using MediaConverter.Core.Models;

namespace MediaConverter.Core.Tests;

/// <summary>
/// Shared helpers for tests that optionally exercise the real ffmpeg binary.
/// </summary>
internal static class TestHelpers
{
    private const string FfmpegEnvironmentVariable = "MEDIACONVERTER_FFMPEG";
    private const string YtDlpEnvironmentVariable = "MEDIACONVERTER_YTDLP";

    /// <summary>
    /// Locates a usable ffmpeg binary, either from the <c>MEDIACONVERTER_FFMPEG</c>
    /// environment variable or next to the test runner, or returns <see langword="null"/>
    /// when none is available.
    /// </summary>
    /// <returns>The full path to ffmpeg, or <see langword="null"/> if it cannot be found.</returns>
    internal static string? LocateFfmpeg() => LocateBinary(FfmpegEnvironmentVariable, "ffmpeg");

    /// <summary>
    /// Locates a usable yt-dlp binary, either from the <c>MEDIACONVERTER_YTDLP</c>
    /// environment variable or next to the test runner, or returns <see langword="null"/>
    /// when none is available.
    /// </summary>
    /// <returns>The full path to yt-dlp, or <see langword="null"/> if it cannot be found.</returns>
    internal static string? LocateYtDlp() => LocateBinary(YtDlpEnvironmentVariable, "yt-dlp");

    /// <summary>
    /// Locates a binary either from the given environment variable or next to the test runner.
    /// </summary>
    /// <param name="environmentVariable">Name of the environment variable that may override the location.</param>
    /// <param name="binaryName">Platform-independent binary name without extension.</param>
    /// <returns>The full path to the binary, or <see langword="null"/> if it cannot be found.</returns>
    private static string? LocateBinary(string environmentVariable, string binaryName)
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(environmentVariable);
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

            var candidate = Path.Combine(directory, OperatingSystem.IsWindows() ? binaryName + ".exe" : binaryName);
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

    /// <summary>
    /// Generates an MP4 video fixture (with an audio track) using ffmpeg's lavfi sources,
    /// asserting that generation succeeds.
    /// </summary>
    /// <param name="ffmpegPath">Path to the ffmpeg executable.</param>
    /// <param name="outputPath">Path where the generated MP4 file will be written.</param>
    /// <param name="lavfiAudioSource">The lavfi audio source used as the second input.</param>
    internal static async Task GenerateVideoAsync(string ffmpegPath, string outputPath, string lavfiAudioSource)
    {
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
        startInfo.ArgumentList.Add("testsrc=size=320x240:rate=15:duration=2");
        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add("lavfi");
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(lavfiAudioSource);
        startInfo.ArgumentList.Add("-c:v");
        startInfo.ArgumentList.Add("mpeg4");
        startInfo.ArgumentList.Add("-pix_fmt");
        startInfo.ArgumentList.Add("yuv420p");
        startInfo.ArgumentList.Add("-c:a");
        startInfo.ArgumentList.Add("aac");
        startInfo.ArgumentList.Add("-shortest");
        startInfo.ArgumentList.Add(outputPath);

        using var process = Process.Start(startInfo);
        Assert.NotNull(process);
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        Assert.True(process.ExitCode == 0, $"Video fixture generation failed: {stderr}");
    }
}