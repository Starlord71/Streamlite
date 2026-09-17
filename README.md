# MediaConverter

Windows desktop media converter built with WPF and a Blazor hybrid UI. It is intended to cover
three tasks:

- Convert audio files between M4A and MP3.
- Download media from a URL as MP4, MP3 or M4A.
- Extract MP3 audio from a local MP4 file.

The project is under active development. The current milestone completes the MVP and its UX polish:
the three feature tabs with consistent validation and state handling, described under
[Status](#status).

## Architecture

The solution is split into three projects with a strict one-way dependency direction:

| Project | Target | Purpose |
| --- | --- | --- |
| `src/MediaConverter.Core` | `net9.0` | Business logic only. Owns the service contracts and models. Never references WPF, Blazor or any UI type. |
| `src/MediaConverter.App` | `net9.0-windows` | WPF host that embeds a `BlazorWebView`. Depends on Core. |
| `tests/MediaConverter.Core.Tests` | `net9.0` | xUnit tests for Core. Depends on Core. |

Dependency direction: `App -> Core` and `Tests -> Core`. Core depends on nothing.

### UI

The UI is built from Razor components rendered inside a WPF `BlazorWebView`. A single
`ServiceCollection` is built at startup and handed to the WebView, so Razor components receive the
Core services through constructor-style `@inject` rather than a classic MVVM view-model layer.

### Error handling

Core never returns user-facing strings. Every operation returns an `OperationResult` that carries a
machine-readable `ErrorCode` on failure. The App layer maps each code to text in the active
language, which keeps Core free of localization concerns. Long-running operations report progress
through `IProgress<ProgressInfo>` and accept a `CancellationToken` for cancellation.

## Requirements

- Windows 10 or later.
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).
- An internet connection on first run, so the external binaries can be downloaded.

## Build, test and run

```sh
dotnet build
dotnet test
dotnet run --project src/MediaConverter.App
```

## External binaries (ffmpeg and yt-dlp)

The conversion and download features rely on two external command-line tools:

- **ffmpeg** is a command-line tool for converting, transcoding and remuxing audio and video. In
  this project it performs the M4A <-> MP3 conversions, extracts MP3 audio from a local MP4 file,
  and handles the extraction and muxing that yt-dlp delegates to it.
- **yt-dlp** is a command-line video/audio downloader (a fork of youtube-dl). It resolves the
  requested URL and downloads the best stream available for the chosen output format.

Neither binary is bundled in this repository. They are downloaded automatically on first run into a
folder next to the application executable, and they keep themselves up to date on later runs. Both
are invoked directly as external processes (`Process`) with their output parsed for real progress;
no wrapper NuGet packages are used.

## Status

Phase 10 (UX polish) is complete. The MVP from Phase 9 still exposes all three features, now with
consistent validation, empty states and error handling across the three tabs:

- The WPF host embeds a `BlazorWebView` and the Core services are registered in dependency
  injection.
- On startup the application calls `IBinariesProvisioningService.ProvisionAsync`, showing real
  download/install progress, and opens the tabbed workspace once it finishes. If there is no
  internet connection the failure is reported through the localized `NetworkError` message, and a
  Retry button reruns provisioning.
- Every user-facing string lives in `Resources.resx` (English, default) and `Resources.es.resx`
  (Spanish) and is resolved through `IStringLocalizer`. Both resource tables keep exact key parity.
- On first run the app asks for the language (pre-selecting the system language) before
  provisioning, stores the choice in `settings.json` next to the executable and applies it on
  later runs. A language selector is always visible and switches the whole UI instantly, including
  the tabs that are not currently rendered.
- The Audio tab converts a local M4A/MP3 file end to end. The source is picked with a native
  Windows file dialog, the target format defaults to the opposite of the source, and the output
  path is derived next to the source (with a numeric suffix so an existing file is never
  overwritten).
- The Video tab downloads media from a URL end to end. The link is validated as a well-formed
  `http`/`https` URL (a malformed link is highlighted after the field loses focus and keeps the
  Download button disabled), the output format is chosen between MP4, MP3 and M4A, and the
  destination folder is picked with a native Windows folder dialog. Because the service picks the
  output file name, the success state reports the destination folder.
- The Video-to-audio tab extracts MP3 audio from a local MP4 file end to end. A non-MP4 selection
  is rejected with the localized unsupported-format error, and the MP3 output path reuses the same
  non-overwriting naming helper as the Audio tab.
- Each tab shows a localized hint that explains the flow to a non-technical user and an empty state
  before anything is selected. Unsupported-format and operation errors are always rendered, even
  when no valid selection drives the rest of the form.
- Cancelling an operation is treated as a neutral outcome (notice, no error styling), clears the
  progress bar and re-enables every control, so a cancelled or failed run never leaves a stuck
  state. The inline cancel confirmation disappears on its own if the operation finishes first.
- An `OperationCoordinator` marks a run as in progress before the first `await`, so a second click
  cannot start a parallel operation. While an operation runs, the pickers, format options, URL
  input, tab bar and language selector are disabled and released as soon as it ends.

Upcoming phases add packaging and release documentation.
Screenshots and a demo GIF are planned once the packaged build is ready.
