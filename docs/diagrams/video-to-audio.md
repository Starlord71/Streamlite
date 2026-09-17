# Video to audio

Sequence for the Video to audio tab. It reuses `IAudioConverterService`: ffmpeg discards the video
stream with `-vn`, so an MP4 source follows the exact same code path as an audio file.

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant Tab as VideoToAudio.razor
    participant Dialogs as FileDialogService
    participant Core as IAudioConverterService
    participant Ffmpeg as ffmpeg

    User->>Tab: Click "Choose file..."
    Tab->>Dialogs: PickFileAsync(title, filter)
    Dialogs-->>Tab: selected path
    alt not an MP4
        Tab->>Tab: show UnsupportedFormat, clear the selection
    else MP4
        Tab->>Tab: BuildTargetPath(path, MP3) next to the source
    end

    User->>Tab: Click "Extract audio"
    Tab->>Core: ConvertAsync(source, output, MP3, progress, token)
    Note over Core,Ffmpeg: same conversion pipeline, ffmpeg drops the video with -vn
    Core->>Ffmpeg: Process.Start(-vn -progress pipe:1)
    Ffmpeg-->>Core: progress
    Core-->>Tab: ProgressInfo(percent, Converting)
    Core-->>Tab: OperationResult.Ok or Fail(code)
```
