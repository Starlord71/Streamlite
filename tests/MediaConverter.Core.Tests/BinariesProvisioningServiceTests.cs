using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using MediaConverter.Core.Models;
using MediaConverter.Core.Services;
using MediaConverter.Core.Services.Binaries;

namespace MediaConverter.Core.Tests;

/// <summary>
/// Tests <see cref="BinariesProvisioningService"/> against a stub HTTP server so download,
/// installation, update and cancellation flows run deterministically without real network.
/// </summary>
public class BinariesProvisioningServiceTests
{
    private const string FfmpegLatestVersion = "9.0.1";
    private const string YtDlpLatestVersion = "2025.09.26";

    private static readonly byte[] FfmpegZipBytes = CreateFfmpegZip();
    private static readonly byte[] YtDlpExeBytes = CreateFakeBytes(200_000);

    [Fact]
    public async Task ProvisionAsync_WhenBinariesMissing_DownloadsAndInstallsBoth()
    {
        var workDir = TestHelpers.CreateTempDirectory();
        try
        {
            var (client, requestedUrls) = CreateWorkingServer();
            try
            {
                var service = new BinariesProvisioningService(workDir, client);
                var result = await service.ProvisionAsync();

                Assert.True(result.Succeeded, $"Expected success, got {result.ErrorCode}.");
                Assert.True(File.Exists(Path.Combine(workDir, "ffmpeg.exe")));
                Assert.True(File.Exists(Path.Combine(workDir, "yt-dlp.exe")));
                Assert.Equal(FfmpegLatestVersion, File.ReadAllText(Path.Combine(workDir, "ffmpeg.version")));
                Assert.Equal(YtDlpLatestVersion, File.ReadAllText(Path.Combine(workDir, "yt-dlp.version")));
                Assert.Contains(requestedUrls, url => url.StartsWith(BinarySources.GetFfmpegDownloadUrl().ToString(), StringComparison.Ordinal));
                Assert.Contains(requestedUrls, url => url.StartsWith(BinarySources.GetYtDlpDownloadUrl().ToString(), StringComparison.Ordinal));
                Assert.Empty(Directory.GetFiles(workDir, "*.part"));
            }
            finally
            {
                client.Dispose();
            }
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }

    [Fact]
    public async Task ProvisionAsync_WhenBinariesAlreadyPresent_DoesNotDownload()
    {
        var workDir = TestHelpers.CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(workDir, "ffmpeg.exe"), "existing ffmpeg");
            File.WriteAllText(Path.Combine(workDir, "yt-dlp.exe"), "existing yt-dlp");

            var (client, requestedUrls) = CreateWorkingServer();
            try
            {
                var service = new BinariesProvisioningService(workDir, client);
                var result = await service.ProvisionAsync();

                Assert.True(result.Succeeded);
                Assert.Equal("existing ffmpeg", File.ReadAllText(Path.Combine(workDir, "ffmpeg.exe")));
                Assert.Equal("existing yt-dlp", File.ReadAllText(Path.Combine(workDir, "yt-dlp.exe")));
                Assert.Empty(requestedUrls);
            }
            finally
            {
                client.Dispose();
            }
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }

