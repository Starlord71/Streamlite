using MediaConverter.Core.Models;
using MediaConverter.Core.Services;

namespace MediaConverter.Core.Tests;

/// <summary>
/// End-to-end conversion tests that require the real ffmpeg binary. Each test is
/// skipped (returns without asserting) when ffmpeg cannot be located, so the suite
/// never fails on machines without the binary.
/// </summary>
public class AudioConverterIntegrationTests
{
    private static readonly string? FfmpegPath = TestHelpers.LocateFfmpeg();

    [Fact]
    public async Task ConvertAsync_RealFfmpeg_ConvertsMp3ToM4a()
    {
        var ffmpegPath = FfmpegPath;
        if (ffmpegPath is null)
        {
            return;
        }

        var workDir = TestHelpers.CreateTempDirectory();
        try
        {
            var source = Path.Combine(workDir, "input.mp3");
            await TestHelpers.GenerateAudioAsync(ffmpegPath, source, AudioFormat.MP3, "sine=frequency=440:duration=2");

            var output = Path.Combine(workDir, "nested", "output.m4a");
            var service = new AudioConverterService(ffmpegPath);
            var result = await service.ConvertAsync(source, output, AudioFormat.M4A);

            Assert.True(result.Succeeded, $"Expected success, got {result.ErrorCode}.");
            Assert.True(File.Exists(output), "Converted file was not created.");
            Assert.True(new FileInfo(output).Length > 0, "Converted file is empty.");
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }

    [Fact]
    public async Task ConvertAsync_RealFfmpeg_ConvertsM4aToMp3()
    {
        var ffmpegPath = FfmpegPath;
        if (ffmpegPath is null)
        {
            return;
        }

        var workDir = TestHelpers.CreateTempDirectory();
        try
        {
            var source = Path.Combine(workDir, "input.m4a");
            await TestHelpers.GenerateAudioAsync(ffmpegPath, source, AudioFormat.M4A, "sine=frequency=440:duration=2");

            var output = Path.Combine(workDir, "output.mp3");
            var service = new AudioConverterService(ffmpegPath);
            var result = await service.ConvertAsync(source, output, AudioFormat.MP3);

            Assert.True(result.Succeeded, $"Expected success, got {result.ErrorCode}.");
            Assert.True(File.Exists(output), "Converted file was not created.");
            Assert.True(new FileInfo(output).Length > 0, "Converted file is empty.");
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }

    [Fact]
    public async Task ConvertAsync_RealFfmpeg_ReportsCompletedProgress()
    {
        var ffmpegPath = FfmpegPath;
        if (ffmpegPath is null)
        {
            return;
        }

        var workDir = TestHelpers.CreateTempDirectory();
        try
        {
            var source = Path.Combine(workDir, "input.mp3");
            await TestHelpers.GenerateAudioAsync(ffmpegPath, source, AudioFormat.MP3, "sine=frequency=440:duration=2");

            var output = Path.Combine(workDir, "output.m4a");
            var reports = new List<ProgressInfo>();
            var progress = new Progress<ProgressInfo>(reports.Add);
            var service = new AudioConverterService(ffmpegPath);
            var result = await service.ConvertAsync(source, output, AudioFormat.M4A, progress);

            Assert.True(result.Succeeded, $"Expected success, got {result.ErrorCode}.");
            Assert.NotEmpty(reports);
            Assert.Contains(reports, r => r.Stage == ProgressStage.Converting);
            var last = reports[^1];
            Assert.Equal(ProgressStage.Completed, last.Stage);
            Assert.Equal(100, last.Percentage);
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }

    [Fact]
    public async Task ConvertAsync_RealFfmpeg_CancellationMidEncoding_ReturnsCancelled()
    {
        var ffmpegPath = FfmpegPath;
        if (ffmpegPath is null)
        {
            return;
        }

        var workDir = TestHelpers.CreateTempDirectory();
        try
        {
            var source = Path.Combine(workDir, "input.mp3");
            await TestHelpers.GenerateAudioAsync(
                ffmpegPath,
                source,
                AudioFormat.MP3,
                "anoisesrc=color=pink:duration=300:amplitude=0.3");

            var output = Path.Combine(workDir, "output.m4a");
            var service = new AudioConverterService(ffmpegPath);
            using var cts = new CancellationTokenSource();

            var conversion = service.ConvertAsync(source, output, AudioFormat.M4A, cancellationToken: cts.Token);
            await Task.Delay(300);
            cts.Cancel();
            var result = await conversion;

            Assert.False(result.Succeeded);
            Assert.Equal(ErrorCode.Cancelled, result.ErrorCode);
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }

    [Fact]
    public async Task ConvertAsync_RealFfmpeg_ExtractsMp3FromMp4()
    {
        var ffmpegPath = FfmpegPath;
        if (ffmpegPath is null)
        {
            return;
        }

        var workDir = TestHelpers.CreateTempDirectory();
        try
        {
            var source = Path.Combine(workDir, "input.mp4");
            await TestHelpers.GenerateVideoAsync(ffmpegPath, source, "sine=frequency=440:duration=2");

            var output = Path.Combine(workDir, "output.mp3");
            var service = new AudioConverterService(ffmpegPath);
            var result = await service.ConvertAsync(source, output, AudioFormat.MP3);

            Assert.True(result.Succeeded, $"Expected success, got {result.ErrorCode}.");
            Assert.True(File.Exists(output), "Extracted MP3 was not created.");
            Assert.True(new FileInfo(output).Length > 0, "Extracted MP3 is empty.");
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }
}