# Code Standards

## General Principles

1. **Keep Modules Single-Purpose**: Each project layer and service owns a single domain responsibility. UI does not download media; downloaders do not write to vaults.
2. **Fail-Safe & Resilient Subprocess Execution**: External CLI dependencies (`yt-dlp`, `ffmpeg`, `whisper.cpp`) must be executed with proper error capture, non-blocking stderr/stdout streaming, and informative exception wrapping.
3. **No Unhandled Blocking Operations**: Never block the WinUI 3 UI thread. All I/O, subprocess execution, OCR, STT, and HTTP requests must be asynchronous.
4. **Strict Local-First Boundary**: Do not introduce dependencies that perform telemetry or cloud calls without explicit user consent.

---

## C# and .NET Conventions

- **Language & Runtime**: Target .NET 8 or .NET 9 with C# 12.
- **Strict Nullability**: `<Nullable>enable</Nullable>` is required across all project files. Resolve all compiler warnings without using `!` null-forgiving operators unless guarded.
- **Async / Await Pattern**:
  - Always accept and pass a `CancellationToken` across all asynchronous methods.
  - Suffix async methods with `Async` (e.g. `DownloadMediaAsync`).
  - Use `ConfigureAwait(false)` in core/engine libraries (outside WinUI UI thread contexts).
- **Domain Modeling**:
  - Use C# `record` types for immutable data contracts (e.g., `DistillJob`, `OcrResult`, `TranscriptionSegment`, `ObsidianNoteMetadata`).
  - Use strongly typed Enums for statuses and post types (`JobStatus`, `InstagramMediaType`, `OllamaModelStatus`).
- **Dependency Injection**:
  - Register services as `Singleton` or `Transient` using `Microsoft.Extensions.DependencyInjection`.
  - Constructor injection is mandatory for all viewmodels and services.

---

## WinUI 3 & MVVM Conventions

- **MVVM Framework**: Use `CommunityToolkit.Mvvm`.
- **ViewModels**: Inherit from `ObservableObject`. Use `[ObservableProperty]` and `[RelayCommand]` source generators.
- **Thread Marshalling**: When updating observable UI properties from background event listeners, marshal to the UI thread using `DispatcherQueue.TryEnqueue()`.
- **Resource Management**: Properly dispose of WinRT image streams, `Process` handles, and `HttpClient` resources implementing `IDisposable` or `IAsyncDisposable`.

---

## Subprocess & External CLI Handling

- **Subprocess Runner**: Encapsulate all CLI calls (`yt-dlp`, `ffmpeg`, `whisper.cpp`) within an `IProcessRunner` service.
- **Streams**: Read `StandardOutput` and `StandardError` asynchronously to prevent OS buffer deadlocks.
- **Progress Reporting**: Parse progress percentages via regular expressions (e.g. yt-dlp download percentage, ffmpeg timecode) and forward via `IProgress<T>`.
- **Executable Resolution**: Check bundled/local paths first (`%LOCALAPPDATA%/Distill/bin/`), followed by system `PATH`. Provide clear diagnostic errors if a binary is missing.

---

## Obsidian File & Markdown Standards

- **Filename Sanitization**: Strip or replace illegal characters (`/`, `\`, `:`, `*`, `?`, `"`, `<`, `>`, `|`, `#`, `^`, `[`, `]`) when generating Markdown note filenames.
- **YAML Frontmatter Structure**:
  ```yaml
  ---
  title: "Captured Post Title"
  date: 2026-08-23T21:55:00
  source_url: "https://www.instagram.com/p/..."
  author: "@creator_handle"
  type: "instagram_post" # or "instagram_reel"
  tags:
    - instagram
    - knowledge-extraction
    - tech
  summary: "Brief distilled summary of the post..."
  ---
  ```
- **Collision Resolution**: If a note named `distilled-note.md` already exists, resolve collision by writing `distilled-note-1.md` or prefixing with `yyyy-MM-dd-`.
- **Atomic Writes**: Write files using standard UTF-8 encoding (`new UTF8Encoding(false)` - without BOM unless required by user settings).

---

## File Organization

```text
src/
├── Distill.App/                 # WinUI 3 Desktop Application
│   ├── Assets/                  # App icons, fonts, splash screens
│   ├── Converters/              # XAML Value Converters
│   ├── Controls/                # Custom UI controls & Stepper cards
│   ├── Services/                # UI-specific services (Clipboard, Dialogs)
│   ├── ViewModels/              # MVVM ViewModels
│   ├── Views/                   # XAML Pages and Windows
│   └── App.xaml                 # App bootstrap and DI container setup
│
├── Distill.Core/                # Pure Domain & Business Logic
│   ├── Models/                  # Data records, entities, enums
│   ├── Interfaces/              # Abstractions for engines, storage, queue
│   ├── Pipeline/                # Distillation orchestrator & state machine
│   └── Services/                # Configuration and Settings manager
│
├── Distill.Engine/              # Low-Level Process & AI Providers
│   ├── Downloaders/             # YtDlpDownloader & metadata parser
│   ├── Media/                   # FFmpegProcessor (demux, frame extraction)
│   ├── Ocr/                     # WindowsMediaOcrService (WinRT OCR)
│   ├── Speech/                  # WhisperCppService (STT process runner)
│   └── Ai/                      # OllamaHttpClient & Prompt templates
│
└── Distill.Obsidian/            # Obsidian Vault & Markdown Formatting
    ├── Vault/                   # Vault detector and file writer
    ├── Formatter/               # Markdown synthesis and frontmatter builder
    └── Protocol/                # Obsidian URI handler
```
