# Progress Tracker

## Current Phase

- **Phase 3: End-to-End Pipeline Integration & Polish (Completed)**

---

## Current Goal

- Ship fully functional, production-ready WinUI 3 desktop application with real-time pipeline job queueing, Settings persistence, and direct Obsidian launching.

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
- [x] Fixed Post carousel download path in `YtDlpReelDownloader`: runs `yt-dlp --dump-single-json`, handles image-only slides via direct HTTP thumbnail downloads (preventing "no video formats found" errors), and processes video slides with yt-dlp + ffmpeg.
- [x] Defined concrete domain result types: `PostDownloadResult` and `ReelDownloadResult` with explicit `Cleanup()` lifecycle management.
- [x] Added domain exceptions: `PrivateMediaException`, `RateLimitException`, `MediaNotFoundException`, `DistillDownloadException`.
- [x] Added unit test suite with `FakeProcessRunner` verifying Post downloads, Reel demuxing/sampling, fallback frame sampling, exception handling, and cleanup.
- [x] Implemented native `WindowsMediaOcrExtractor` using `Windows.Media.Ocr.OcrEngine` with user profile language loading, reading order sorting, and parallel batch processing (`ExtractTextFromMultipleAsync`).
- [x] Added `OcrLanguageNotInstalledException` with actionable Windows settings instructions.
- [x] Implemented `WhisperCppTranscriber` wrapping `whisper-cli.exe` with settings-based model path, thread count, language hints, timestamp stripping, diagnostic filtering, and graceful silent audio handling.
- [x] Implemented `OllamaNoteFormatter` calling `POST /api/generate` with anti-boilerplate distillation prompt templates, retry-once on failure, timeout handling, and `OllamaConnectionException`.
- [x] Implemented `ObsidianVaultWriter` with heading-based title extraction, filesystem slug sanitization, collision suffix resolution, YAML frontmatter formatting, and `obsidian://` direct URI generation.
- [x] Implemented `PipelineOrchestrator` in `Distill.Core.Pipeline` orchestrating full sequence (download -> OCR/STT -> Ollama distillation -> Obsidian vault save) with fine-grained progress events.
- [x] Built WinUI 3 `MainPage` with Fluent `NavigationView`, Ingest Hero card, Real-time Job Queue `ListView`, and complete `Settings` management panel.
- [x] Implemented background task job execution so UI remains 100% fluid and non-blocking during heavy AI/extraction workloads.
- [x] Added comprehensive unit test suites covering all components (`PipelineOrchestratorTests`, `YtDlpReelDownloaderTests`, `WindowsMediaOcrExtractorTests`, `WhisperCppTranscriberTests`, `OllamaNoteFormatterTests`, `ObsidianVaultWriterTests`) — **33 tests passing**.

---

## In Progress

- Production verification and packaging.

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
- **Decision 7: Resilient Local Ollama Client**
  - *Rationale*: `OllamaNoteFormatter` uses `stream: false` with single-retry resilience and clear diagnostics directing the user to start `ollama serve` if offline.
- **Decision 8: Collision-Free Slugged Vault Writer**
  - *Rationale*: Derives slugs from the synthesized note's H1 title, applies character sanitization, and appends numeric suffixes upon collision, maintaining full compatibility with Obsidian links.
- **Decision 9: Event-Driven Non-Blocking Pipeline Orchestration**
  - *Rationale*: `PipelineOrchestrator` fires `JobChanged` events as each phase completes; `MainViewModel` updates observable items via `DispatcherQueue` on background worker tasks, ensuring the UI is never blocked by video downloads or AI inference.
- **Decision 10: Metadata Dump & Direct HTTP Carousel Ingestion**
  - *Rationale*: Running `yt-dlp --dump-single-json` inspects carousel entries individually; downloading image slides directly via `HttpClient` resolves the "no video formats found" yt-dlp error while preserving video download handling for mixed carousels.

---

## Session Notes

- Carousel post download path fixed and verified with 3 dedicated mock unit tests.
- Application recompiled and running on desktop.
