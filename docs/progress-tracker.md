# Progress Tracker

## Current Phase

- **Phase 2: Extraction & Processing Engine Implementations (In Progress)**

---

## Current Goal

- Implement production engine components: `yt-dlp` media downloader, `ffmpeg` audio/frame processor, native `Windows.Media.Ocr`, `whisper.cpp` STT runner, and `Ollama` REST API client.

---

## Completed

- [x] Defined complete product vision, user flows, and scope in [docs/project-overview.md](file:///e:/Coding/Projects/local-first%20Instagram%20%E2%86%92%20Obsidian%20knowledge%20extraction/docs/project-overview.md).
- [x] Architected modular local-first system with sequence and component diagrams in [docs/architecture.md](file:///e:/Coding/Projects/local-first%20Instagram%20%E2%86%92%20Obsidian%20knowledge%20extraction/docs/architecture.md).
- [x] Specified Windows 11 Fluent Design tokens, color palette, and layout patterns in [docs/ui-context.md](file:///e:/Coding/Projects/local-first%20Instagram%20%E2%86%92%20Obsidian%20knowledge%20extraction/docs/ui-context.md).
- [x] Established C# 12/.NET, WinUI 3, and subprocess engineering standards in [docs/code-standards.md](file:///e:/Coding/Projects/local-first%20Instagram%20%E2%86%92%20Obsidian%20knowledge%20extraction/docs/code-standards.md).
- [x] Documented phased delivery roadmap and development guidelines in [docs/ai-workflow-rules.md](file:///e:/Coding/Projects/local-first%20Instagram%20%E2%86%92%20Obsidian%20knowledge%20extraction/docs/ai-workflow-rules.md).
- [x] Created `Distill.sln` solution linking `Distill.App`, `Distill.Core`, and `Distill.Tests`.
- [x] Created `Distill.Core` class library with domain models and 5 core pipeline interfaces/stubs.
- [x] Configured GitHub Actions CI and public repository synchronization.
- [x] Implemented `IProcessRunner` (`DefaultProcessRunner`) and `IToolLocator` (`ToolLocator`) for managed subprocess execution and tool discovery.
- [x] Implemented `YtDlpReelDownloader` with Post (carousel image download) and Reel (video download + ffmpeg 16kHz WAV audio demux + scene/interval frame extraction).
- [x] Defined concrete domain result types: `PostDownloadResult` and `ReelDownloadResult` with explicit `Cleanup()` lifecycle management.
- [x] Added domain exceptions: `PrivateMediaException`, `RateLimitException`, `MediaNotFoundException`, `DistillDownloadException`.
- [x] Added unit test suite with `FakeProcessRunner` verifying Post downloads, Reel demuxing/sampling, fallback frame sampling, exception handling, and cleanup.
- [x] Implemented native `WindowsMediaOcrExtractor` using `Windows.Media.Ocr.OcrEngine` with user profile language loading, reading order sorting, and parallel batch processing (`ExtractTextFromMultipleAsync`).
- [x] Added `OcrLanguageNotInstalledException` with actionable Windows settings instructions.
- [x] Implemented `WhisperCppTranscriber` wrapping `whisper-cli.exe` with settings-based model path, thread count, language hints, timestamp stripping, diagnostic filtering, and graceful silent audio handling.
- [x] Added unit test suite in `Distill.Tests/WhisperCppTranscriberTests.cs`.

---

## In Progress

- Phase 2 Engine Implementations (LLM formatting & Obsidian Vault writing).

---

## Next Up

- **Phase 2 (Continued)**:
  - Implement `OllamaNoteFormatter` calling `http://localhost:11434/api/generate` with prompt templates.
  - Implement `ObsidianVaultWriter` with robust frontmatter formatting and collision resolution.

---

## Open Questions & Considerations

1. **Default Ollama Model Selection**: Default to `llama3.2:3b` for fast, lightweight denoising on consumer laptops, with simple settings UI dropdown to switch to `qwen2.5:7b` or `llama3.1:8b`.
2. **Instagram Cookie Handling**: Provide an optional settings input to specify browser or cookies file for `yt-dlp` to handle age-restricted reels.

---

## Architecture Decisions

- **Decision 1: Zero-Python Native Architecture**
  - *Rationale*: Eliminates the need to bundle a bulky, fragile Python environment with the Windows app. `yt-dlp`, `ffmpeg`, and `whisper.cpp` run as standalone binaries; `Windows.Media.Ocr` runs via native Windows runtime; `Ollama` runs via local HTTP.
- **Decision 2: Decoupled Core Layer**
  - *Rationale*: Isolating extraction, OCR, STT, and LLM orchestration from WinUI 3 presentation types allows the same core logic to be reused for future Android app or CLI tools.
- **Decision 3: Direct Vault Filesystem Integration**
  - *Rationale*: Direct `.md` file writing requires no Obsidian community plugin installation and immediately works with Obsidian's live filesystem watcher, accompanied by `obsidian://` URI deep links.
- **Decision 4: Explicit Cleanup Model on DownloadResult**
  - *Rationale*: `DownloadResult` implements `IDisposable` with a non-automatic `Cleanup()` method, allowing downstream pipeline stages (OCR, STT) to safely access intermediate images and audio files before explicitly purging the working directory.
- **Decision 5: Native WinRT OCR with Vertical Band Sorting**
  - *Rationale*: `Windows.Media.Ocr` runs in-process with hardware acceleration and no external dependencies. Sorting detected word bounding boxes into vertical bands preserves natural human reading flow across Instagram carousels and infographic slides.
- **Decision 6: Non-Throwing Graceful Transcriber Fallback**
  - *Rationale*: If audio is silent, corrupt, or whisper is not installed, `WhisperCppTranscriber` returns an empty string rather than throwing, allowing OCR-only synthesis to complete the note.

---

## Session Notes

- `WhisperCppTranscriber` implemented and registered in DI container.
- Unit tests added in `WhisperCppTranscriberTests.cs`.