    [Fact]
    public async Task ProvisionAsync_ReportsDownloadingInstallingCompletedStages()
    {
        var workDir = TestHelpers.CreateTempDirectory();
        try
        {
            var (client, _) = CreateWorkingServer();
            try
            {
                var service = new BinariesProvisioningService(workDir, client);
                var reports = new RecordingProgress();
                var result = await service.ProvisionAsync(reports);

                Assert.True(result.Succeeded, $"Expected success, got {result.ErrorCode}.");
                Assert.Contains(reports.Reports, r => r.Stage == ProgressStage.Downloading);
                Assert.Contains(reports.Reports, r => r.Stage == ProgressStage.Installing);
                Assert.Contains(reports.Reports, r => r.Stage == ProgressStage.Downloading && r.Percentage > 0 && r.Percentage < 100);
                var lastDownloading = reports.Reports.Last(r => r.Stage == ProgressStage.Downloading);
                Assert.Equal(100, lastDownloading.Percentage);
                var last = reports.Reports[^1];
                Assert.Equal(ProgressStage.Completed, last.Stage);
                Assert.Equal(100, last.Percentage);
            }
            finally
            {
                client.Dispose();
            }
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }

    [Fact]
    public async Task ProvisionAsync_CancellationMidDownload_ReturnsCancelledAndCleansPartialFile()
    {
        var workDir = TestHelpers.CreateTempDirectory();
        try
        {
            var slowPayload = CreateFakeBytes(1_000_000);
            var handler = new FakeHttpMessageHandler(request =>
            {
                var url = request.RequestUri!.ToString();
                if (url == BinarySources.GetFfmpegVersionUrl().ToString())
                {
                    return TextResponse(FfmpegLatestVersion);
                }

                if (url == BinarySources.GetFfmpegDownloadUrl().ToString())
                {
                    var content = new StreamContent(new SlowStream(slowPayload, 4096));
                    content.Headers.ContentLength = slowPayload.Length;
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });
            using var client = new HttpClient(handler);
            var service = new BinariesProvisioningService(workDir, client);
            using var cts = new CancellationTokenSource();

            var provisioning = service.ProvisionAsync(cancellationToken: cts.Token);
            await Task.Delay(150);
            cts.Cancel();
            var result = await provisioning;

            Assert.False(result.Succeeded);
            Assert.Equal(ErrorCode.Cancelled, result.ErrorCode);
            Assert.False(File.Exists(Path.Combine(workDir, "ffmpeg.exe")));
            Assert.Empty(Directory.GetFiles(workDir, "*.part"));
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }

    [Fact]
    public async Task ProvisionAsync_AlreadyCancelled_ReturnsCancelled()
    {
        var workDir = TestHelpers.CreateTempDirectory();
        try
        {
            var (client, _) = CreateWorkingServer();
            try
            {
                var service = new BinariesProvisioningService(workDir, client);
                using var cts = new CancellationTokenSource();
                cts.Cancel();

                var result = await service.ProvisionAsync(cancellationToken: cts.Token);

                Assert.False(result.Succeeded);
                Assert.Equal(ErrorCode.Cancelled, result.ErrorCode);
            }
            finally
            {
                client.Dispose();
            }
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }

    [Fact]
    public async Task ProvisionAsync_WhenVersionFetchFails_StillInstallsBinaryWithoutMarker()
    {
        var workDir = TestHelpers.CreateTempDirectory();
        try
        {
            var handler = new FakeHttpMessageHandler(request =>
            {
                var url = request.RequestUri!.ToString();
                if (url == BinarySources.GetFfmpegVersionUrl().ToString() ||
                    url == BinarySources.GetYtDlpVersionUrl().ToString())
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }

                if (url == BinarySources.GetFfmpegDownloadUrl().ToString())
                {
                    return BytesResponse(FfmpegZipBytes);
                }

                if (url == BinarySources.GetYtDlpDownloadUrl().ToString())
                {
                    return BytesResponse(YtDlpExeBytes);
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });
            using var client = new HttpClient(handler);
            var service = new BinariesProvisioningService(workDir, client);

            var result = await service.ProvisionAsync();

            Assert.True(result.Succeeded, $"Expected success, got {result.ErrorCode}.");
            Assert.True(File.Exists(Path.Combine(workDir, "ffmpeg.exe")));
            Assert.True(File.Exists(Path.Combine(workDir, "yt-dlp.exe")));
            Assert.False(File.Exists(Path.Combine(workDir, "ffmpeg.version")));
            Assert.False(File.Exists(Path.Combine(workDir, "yt-dlp.version")));
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }

    [Fact]
    public async Task ProvisionAsync_WhenDownloadFails_ReturnsNetworkError()
    {
        var workDir = TestHelpers.CreateTempDirectory();
        try
        {
            var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
            using var client = new HttpClient(handler);
            var service = new BinariesProvisioningService(workDir, client);

            var result = await service.ProvisionAsync();

            Assert.False(result.Succeeded);
            Assert.Equal(ErrorCode.NetworkError, result.ErrorCode);
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }

    [Fact]
    public async Task ProvisionAsync_WhenArchiveLacksExecutable_ReturnsProvisioningFailed()
    {
        var workDir = TestHelpers.CreateTempDirectory();
        try
        {
            var badZip = CreateZipWithoutExecutable();
            var handler = new FakeHttpMessageHandler(request =>
            {
                var url = request.RequestUri!.ToString();
                if (url == BinarySources.GetFfmpegVersionUrl().ToString())
                {
                    return TextResponse(FfmpegLatestVersion);
                }

                if (url == BinarySources.GetFfmpegDownloadUrl().ToString())
                {
                    return BytesResponse(badZip);
                }

                if (url == BinarySources.GetYtDlpVersionUrl().ToString())
                {
                    return JsonResponse(new { tag_name = YtDlpLatestVersion });
                }

                if (url == BinarySources.GetYtDlpDownloadUrl().ToString())
                {
                    return BytesResponse(YtDlpExeBytes);
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });
            using var client = new HttpClient(handler);
            var service = new BinariesProvisioningService(workDir, client);

            var result = await service.ProvisionAsync();

            Assert.False(result.Succeeded);
            Assert.Equal(ErrorCode.ProvisioningFailed, result.ErrorCode);
            Assert.Empty(Directory.GetFiles(workDir, "*.part"));
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }

    [Fact]
    public async Task UpdateAsync_WhenLocalVersionMatchesLatest_DoesNotDownload()
    {
        var workDir = TestHelpers.CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(workDir, "ffmpeg.exe"), "dummy ffmpeg");
            File.WriteAllText(Path.Combine(workDir, "ffmpeg.version"), FfmpegLatestVersion);
            File.WriteAllText(Path.Combine(workDir, "yt-dlp.exe"), "dummy yt-dlp");
            File.WriteAllText(Path.Combine(workDir, "yt-dlp.version"), YtDlpLatestVersion);

            var (client, requestedUrls) = CreateWorkingServer();
            try
            {
                var service = new BinariesProvisioningService(workDir, client);
                var result = await service.UpdateAsync();

                Assert.True(result.Succeeded, $"Expected success, got {result.ErrorCode}.");
                Assert.Equal("dummy ffmpeg", File.ReadAllText(Path.Combine(workDir, "ffmpeg.exe")));
                Assert.Equal("dummy yt-dlp", File.ReadAllText(Path.Combine(workDir, "yt-dlp.exe")));
                Assert.DoesNotContain(requestedUrls, url => url.StartsWith(BinarySources.GetFfmpegDownloadUrl().ToString(), StringComparison.Ordinal));
                Assert.DoesNotContain(requestedUrls, url => url.StartsWith(BinarySources.GetYtDlpDownloadUrl().ToString(), StringComparison.Ordinal));
            }
            finally
            {
                client.Dispose();
            }
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }

    [Fact]
    public async Task UpdateAsync_WhenLocalVersionOutdated_ReplacesBinaryAndMarker()
    {
        var workDir = TestHelpers.CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(workDir, "ffmpeg.exe"), "old ffmpeg");
            File.WriteAllText(Path.Combine(workDir, "ffmpeg.version"), "7.0");
            File.WriteAllText(Path.Combine(workDir, "yt-dlp.exe"), "old yt-dlp");
            File.WriteAllText(Path.Combine(workDir, "yt-dlp.version"), "2025.01.01");

            var (client, _) = CreateWorkingServer();
            try
            {
                var service = new BinariesProvisioningService(workDir, client);
                var result = await service.UpdateAsync();

                Assert.True(result.Succeeded, $"Expected success, got {result.ErrorCode}.");
                Assert.Equal(FfmpegLatestVersion, File.ReadAllText(Path.Combine(workDir, "ffmpeg.version")));
                Assert.Equal(YtDlpLatestVersion, File.ReadAllText(Path.Combine(workDir, "yt-dlp.version")));
                Assert.Equal(YtDlpExeBytes.Length, new FileInfo(Path.Combine(workDir, "yt-dlp.exe")).Length);
                Assert.Equal("fake ffmpeg executable", File.ReadAllText(Path.Combine(workDir, "ffmpeg.exe")));
            }
            finally
            {
                client.Dispose();
            }
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }

    [Fact]
    public async Task UpdateAsync_WhenMarkerMissing_LeavesBinaryUntouched()
    {
        var workDir = TestHelpers.CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(workDir, "yt-dlp.exe"), "existing yt-dlp");

            var (client, requestedUrls) = CreateWorkingServer();
            try
            {
                var service = new BinariesProvisioningService(workDir, client);
                var result = await service.UpdateAsync();

                Assert.True(result.Succeeded);
                Assert.Equal("existing yt-dlp", File.ReadAllText(Path.Combine(workDir, "yt-dlp.exe")));
                Assert.DoesNotContain(requestedUrls, url => url.StartsWith(BinarySources.GetYtDlpDownloadUrl().ToString(), StringComparison.Ordinal));
            }
            finally
            {
                client.Dispose();
            }
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }

    [Fact]
    public async Task UpdateAsync_WhenBinaryMissing_ProvisionsIt()
    {
        var workDir = TestHelpers.CreateTempDirectory();
        try
        {
            var (client, _) = CreateWorkingServer();
            try
            {
                var service = new BinariesProvisioningService(workDir, client);
                var result = await service.UpdateAsync();

                Assert.True(result.Succeeded, $"Expected success, got {result.ErrorCode}.");
                Assert.True(File.Exists(Path.Combine(workDir, "ffmpeg.exe")));
                Assert.True(File.Exists(Path.Combine(workDir, "yt-dlp.exe")));
                Assert.True(File.Exists(Path.Combine(workDir, "ffmpeg.version")));
                Assert.True(File.Exists(Path.Combine(workDir, "yt-dlp.version")));
            }
            finally
            {
                client.Dispose();
            }
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }

    [Fact]
    public async Task UpdateAsync_WhenVersionCheckFails_ReturnsNetworkError()
    {
        var workDir = TestHelpers.CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(workDir, "ffmpeg.exe"), "dummy ffmpeg");
            File.WriteAllText(Path.Combine(workDir, "ffmpeg.version"), FfmpegLatestVersion);
            File.WriteAllText(Path.Combine(workDir, "yt-dlp.exe"), "dummy yt-dlp");
            File.WriteAllText(Path.Combine(workDir, "yt-dlp.version"), YtDlpLatestVersion);

            var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
            using var client = new HttpClient(handler);
            var service = new BinariesProvisioningService(workDir, client);

            var result = await service.UpdateAsync();

            Assert.False(result.Succeeded);
            Assert.Equal(ErrorCode.NetworkError, result.ErrorCode);
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }

    [Fact]
    public async Task UpdateAsync_AlreadyCancelled_ReturnsCancelled()
    {
        var workDir = TestHelpers.CreateTempDirectory();
        try
        {
            var (client, _) = CreateWorkingServer();
            try
            {
                var service = new BinariesProvisioningService(workDir, client);
                using var cts = new CancellationTokenSource();
                cts.Cancel();

                var result = await service.UpdateAsync(cancellationToken: cts.Token);

                Assert.False(result.Succeeded);
                Assert.Equal(ErrorCode.Cancelled, result.ErrorCode);
            }
            finally
            {
                client.Dispose();
            }
        }
        finally
        {
            Directory.Delete(workDir, recursive: true);
        }
    }

    private static (HttpClient Client, List<string> RequestedUrls) CreateWorkingServer()
    {
        var requestedUrls = new List<string>();
        var handler = new FakeHttpMessageHandler(request =>
        {
            requestedUrls.Add(request.RequestUri!.ToString());
            var url = request.RequestUri!.ToString();

            if (url == BinarySources.GetYtDlpVersionUrl().ToString())
            {
                return JsonResponse(new { tag_name = YtDlpLatestVersion });
            }

            if (url == BinarySources.GetFfmpegVersionUrl().ToString())
            {
                return TextResponse(FfmpegLatestVersion);
            }

            if (url == BinarySources.GetYtDlpDownloadUrl().ToString())
            {
                return BytesResponse(YtDlpExeBytes);
            }

            if (url == BinarySources.GetFfmpegDownloadUrl().ToString())
            {
                return BytesResponse(FfmpegZipBytes);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        return (new HttpClient(handler), requestedUrls);
    }

    private static HttpResponseMessage JsonResponse(object value)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(value)),
        };
    }

    private static HttpResponseMessage TextResponse(string text)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(text),
        };
    }

    private static HttpResponseMessage BytesResponse(byte[] bytes)
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentLength = bytes.Length;
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = content,
        };
    }

    private static byte[] CreateFfmpegZip()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("ffmpeg-9.0.1-essentials_build/bin/ffmpeg.exe");
            using var entryStream = entry.Open();
            entryStream.Write(Encoding.UTF8.GetBytes("fake ffmpeg executable"));
        }

