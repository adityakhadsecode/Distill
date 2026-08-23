# AI Workflow Rules

## Approach

Build **Distill** incrementally using a strict, spec-driven development workflow.
The documentation files in `docs/` define what to build, how to build it, and the current state of progress. All implementation must be executed against these specifications without inventing unapproved external dependencies or cloud telemetry.

---

## Scoping Rules

1. **One Feature Unit at a Time**: Complete and verify a single module/layer (e.g. Core Models -> Downloader -> OCR -> Obsidian Writer) before moving to subsequent layers.
2. **Small, Verifiable Increments**: Favor testable, focused code units over large speculative cross-cutting modifications.
3. **Strict Separation of Concerns**: Do not mix WinUI XAML presentation code with raw media processing or Ollama API communication in a single PR/commit.

---

## Phased Implementation Roadmap

- **Phase 1: Project Scaffolding & Core Domain Models**
  - .NET Solution and project structure setup (`Distill.App`, `Distill.Core`, `Distill.Engine`, `Distill.Obsidian`).
  - Core interfaces (`IMediaDownloader`, `IOcrEngine`, `ISpeechToTextEngine`, `ILanguageModelClient`, `IVaultWriter`).
  - Job Queue and Pipeline State Machine.

- **Phase 2: Extraction & Processing Engine**
  - `yt-dlp` subprocess runner and Instagram Post/Reel parser.
  - `ffmpeg` audio demuxer and frame sampler.
  - Native `Windows.Media.Ocr` service integration.
  - Standalone `whisper.cpp` CLI wrapper for STT.

- **Phase 3: AI Distillation & Obsidian Integration**
  - `OllamaHttpClient` with streaming completion and system prompt templates.
  - Markdown note formatter with customizable YAML frontmatter.
  - Obsidian vault filesystem writer and `obsidian://` URI launcher.

- **Phase 4: WinUI 3 Desktop Interface**
  - Fluent Design main window with Mica backdrop and navigation.
  - Quick Ingest view with clipboard auto-detection.
  - Live pipeline progress stepper card and active queue view.
  - Obsidian note preview modal.
  - Settings page (Vault directory selector, model picker, dependency check diagnostics).

- **Phase 5: Verification, Packaging & Polish**
  - End-to-end testing with sample Instagram Post and Reel URLs.
  - Edge case handling (network failure, private posts, missing binaries).
  - Standalone packaging and release readiness.

---

## When to Split Work

Split an implementation step immediately if it combines:
1. UI presentation changes together with external process/AI service changes.
2. Multiple unrelated engine integrations (e.g., implementing Whisper STT and Ollama LLM in the same single commit).
3. Ambiguities not resolved in `docs/architecture.md` or `docs/project-overview.md`.

If a change cannot be built and verified independently within a few minutes, the scope is too broad—split it.

---

## Handling Missing Requirements

- Do not invent product behavior or introduce external cloud APIs that violate the local-first principle.
- If a requirement or edge case is ambiguous, resolve it in the relevant doc file before implementing.
- If a requirement is missing or blocked, record it in `docs/progress-tracker.md` under **Open Questions**.

---

## Protected Files & Standards

Do not modify the following without explicit instructions:
- `docs/` guidelines without updating `docs/progress-tracker.md`.
- WinUI 3 generated code (`App.xaml.cs` initialization boilerplate, `.g.cs` files).

---

## Before Moving to the Next Unit (Definition of Done)

1. The current feature unit builds without compiler errors or warnings (`dotnet build`).
2. Unit tests pass for the implemented logic (`dotnet test`).
3. No invariant in `docs/architecture.md` was violated.
4. `docs/progress-tracker.md` is updated with completed tasks, architectural notes, and next steps.
