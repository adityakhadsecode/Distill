# Progress Tracker

## Current Phase

- **Phase 1: Project Scaffolding & Core Interfaces (Complete)**
- Ready for **Phase 2: Extraction & Processing Engine Implementations**

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
- [x] Created `Distill.Core` class library with domain models and 5 core pipeline interfaces/stubs:
  - `Downloaders/` (`IReelDownloader`, `ReelDownloaderStub`)
  - `Ocr/` (`ITextExtractor`, `WindowsMediaOcrExtractorStub`)
  - `SpeechToText/` (`ITranscriber`, `WhisperCppTranscriberStub`)
  - `Formatting/` (`INoteFormatter`, `OllamaNoteFormatterStub`)
  - `VaultWriter/` (`IVaultWriter`, `ObsidianVaultWriterStub`)
- [x] Added `Distill.App` WinUI 3 desktop application with `appsettings.json`, `app.manifest`, `MainViewModel` MVVM logic, and full `Microsoft.Extensions.DependencyInjection` container wiring in `App.xaml.cs`.
- [x] Created `Distill.Tests` test suite with unit tests verifying pipeline stubs, frontmatter formatting, and settings loading.

---

## In Progress

- Phase 2 Engine implementation readiness.

---

## Next Up

- **Phase 2**:
  - Implement real `YtDlpReelDownloader` using managed CLI subprocess execution.
  - Implement `FFmpegAudioAndFrameProcessor` for 16kHz WAV audio demuxing and keyframe extraction.
  - Implement native `WindowsMediaOcrExtractor` utilizing WinRT `Windows.Media.Ocr.OcrEngine`.
  - Implement `WhisperCppTranscriber` wrapping `whisper-cli.exe`.
  - Implement `OllamaNoteFormatter` calling `http://localhost:11434/api/generate` with prompt templates.

---

## Open Questions & Considerations

1. **Whisper.cpp Execution Mode**: Use pre-compiled standalone CLI executable (`whisper-cli.exe` called via subprocess) vs C# P/Invoke native DLL bindings (`whisper.net`).
   - *Recommendation*: Subprocess CLI execution matches `yt-dlp` and `ffmpeg` execution patterns with clean memory isolation.
2. **Default Ollama Model Selection**: Default to `llama3.2:3b` for fast, lightweight denoising on consumer laptops, with simple settings UI dropdown to switch to `qwen2.5:7b` or `llama3.1:8b`.
3. **Instagram Cookie Handling**: Provide an optional settings input to specify browser or cookies file for `yt-dlp` to handle age-restricted reels.

---

## Architecture Decisions

- **Decision 1: Zero-Python Native Architecture**
  - *Rationale*: Eliminates the need to bundle a bulky, fragile Python environment with the Windows app. `yt-dlp`, `ffmpeg`, and `whisper.cpp` run as standalone binaries; `Windows.Media.Ocr` runs via native Windows runtime; `Ollama` runs via local HTTP.
- **Decision 2: Decoupled Core Layer**
  - *Rationale*: Isolating extraction, OCR, STT, and LLM orchestration from WinUI 3 presentation types allows the same core logic to be reused for future Android app or CLI tools.
- **Decision 3: Direct Vault Filesystem Integration**
  - *Rationale*: Direct `.md` file writing requires no Obsidian community plugin installation and immediately works with Obsidian's live filesystem watcher, accompanied by `obsidian://` URI deep links.

---

## Session Notes

- Solution and 3 projects created cleanly.
- Dependency injection configured in `App.xaml.cs` with full MVVM bindings in `MainViewModel.cs` and `MainWindow.xaml`.
