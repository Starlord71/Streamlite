using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaConverter.Core.Models;

namespace MediaConverter.Core.Services.Binaries;

/// <summary>
/// Performs the raw HTTP work for binary provisioning: downloading bytes to a partial
/// file with progress reporting, and resolving the latest version marker from the binary's
/// source. Not thread-safe; one instance is expected per binary.
/// </summary>
internal sealed class BinaryDownloader
{
    private const int BufferSize = 81920;

    private readonly HttpClient _httpClient;
    private readonly IProgress<ProgressInfo>? _progress;
    private readonly string _detail;

    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryDownloader"/> class.
    /// </summary>
    /// <param name="httpClient">The client used for all requests.</param>
    /// <param name="progress">Optional receiver of download progress updates.</param>
    /// <param name="detail">Machine-readable detail (the binary name) attached to progress updates.</param>
    public BinaryDownloader(HttpClient httpClient, IProgress<ProgressInfo>? progress, string detail)
    {
        _httpClient = httpClient;
        _progress = progress;
        _detail = detail;
    }

    /// <summary>
    /// Fetches the latest version marker for a binary from its source.
    /// </summary>
    /// <param name="binary">The binary whose version marker is fetched.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The latest version string, or <see langword="null"/> when the source returned empty content.</returns>
    /// <exception cref="HttpRequestException">The source responded with a non-success status code.</exception>
    /// <exception cref="JsonException">A GitHub release response did not contain a usable <c>tag_name</c>.</exception>
    public async Task<string?> FetchLatestVersionAsync(BinaryDefinition binary, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(binary.VersionUrl);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Version request for '{binary.Name}' failed with HTTP status {(int)response.StatusCode}.");
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!binary.UsesGitHubJson)
        {
            return string.IsNullOrWhiteSpace(content) ? null : content.Trim();
        }

        if (!GitHubReleaseParser.TryGetLatestVersion(content, out var version))
        {
            throw new JsonException($"GitHub release response for '{binary.Name}' did not contain a valid tag_name.");
        }

        return version;
    }

    /// <summary>
    /// Downloads the resource at <paramref name="url"/> into a partial file next to
    /// <paramref name="destinationPath"/>, reporting byte-based progress percentages.
    /// </summary>
    /// <param name="url">The URL to download.</param>
    /// <param name="destinationPath">Final path the file will be installed to; the download is written to <c>destinationPath + ".part"</c>.</param>
    /// <param name="cancellationToken">Token used to cancel the download.</param>
    /// <returns>The path of the downloaded partial file.</returns>
    public async Task<string> DownloadAsync(Uri url, string destinationPath, CancellationToken cancellationToken)
    {
        var partPath = destinationPath + ".part";
        using var request = CreateRequest(url);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Download of '{url}' failed with HTTP status {(int)response.StatusCode}.");
        }

        var totalBytes = response.Content.Headers.ContentLength;
        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        try
        {
            await using var fileStream = new FileStream(
                partPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                BufferSize,
                useAsync: true);

            var buffer = new byte[BufferSize];
            long totalRead = 0;
            while (true)
            {
                var read = await contentStream.ReadAsync(buffer.AsMemory(), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                totalRead += read;
                ReportDownloadProgress(totalBytes, totalRead);
            }

            await fileStream.FlushAsync(cancellationToken);
            ReportDownloadProgress(totalBytes, totalRead);
        }
        catch
        {
            TryDeleteFile(partPath);
            throw;
        }

        return partPath;
    }

    /// <summary>
    /// Removes a partial download file, ignoring failures so cleanup never masks the original error.
    /// </summary>
    private static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    /// <summary>
    /// Reports the current download percentage, computed as bytes read over the response's
    /// Content-Length. Reports 0 when the server did not provide a length.
    /// </summary>
    private void ReportDownloadProgress(long? totalBytes, long totalRead)
    {
        var percentage = totalBytes.HasValue && totalBytes.Value > 0
            ? Math.Clamp((double)totalRead / totalBytes.Value * 100, 0, 100)
            : 0;
        _progress?.Report(new ProgressInfo(percentage, ProgressStage.Downloading, _detail));
    }

    private static HttpRequestMessage CreateRequest(Uri url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("MediaConverter", "1.0"));
        return request;
    }
}