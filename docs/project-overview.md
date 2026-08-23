# Distill — Local-First Instagram → Obsidian Knowledge Extraction

## Overview

**Distill** is a native Windows application built with WinUI 3 and .NET designed to seamlessly convert Instagram content (single/carousel image posts and video Reels) into clean, structured, and richly formatted Markdown notes saved directly into a local Obsidian vault.

Distill solves the problem of friction and digital hoarding when saving insightful Instagram posts and educational reels. Instead of losing posts in Instagram's "Saved" bookmark abyss, Distill downloads the media locally, performs local Optical Character Recognition (OCR) and Speech-to-Text (STT) transcription, passes the extracted content to a local LLM (via Ollama) for synthesis and denoising, and writes structured Markdown files with complete YAML frontmatter into Obsidian with zero cloud dependencies.

---

## Goals

1. **Effortless Knowledge Capture**: Extract and synthesize knowledge from Instagram post/reel URLs with single-click manual entry or automatic clipboard detection.
2. **100% Local-First Privacy & Security**: Run all processing—media extraction, audio demuxing, frame OCR, audio STT transcription, and LLM formatting—entirely on the local machine with zero external cloud subscriptions or telemetry.
3. **Seamless Obsidian Integration**: Write standard Markdown notes directly to the user's Obsidian vault directory with rich YAML frontmatter (source URL, author, timestamp, tags, media type) and instant deep-linking via `obsidian://` URIs.
4. **Clean Native Performance & Zero-Python Runtime**: Rely on a high-performance native C# WinUI 3 desktop shell, native `Windows.Media.Ocr` API, standalone `whisper.cpp` engine, standalone `yt-dlp` & `ffmpeg` binaries, and local `Ollama` daemon—avoiding cumbersome Python environment bundling.
5. **Portability-Ready Architecture**: Decouple the core extraction and distillation pipeline from the UI presentation layer to facilitate a future Android companion app or CLI tool.

---

## Core User Flows

### 1. Instagram Carousel / Image Post Flow
1. **Input**: User pastes an Instagram post URL (or Distill auto-detects it from clipboard).
2. **Download**: `yt-dlp` downloads all slide images to a local temporary workspace.
3. **OCR Processing**: Native `Windows.Media.Ocr` scans each slide in order and extracts on-screen text, headings, diagrams, and captions.
4. **LLM Distillation**: Raw OCR text and post captions are sent to local Ollama (e.g. `llama3.2`, `qwen2.5:7b`), which strips boilerplate/promo noise, corrects OCR typos, identifies key takeaways, and formats into clean Markdown.
5. **Vault Output**: Distill formats YAML frontmatter and writes `<VaultPath>/<Folder>/<Slug>.md`.
6. **Interaction**: Live progress updates throughout, with a quick action to open the note directly in Obsidian.

### 2. Instagram Reel Flow
1. **Input**: User submits an Instagram Reel URL.
2. **Download & Demux**: `yt-dlp` downloads the MP4 video. `ffmpeg` demuxes the 16kHz mono audio track and samples video frames (at fixed intervals or scene changes).
3. **STT & OCR Extraction**:
   - `whisper.cpp` transcribes spoken audio with timestamps into text.
   - `Windows.Media.Ocr` extracts on-screen text, titles, and slide overlays from sampled frames.
4. **Multimodal Synthesis via LLM**: Spoken transcript and visual frame text are combined and fed to Ollama to create a cohesive summary, step-by-step breakdown, quotes, and categorized bullet points.
5. **Vault Output**: Markdown note is saved into the Obsidian vault with tags `#instagram/reel`, source URL, author metadata, and key takeaways.

### 3. Queue & Settings Configuration Flow
1. User configures Obsidian Vault path and default destination folder (e.g. `Sources/Instagram/`).
2. User selects preferred local Ollama model and Whisper model (e.g. `base.en`, `small`, `medium`).
3. User queues multiple URLs; Distill processes them sequentially with live stage updates and error handling.

---

## Features

### Ingestion & Queue Management
- Single URL input with instant URL validation (Post vs Reel detection).
- Automatic clipboard watcher toggle (detects Instagram links and offers 1-click ingest).
- Multi-job queue with status indicators (Queued, Downloading, Demuxing, OCR/STT, Distilling, Complete, Failed).

### Media Processing & Extraction
- `yt-dlp` execution wrapper for robust media and metadata retrieval.
- `ffmpeg` pipeline for audio extraction (`.wav`) and deduplicated frame sampling.
- Native Windows OCR (`Windows.Media.Ocr`) for slide and video frame text extraction.
- Native `whisper.cpp` integration for fast, GPU/CPU-accelerated speech-to-text without Python dependencies.

### Local AI Synthesis & Formatting
- Local LLM prompt templates via Ollama HTTP API.
- Intelligent denoising (removes "Link in bio", "Follow for more", ad copy, sponsored tags).
- Auto-tagging based on content analysis (e.g., `#coding`, `#design`, `#productivity`, `#fitness`).
- Configurable Markdown output templates (Summary, Key Points, Step-by-Step, Full Transcript, Visual Notes).

### Obsidian Vault Integration
- Direct filesystem file writer with collision handling.
- Full YAML frontmatter support (title, date, original_url, author, type, tags, media_count).
- `obsidian://open?vault=...&file=...` URI execution to jump straight into Obsidian.

---

## Scope

### In Scope
- Windows 10/11 desktop application using WinUI 3 and .NET 8/9.
- Instagram single photo posts, multi-image carousel posts, and video Reels.
- Local OCR via Windows Runtime `Windows.Media.Ocr`.
- Local STT via `whisper.cpp` CLI / DLL.
- Local LLM distillation via Ollama REST API (`http://localhost:11434`).
- Local temporary storage management & automatic artifact cleanup.
- Configurable Obsidian Vault destination and customizable markdown templates.

### Out of Scope (Initial Phase)
- Cloud-hosted SaaS / sync servers (strictly local-first).
- Direct private Instagram scraping without user cookies (relies on public URLs or user-provided browser cookies via `yt-dlp`).
- Full Android build (architecture will be decoupled, but Android UI is deferred to future phase).
- Video player / heavy media editing tools inside the app (focus is knowledge extraction).

---

## Success Criteria

1. **Complete Extraction Pipeline**: A user can paste any public Instagram carousel post or Reel URL and receive a structured Markdown file in their designated Obsidian vault within seconds.
2. **Local-First Independence**: The application operates with zero external cloud API tokens or remote services; all inference runs against local Ollama, Windows.Media.Ocr, and whisper.cpp.
3. **High Quality Markdown**: Distilled notes have properly formatted frontmatter, clear headings, concise bullet points, and extracted code/quotes without OCR or speech noise.
4. **Reliable Error Handling**: If a network error, missing dependency, or invalid URL occurs, the app provides actionable UI feedback without crashing.
