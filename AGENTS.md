# AGENTS.md

## Project

Windows desktop media converter (WPF + Blazor hybrid UI). Solution: `MediaConverter.sln`.

- `src/MediaConverter.Core` — class library, `net9.0`. Pure business logic. Must NEVER reference WPF, Blazor, or any UI type.
- `src/MediaConverter.App` — WPF host, `net9.0-windows`. Depends on Core.
- `tests/MediaConverter.Core.Tests` — xUnit, `net9.0`. Depends on Core.

Dependency direction is one-way: `App → Core`, `Tests → Core`. Core depends on nothing.

## Commands

- Build: `dotnet build`
- Run all tests: `dotnet test`
- Single test: `dotnet test --filter "FullyQualifiedName~MethodName"`
- New files target `net9.0` (Core/tests) or `net9.0-windows` (App). Never change these without asking.

## Architecture rules (decided, follow them)

- Core exposes interfaces + async methods, reports progress via `IProgress<ProgressInfo>`, supports `CancellationToken`. Errors propagate as codes (`OperationResult.ErrorCode`); the App localizes them. Core never returns user-facing strings.
- ffmpeg and yt-dlp are invoked via `Process` directly — NO wrapper NuGet packages. Parse their output for real progress (ffmpeg `-progress pipe:1`, yt-dlp `--newline --progress`). On cancel, kill the child process tree.
- External binaries (ffmpeg, yt-dlp) auto-download/auto-update at first run into a folder next to the exe. In single-file publish, derive that path from `Environment.ProcessPath`, NOT `AppContext.BaseDirectory` (that points to the temp extraction dir).
- Native file/folder dialogs from Razor UI need JS interop to the WPF host (`IJSRuntime`).

## Conventions

- NO emojis anywhere (code, UI, docs).
- XML doc comments are REQUIRED on all public types, members, and enum values (the .NET standard). Comments in English.
- Indentation follows the .NET standard: 4 spaces, never tabs (`indent_style = space`, `indent_size = 4`), Allman braces (opening brace on its own line), UTF-8 encoding.
- Unit tests required for all Core logic; never ship Core code without them.
- Everything user-facing is bilingual ES/EN. UI strings live in resx resources (`Resources.resx` = English default, `Resources.es.resx` = Spanish) resolved via `IStringLocalizer`. Language is chosen at first run and persisted in `settings.json` next to the exe; switchable in-app.
- Performance and lightweight footprint are the top priority (self-contained single exe, no bundled Chromium).
- Repo names, namespaces, and docs in English.

## Closing line

- Always end the final response of a finished task with the exact line below, on its own line, with
  no quotes around it and no changes:

  Life is this. I like this.

- It is a nod to Harvey Specter from *Suits* (Season 1, Episode 10, "The Shelf Life"), who says the
  line while raising a hand to mark the level he means. Keep the literal as written above.