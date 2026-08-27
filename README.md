# Distill ⚡

> **Local-First Instagram Reel & Post Knowledge Extraction for Obsidian**

[![Release](https://img.shields.io/github/v/release/adityakhadsecode/Distill?color=7C3AED&label=release)](https://github.com/adityakhadsecode/Distill/releases/latest)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WinUI 3](https://img.shields.io/badge/UI-WinUI%203%20%2F%20Fluent%202-0078D4?logo=windows&logoColor=white)](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/)
[![Ollama](https://img.shields.io/badge/LLM-Ollama%20(Local)-white?logo=ollama&logoColor=black)](https://ollama.com/)
[![Whisper](https://img.shields.io/badge/STT-whisper.cpp-4F46E5)](https://github.com/ggerganov/whisper.cpp)
[![Obsidian](https://img.shields.io/badge/Target-Obsidian%20Vault-7C3AED?logo=obsidian&logoColor=white)](https://obsidian.md/)
[![Privacy](https://img.shields.io/badge/Privacy-100%25%20Local--First-10B981)](#-zero-cloud-privacy)

**Distill** is a native Windows 11 desktop application built with WinUI 3 and .NET 8 that transforms educational Instagram content (multi-slide carousel posts and video Reels) into clean, structured, and richly formatted Markdown notes saved directly into your local [Obsidian](https://obsidian.md) vault.

Never lose actionable knowledge in Instagram's "Saved" bookmark abyss again.

---

## 📥 Download & Installation

Get the latest release from the [**Releases Page**](https://github.com/adityakhadsecode/Distill/releases/latest):

| Download | Description | Instructions |
| :--- | :--- | :--- |
| [**`Distill_1.0.0.0_x64.msix`**](https://github.com/adityakhadsecode/Distill/releases/latest) | **Signed Windows 11 App Package** | Download the `.msix`, `.cer` certificate, and `install-msix.ps1`. Run `powershell -ExecutionPolicy Bypass -File .\install-msix.ps1`. |
| [**`Distill-Portable-x64.zip`**](https://github.com/adityakhadsecode/Distill/releases/latest) | **Standalone Portable ZIP** | Extract `.zip` and double-click `Distill.App.exe`. Zero installation required. |

---

## 🎯 How It Works

```mermaid
flowchart LR
    A["📸 Post / 🎬 Reel URL"] --> B["⬇️ yt-dlp Downloader"]
    B --> C{"Media Type"}
    
    %% Post Path
    C -->|Carousel / Post| D["🔍 Windows.Media.Ocr (Images)"]
    
    %% Reel Path
    C -->|Reel Video| E["✂️ ffmpeg Demuxer"]
    E --> F["🎙️ whisper.cpp (Audio STT)"]
    E --> G["🔍 Windows.Media.Ocr (Frames)"]
    
    %% Synthesis
    D --> H["🧠 Ollama LLM (Local Denoising & Synthesis)"]
    F --> H
    G --> H
    
    %% Storage
    H --> I["📝 Markdown Note + YAML Frontmatter"]
    I --> J["📂 Obsidian Vault (<slug>.md)"]
```

### 1. Carousel / Image Post Path
1. **Download**: `yt-dlp` fetches all high-resolution carousel slides.
2. **OCR**: Native `Windows.Media.Ocr` extracts on-screen text, headings, diagrams, and slide quotes.
3. **LLM Distillation**: Raw OCR text is passed to a local Ollama model (e.g. `llama3.2:3b`, `qwen2.5:7b`) to remove boilerplate, correct OCR typos, extract key takeaways, and format clean Markdown.
4. **Vault Output**: The note is formatted with YAML frontmatter (source URL, author, date, tags) and written directly to your configured Obsidian vault.

### 2. Video Reel Path
1. **Download & Demux**: `yt-dlp` downloads the video; `ffmpeg` separates the 16kHz WAV audio and samples video keyframes.
2. **Multimodal Extraction**:
   - `whisper.cpp` runs Speech-to-Text on the audio track.
   - `Windows.Media.Ocr` scans sampled video frames for on-screen text overlays.
3. **Synthesis**: Spoken transcript and visual frame text are combined and synthesized into a structured summary with key points and step-by-step instructions.
4. **Vault Output**: Markdown note is saved with full frontmatter, ready to open in Obsidian via `obsidian://` deep links.

---

## ✨ Key Features

- **🛡️ 100% Local-First & Zero Cloud Costs**: All OCR, audio transcription, LLM distillation, and file writing run entirely on your local machine with zero external cloud subscriptions, API keys, or telemetry.
- **⚡ Zero-Python Distribution Architecture**: Native C# WinUI 3 desktop shell calling standalone native binaries (`yt-dlp`, `ffmpeg`, `whisper.cpp`), native Windows runtime OCR (`Windows.Media.Ocr`), and the local `Ollama` daemon—no brittle Python environments to bundle or break.
- **🎨 Windows 11 Fluent 2 Design**: Clean Mica material backdrop, extended custom title bar, compact `NavigationView`, theme-adaptive color system (Follow System / Dark / Light), and hero spotlight queue.
- **🩺 Live System Health & In-App Asset Manager**: Automatically checks local prerequisites (Vault, Ollama, Whisper, OCR) and provides one-click in-app downloading for missing binaries and GGML Whisper voice models.
- **📂 Seamless Obsidian Integration**: Direct file system writes with collision protection, tags (`#instagram/reel`, `#instagram/post`), clean YAML metadata, and instant `obsidian://` URI launcher support.

---

## 🏗️ Solution Architecture

```text
Distill.sln
├── Distill.App/                 # WinUI 3 Desktop Application (MVVM, XAML, DI Container)
│   ├── Converters/              # Health status, status brushes, visibility converters
│   ├── ViewModels/              # MainViewModel & PipelineJobItemViewModel
│   ├── Views/                   # MainPage.xaml with Fluent 2 shell
│   ├── MainWindow.xaml          # Extended Mica Window with custom titlebar
│   ├── App.xaml / App.xaml.cs   # Dependency Injection container & lifecycle
│   └── appsettings.json         # Local configuration file
│
├── Distill.Core/                # Domain & Core Pipeline (No UI dependencies)
│   ├── Configuration/           # DistillSettings options model
│   ├── Diagnostics/             # ISystemHealthService & tool diagnostics
│   ├── Downloaders/             # IReelDownloader (yt-dlp + ffmpeg)
│   ├── Models/                  # PipelineJob, ExtractedContent, NoteMetadata
│   ├── Ocr/                     # ITextExtractor (Windows.Media.Ocr)
│   ├── Pipeline/                # IPipelineOrchestrator background worker
│   ├── Process/                 # IProcessRunner & IToolLocator
│   ├── SpeechToText/            # ITranscriber (whisper.cpp)
│   ├── Formatting/              # INoteFormatter (Ollama HTTP client)
│   └── VaultWriter/             # IVaultWriter (Obsidian Markdown writer)
│
└── Distill.Tests/               # xUnit Unit Test Suite (61 passing tests)
    ├── SystemHealthServiceTests.cs
    ├── YtDlpReelDownloaderTests.cs
    ├── OnboardingAndSettingsTests.cs
    └── VaultWriterStubTests.cs
```

---

## 🚀 Getting Started (Development)

### Prerequisites

1. **Windows 10 (1809+) or Windows 11**
2. **.NET 8.0 SDK** (with Windows Desktop workload)
3. **Local Tools & Models**:
   - [Ollama](https://ollama.com/) running locally:
     ```bash
     ollama pull llama3.2:3b
     ```
   - [yt-dlp](https://github.com/yt-dlp/yt-dlp) and [ffmpeg](https://ffmpeg.org/) (auto-downloadable in-app or via `scoop install yt-dlp ffmpeg`).
   - [whisper.cpp](https://github.com/ggerganov/whisper.cpp) binary (auto-downloadable in-app).

### Building and Running from Source

```powershell
# Restore & build solution
dotnet build Distill.sln

# Run unit tests
dotnet test

# Run WinUI 3 application
dotnet run --project Distill.App\Distill.App.csproj
```

### Packaging

```powershell
# Builds both Portable ZIP and signed MSIX package
powershell.exe -ExecutionPolicy Bypass -File .\build-packages.ps1
```

---

## 📜 Documentation

Full engineering and design specifications are maintained in the [`docs/`](docs/) directory:

- [Project Overview](docs/project-overview.md) — Product requirements, user flows, and scope.
- [Architecture](docs/architecture.md) — System boundaries, data flow, and invariants.
- [UI Context](docs/ui-context.md) — Fluent Design guidelines, tokens, and layout patterns.
- [Code Standards](docs/code-standards.md) — C# 12/.NET standards and MVVM rules.
- [AI Workflow Rules](docs/ai-workflow-rules.md) — Phased development workflow and scoping boundaries.
- [Progress Tracker](docs/progress-tracker.md) — Current phase, completed work, and roadmap.

---

## 📄 License

MIT License. Feel free to use, modify, and distribute.
