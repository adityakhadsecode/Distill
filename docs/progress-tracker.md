# Progress Tracker

## Current Phase

- **Phase 3: End-to-End Pipeline Integration & Polish (Completed)**
- **Phase 4: Packaging & Distribution (Completed)**
- **Phase 5: Enhanced Settings & Live Diagnostics (Completed)**
- **Phase 6: Native Fluent Design UI Polish & Onboarding (Completed)**
- **Phase 7: Fluent 2 / WinUI 3 Design System & Shell Refactoring (Completed)**

---

## Current Goal

- Release and distribute Distill with full Fluent 2 / WinUI 3 design system adherence, extended Mica title bar, native NavigationView, structured Settings cards/expanders, and semantic readiness iconography.

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
- [x] Redesigned WinUI 3 application shell with compact Windows 11 Fluent Design navigation rail (`NavigationView`), identity area, and clean view switcher.
- [x] Added First-Launch Onboarding ("Get Started with Distill") screen with 3-step visual guide (Paste, Distill, Save) and live Engine Readiness checklist (Vault, Ollama, Whisper, OCR).
- [x] Persisted onboarding completion flag (`HasCompletedOnboarding`) in `DistillSettings` and added a "Show Getting Started Guide" action in Settings.
- [x] Redesigned Main Extract screen with prominent compact URL input, paste-from-clipboard support, inline regex validation, and supported format labels.
- [x] Created useful empty state illustration with local-first feature pills.
- [x] Refined active queue cards with status badge pills, clear stage step descriptions, progress bars, action buttons ("Open in Obsidian", "View File"), and an expandable diagnostic details pane (`Expander`).
- [x] Redesigned Settings into 5 structured, well-proportioned sections: General, AI & Speech Synthesis, Pipeline & Performance, System Health & Tools, and About Distill.
- [x] Converted status brush styling to theme-adaptive semantic colors supporting both dark and light Windows themes.
- [x] Added unit test suite for onboarding settings and URL validation — **53 tests passing (100%)**.

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
- **Decision 11: Dual Distribution Strategy (Portable EXE + Signed MSIX)**
  - *Rationale*: Providing both a self-contained portable directory (for immediate testing without installation) and an official signed MSIX package (with Start menu tile and system integration) gives users the best of both installation models.
- **Decision 12: Real-time Diagnostics & In-App Asset Management**
  - *Rationale*: Enabling users to check tool readiness, auto-discover Ollama models, download Whisper voice models, and pick vault folders with native dialogs in-app dramatically simplifies user onboarding and maintenance.
- **Decision 13: Native Windows 11 Fluent Design & Light/Dark Theme Adaptivity**
  - *Rationale*: Replacing flat developer-console cards with native Windows 11 Fluent Design controls (`Expander`, `InfoBar`, `ProgressBar`, `ToggleSwitch`, `Slider`) and theme-aware resources ensures a polished, calm, and professional productivity experience that integrates seamlessly with Windows 11.
- **Decision 14: Single-Page Adaptive Layout, Branded Custom TitleBar & Sliding Settings Drawer**
  - *Rationale*: Replacing a persistent left navigation sidebar with an adaptive single-page canvas provides maximum focus. When empty, the URL input sits centered in spotlight mode; when jobs are processed, it docks to the top with the live queue below. The custom TitleBar adapts to pure black in Dark mode and pure white in Light mode with "Distill" typography, while a floating bottom-right Settings gear button opens an overlay sliding drawer.
- **Decision 15: Fluent 2 Shell Integration with Extended Mica TitleBar and Native NavigationView**
  - *Rationale*: Embracing Windows 11 Fluent 2 design principles by extending content into the title bar (`ExtendsContentIntoTitleBar = true`), utilizing `MicaBackdrop` with acrylic fallback, hosting top-level destinations within a native `NavigationView` (`PaneDisplayMode="LeftCompact"`), structuring settings in clean 60px/32px cards and `Expander` sections, and standardizing readiness states with Segoe Fluent Icon glyphs (`\uE73E`, `\uE7BA`, `\uEA39`) and semantic brushes (`SystemFillColorSuccessBrush`, `SystemFillColorCautionBrush`, `SystemFillColorCriticalBrush`).
- **Decision 16: Geometric Distillation Crucible Brand Identity & App Logo System**
  - *Rationale*: Designing an intentional, ownable brand symbol that visualizes the core value proposition: raw noisy media transformed into pure, structured knowledge. The mark uses clean isometric facets transitioning from Royal Amethyst Violet (`#7C3AED`) and Electric Indigo (`#6366F1`) into a luminous crystal droplet (`#DDD6FE`) on a dark obsidian squircle, rendered via sharp XAML vector geometry in-app and high-resolution antialiased PNG assets for Windows packaging.

---

- [x] Implemented native Windows 11 extended title bar in `MainWindow.xaml.cs` with `MicaBackdrop` (and `DesktopAcrylicBackdrop` fallback), drag region registration, and theme-adaptive caption buttons (`AppWindowTitleBar`).
- [x] Restructured top-level application navigation using Fluent 2 `NavigationView` in `MainPage.xaml` with bidirectional selection synchronization in `MainViewModel`.
- [x] Refactored Extraction Queue with hero spotlight empty state, docked URL input bar, live progress indicators, stage badges, and expandable extracted details accordion.
- [x] Upgraded Get Started onboarding guide with 3-step explainer cards and live System & Engine Readiness dashboard powered by `BoolToGlyphConverter` and `BoolToBrushConverter`.
- [x] Refactored Settings into 5 structured Fluent 2 sections (General, AI & Speech Synthesis, Pipeline & Performance, System Tools, and Save Footer) using native WinUI 3 card and expander layout patterns.
- [x] Created the Distill Brand Identity & Logo System (3×3 presentation board and 1:1 master app icon).
- [x] Replaced placeholder badges with native XAML vector geometry for TitleBar and Hero Spotlight in `MainPage.xaml`.
- [x] Upgraded `build-packages.ps1` to render the geometric Crucible logo across all Windows MSIX and application tile assets.
- [x] Maintained 100% test coverage with all 61 unit tests passing.
- [x] Zero compilation warnings and zero errors across the entire solution.
- [x] Built and signed installable MSIX package (`publish/Distill_1.0.0.0_x64.msix`) with self-signed certificate (`publish/Distill_DevCert.cer`).
- [x] Published self-contained portable executable build (`publish/Distill-Portable-x64/Distill.App.exe`).
