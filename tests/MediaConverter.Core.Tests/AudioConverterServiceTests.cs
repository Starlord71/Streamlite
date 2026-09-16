using MediaConverter.Core.Models;
using MediaConverter.Core.Services;

namespace MediaConverter.Core.Tests;

/// <summary>
/// Tests the pure validation and error-code logic of <see cref="AudioConverterService"/>.
/// These tests never spawn ffmpeg, so they run on any machine.
/// </summary>
public class AudioConverterServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ConvertAsync_NullOrWhitespaceSource_ReturnsInvalidInput(string? sourcePath)
    {
        var service = new AudioConverterService();
        var result = await service.ConvertAsync(sourcePath!, "output.mp3", AudioFormat.MP3);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.InvalidInput, result.ErrorCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ConvertAsync_NullOrWhitespaceOutput_ReturnsInvalidInput(string? outputPath)
    {
        var service = new AudioConverterService();
        var result = await service.ConvertAsync("input.m4a", outputPath!, AudioFormat.MP3);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.InvalidInput, result.ErrorCode);
    }

    [Fact]
    public async Task ConvertAsync_AlreadyCancelled_ReturnsCancelled()
    {
        var service = new AudioConverterService();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await service.ConvertAsync("input.m4a", "output.mp3", AudioFormat.MP3, cancellationToken: cts.Token);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.Cancelled, result.ErrorCode);
    }

    [Fact]
    public async Task ConvertAsync_UndefinedTargetFormat_ReturnsUnsupportedFormat()
    {
        var service = new AudioConverterService();
        var result = await service.ConvertAsync("input.m4a", "output.mp3", (AudioFormat)999);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.UnsupportedFormat, result.ErrorCode);
    }

    [Fact]
    public async Task ConvertAsync_OutputExtensionMismatch_ReturnsInvalidInput()
    {
        var service = new AudioConverterService();
        var result = await service.ConvertAsync("input.mp3", "output.m4a", AudioFormat.MP3);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.InvalidInput, result.ErrorCode);
    }

    [Fact]
    public async Task ConvertAsync_NonexistentSource_ReturnsFileNotFound()
    {
        var service = new AudioConverterService();
        var missingSource = Path.Combine(Path.GetTempPath(), $"missing-source-{Guid.NewGuid():N}.m4a");

        var result = await service.ConvertAsync(missingSource, "output.mp3", AudioFormat.MP3);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.FileNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task ConvertAsync_OutputPathBlockedByFile_ReturnsOutputPathInvalid()
    {
        var workDir = TestHelpers.CreateTempDirectory();
        var source = Path.Combine(workDir, "input.m4a");
        File.WriteAllText(source, "dummy");
        var blocker = Path.Combine(workDir, "blocker.txt");
        File.WriteAllText(blocker, "blocking");

        var service = new AudioConverterService();

        var result = await service.ConvertAsync(source, Path.Combine(blocker, "output.mp3"), AudioFormat.MP3);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.OutputPathInvalid, result.ErrorCode);

        Directory.Delete(workDir, recursive: true);
    }

    [Fact]
    public async Task ConvertAsync_MissingBinary_ReturnsBinaryNotFound()
    {
        var workDir = TestHelpers.CreateTempDirectory();
        var source = Path.Combine(workDir, "input.m4a");
        File.WriteAllText(source, "dummy");
        var missingFfmpeg = Path.Combine(workDir, "does-not-exist", "ffmpeg.exe");
        var service = new AudioConverterService(missingFfmpeg);

        var result = await service.ConvertAsync(source, Path.Combine(workDir, "output.mp3"), AudioFormat.MP3);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.BinaryNotFound, result.ErrorCode);

        Directory.Delete(workDir, recursive: true);
    }
}