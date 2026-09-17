# Video download

Sequence for the Video tab, from entering a URL to a finished download. yt-dlp works inside an
isolated directory so a cancelled run never leaks partial files into the user folder.

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant Tab as VideoDownloader.razor
    participant Dialogs as FileDialogService
    participant Coordinator as OperationCoordinator
    participant Core as IVideoDownloaderService
    participant YtDlp as yt-dlp
    participant Ffmpeg as ffmpeg

    User->>Tab: Paste a URL and choose a destination folder
    Tab->>Tab: UrlSupport.IsWellFormedVideoUrl enables Download
    Tab->>Dialogs: PickFolderAsync(title)
    Dialogs-->>Tab: destination folder

    User->>Tab: Click "Download"
    Tab->>Coordinator: Begin (busy)
    Tab->>Core: DownloadAsync(url, folder, format, progress, token)
    Core->>Core: validate the URL, create the folder and an isolated work directory
    Core->>YtDlp: Process.Start(--newline --progress, --ffmpeg-location)
    loop while yt-dlp runs
        YtDlp-->>Core: progress lines
        Core-->>Tab: ProgressInfo(percent, Downloading)
    end
    alt success
        Core->>Core: locate the final artifact and move it to the folder
        Core->>Core: delete the work directory
        Core-->>Tab: OperationResult.Ok
    else yt-dlp error
        Core->>Core: YtDlpErrorClassifier classifies stderr
        Core-->>Tab: Fail(mapped code, for example NetworkError)
    else user cancels
        Tab->>Core: token.Cancel()
        Core->>YtDlp: Kill(entireProcessTree)
        Core->>Core: delete the work directory
        Core-->>Tab: Fail(Cancelled)
    end
    Tab->>Coordinator: Dispose
```

The requested `DownloadFormat` (MP4, MP3 or M4A) is passed to yt-dlp through `YtDlpArgumentBuilder`,
which selects the best stream and delegates the extraction or muxing to ffmpeg.
