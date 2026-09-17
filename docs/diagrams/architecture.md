# Architecture

Layered view of the solution. The dependency direction is strictly one way: the App depends on
Core, and Core depends on nothing (no WPF, no Blazor, no UI types).

```mermaid
flowchart TB
    subgraph App["MediaConverter.App - net9.0-windows (WPF host)"]
        direction TB
        Window["MainWindow.xaml<br/>BlazorWebView, HostPage wwwroot/index.html"]
        subgraph Components["Razor components"]
            Main["Main.razor<br/>language bar, tab bar, setup screens"]
            AudioTab["AudioConverter.razor"]
            VideoTab["VideoDownloader.razor"]
            VideoToAudioTab["VideoToAudio.razor"]
        end
        subgraph AppServices["App services (dependency injection)"]
            Language["LanguageService<br/>applies CultureInfo, persists settings.json"]
            Dialogs["FileDialogService (scoped)<br/>wraps IJSRuntime and the WPF dialogs"]
            Coordinator["OperationCoordinator<br/>one operation at a time, busy flag"]
            Validators["UrlSupport, AudioFileSupport,<br/>VideoFileSupport"]
            Localizer["LocalizerExtensions<br/>maps ErrorCode and ProgressStage to text"]
        end
        Resources["Resources.resx (English) + Resources.es.resx (Spanish)"]
        Assets["wwwroot: index.html, css/app.css, js/fileDialog.js"]
    end

    subgraph Core["MediaConverter.Core - net9.0 (class library, no UI)"]
        direction TB
        Interfaces["Interfaces<br/>IAudioConverterService, IVideoDownloaderService,<br/>IBinariesProvisioningService"]
        Services["Services<br/>AudioConverterService, VideoDownloaderService,<br/>BinariesProvisioningService"]
        Parsers["Parsers and classifiers<br/>FfmpegProgressParser, YtDlpProgressParser,<br/>YtDlpArgumentBuilder, YtDlpErrorClassifier,<br/>DownloadArtifactLocator"]
        BinarySub["Binaries subsystem<br/>BinaryCatalog, BinaryDownloader, BinaryPaths,<br/>BinarySources, GitHubReleaseParser,<br/>ProvisioningExceptionMapper, VersionComparer"]
        Models["Models<br/>OperationResult, ProgressInfo, ErrorCode,<br/>AudioFormat, DownloadFormat, ProgressStage"]
    end

    Window --> Components
    Main --> AudioTab
    Main --> VideoTab
    Main --> VideoToAudioTab
    AudioTab --> AppServices
    VideoTab --> AppServices
    VideoToAudioTab --> AppServices
    Localizer --> Resources
    Components -.-> Assets
    Dialogs <-->|"JavaScript interop"| Assets

    App -->|"interfaces, async methods,<br/>IProgress and CancellationToken"| Core
    Services --> Parsers
    Services --> BinarySub
    Interfaces --> Models
    Services --> Models

    subgraph External["External tools and files (next to the executable)"]
        FFmpeg["ffmpeg.exe"]
        YtDlp["yt-dlp.exe"]
        Settings["settings.json (language preference)"]
    end
    Services --> FFmpeg
    Services --> YtDlp
    BinarySub --> FFmpeg
    BinarySub --> YtDlp
    Language --> Settings
```

The golden rule: Core never knows a UI exists. It exposes interfaces, async methods, progress through
`IProgress<ProgressInfo>` and cancellation through `CancellationToken`, and it reports failures as
machine-readable `ErrorCode` values. The App translates those codes into the active language, so Core
stays free of user-facing strings.
