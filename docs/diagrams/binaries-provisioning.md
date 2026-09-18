# Binaries provisioning

First-run flow that prepares ffmpeg and yt-dlp in the application data folder
(`%LOCALAPPDATA%\MediaConverter`). Progress is real (byte-based), and a failure is mapped to a
machine-readable code that the App localizes.

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant Main as Main.razor
    participant Language as LanguageService
    participant Core as IBinariesProvisioningService
    participant Downloader as BinaryDownloader
    participant Fs as File system

    User->>Main: Pick a language on first run
    Main->>Language: SetLanguage(code)
    Language->>Fs: write settings.json
    Main->>Core: ProvisionAsync(progress, token)

    loop for each binary in BinaryCatalog
        Core->>Fs: check the binary and its size
        alt already usable
            Core->>Core: skip
        else missing
            Core->>Downloader: FetchLatestVersionAsync
            Core->>Downloader: DownloadAsync to target.part
            Downloader-->>Core: progress through IProgress
            Core-->>Main: ProgressInfo(percent, Downloading)
            Core->>Fs: extract the archive or move the file into place
            Core-->>Main: ProgressInfo(100, Installing)
            Core->>Fs: write the version marker
        end
    end

    alt success
        Core-->>Main: OperationResult.Ok
        Main->>Main: open the tabbed workspace
    else no internet or failure
        Core->>Core: ProvisioningExceptionMapper maps the exception
        Core-->>Main: Fail(NetworkError or another code)
        Main->>Main: show the localized message and a Retry button
    end
```

On later runs the language is read from `settings.json` and provisioning starts immediately; if the
binaries are already present and valid they are skipped.