        return stream.ToArray();
    }

    private static byte[] CreateZipWithoutExecutable()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("readme.txt");
            using var entryStream = entry.Open();
            entryStream.Write(Encoding.UTF8.GetBytes("no executable here"));
        }

        return stream.ToArray();
    }

    private static byte[] CreateFakeBytes(int count)
    {
        var bytes = new byte[count];
        new Random(1234).NextBytes(bytes);
        return bytes;
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }

    private sealed class RecordingProgress : IProgress<ProgressInfo>
    {
        public List<ProgressInfo> Reports { get; } = new();

        public void Report(ProgressInfo value) => Reports.Add(value);
    }

    private sealed class SlowStream : Stream
    {
        private readonly byte[] _data;
        private readonly int _chunkSize;
        private int _position;

        public SlowStream(byte[] data, int chunkSize)
        {
            _data = data;
            _chunkSize = chunkSize;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _data.Length;

        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(50, cancellationToken);
            return CopyChunk(buffer.Span);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Thread.Sleep(50);
            return CopyChunk(buffer.AsSpan(offset, count));
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        private int CopyChunk(Span<byte> destination)
        {
            if (_position >= _data.Length)
            {
                return 0;
            }

            var count = Math.Min(_chunkSize, _data.Length - _position);
            _data.AsSpan(_position, count).CopyTo(destination);
            _position += count;
            return count;
        }
    }
}