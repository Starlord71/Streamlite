# Core components

Class view of MediaConverter.Core. Core references no WPF or Blazor types, so it can be tested in
isolation and reused from any host.

```mermaid
classDiagram
    direction TB

    class IAudioConverterService {
        <<interface>>
        +ConvertAsync(sourcePath, outputPath, targetFormat, progress, cancellationToken) OperationResult
    }
    class IVideoDownloaderService {
        <<interface>>
        +DownloadAsync(url, outputDirectory, format, progress, cancellationToken) OperationResult
    }
    class IBinariesProvisioningService {
        <<interface>>
        +ProvisionAsync(progress, cancellationToken) OperationResult
        +UpdateAsync(progress, cancellationToken) OperationResult
    }

    class AudioConverterService
    class VideoDownloaderService
    class BinariesProvisioningService

    IAudioConverterService <|.. AudioConverterService
    IVideoDownloaderService <|.. VideoDownloaderService
    IBinariesProvisioningService <|.. BinariesProvisioningService

    class OperationResult {
        +bool Succeeded
        +ErrorCode ErrorCode
        +Ok() OperationResult
        +Fail(code) OperationResult
    }
    class ProgressInfo {
        +double Percentage
        +ProgressStage Stage
        +string Detail
    }
    class ErrorCode {
        <<enumeration>>
        Unknown
        Cancelled
        InvalidInput
        FileNotFound
        OutputPathInvalid
        UnsupportedFormat
        InvalidUrl
        NetworkError
        AccessDenied
        InsufficientDiskSpace
        BinaryNotFound
        BinaryDownloadFailed
        ProvisioningFailed
        ConversionFailed
        DownloadFailed
    }
    class AudioFormat {
        <<enumeration>>
        M4A
        MP3
    }
    class DownloadFormat {
        <<enumeration>>
        MP4
        MP3
        M4A
    }
    class ProgressStage {
        <<enumeration>>
        Starting
        Analyzing
        Downloading
        Converting
        Installing
        Finalizing
        Completed
    }

    AudioConverterService ..> OperationResult
    AudioConverterService ..> ProgressInfo
    VideoDownloaderService ..> OperationResult
    VideoDownloaderService ..> ProgressInfo
    BinariesProvisioningService ..> OperationResult
    BinariesProvisioningService ..> ProgressInfo
    OperationResult ..> ErrorCode
    ProgressInfo ..> ProgressStage
```

## Helper types

- Audio pipeline: `FfmpegProgressParser` parses ffmpeg `-progress pipe:1` output into a percentage.
- Download pipeline: `YtDlpArgumentBuilder` builds the yt-dlp arguments, `YtDlpProgressParser`
  parses its progress lines, `YtDlpErrorClassifier` maps stderr to an `ErrorCode`, and
  `DownloadArtifactLocator` finds the single file yt-dlp produced.
- Binaries subsystem: `BinaryCatalog` and `BinaryDefinition` describe what to download,
  `BinaryDownloader` performs the HTTP work, `BinarySources` and `GitHubReleaseParser` resolve
  versions, `BinaryPaths` resolves where the binaries live, `ProvisioningExceptionMapper` maps
  exceptions to `ErrorCode`, and `VersionComparer` decides when an update is needed.
