namespace Distill.Core.Configuration;

/// <summary>
/// Configuration options loaded from appsettings.json and managed through the UI.
/// </summary>
public class DistillSettings
{
    public const string SectionName = "DistillSettings";

    /// <summary>
    /// Path to the Obsidian vault directory or subfolder where distilled notes are saved.
    /// </summary>
    public string VaultFolderPath { get; set; } = string.Empty;

    /// <summary>
    /// The Ollama model tag used for note formatting (e.g., "llama3.2:3b", "qwen2.5:7b").
    /// </summary>
    public string OllamaModelName { get; set; } = "llama3.2:3b";

    /// <summary>
    /// The local Ollama server endpoint URL (default: http://localhost:11434).
    /// </summary>
    public string OllamaEndpoint { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Path to a custom yt-dlp binary (e.g. C:\tools\yt-dlp.exe). If empty, looks in tools/ or PATH.
    /// </summary>
    public string YtDlpBinaryPath { get; set; } = string.Empty;

    /// <summary>
    /// Path to a custom ffmpeg binary (e.g. C:\ffmpeg\bin\ffmpeg.exe). If empty, looks in tools/ or PATH.
    /// </summary>
    public string FfmpegBinaryPath { get; set; } = string.Empty;

    /// <summary>
    /// Path to the whisper.cpp binary (e.g. whisper-cli.exe). If empty, looks in tools/ or PATH.
    /// </summary>
    public string WhisperBinaryPath { get; set; } = string.Empty;

    /// <summary>
    /// Path to the whisper model binary (e.g., "models/ggml-base.en.bin", "models/ggml-small.en.bin").
    /// </summary>
    public string WhisperModelPath { get; set; } = "models/ggml-base.en.bin";

    /// <summary>
    /// Number of CPU threads to allocate for whisper transcription.
    /// </summary>
    public int WhisperThreadCount { get; set; } = 4;

    /// <summary>
    /// Spoken language hint for whisper transcription (default: "en" or "auto").
    /// </summary>
    public string WhisperLanguage { get; set; } = "en";

    /// <summary>
    /// Whether to automatically open distilled notes in Obsidian via URI protocol upon completion.
    /// </summary>
    public bool AutoOpenInObsidian { get; set; } = true;

    /// <summary>
    /// Maximum number of concurrent download and synthesis pipeline jobs.
    /// </summary>
    public int MaxConcurrentJobs { get; set; } = 2;

    /// <summary>
    /// Scene-change detection sensitivity threshold for ffmpeg frame extraction (0.1 to 0.9, default: 0.3).
    /// </summary>
    public double SceneChangeThreshold { get; set; } = 0.3;

    /// <summary>
    /// Whether to append raw OCR text and voice transcript in an expandable details footer inside the generated Markdown note.
    /// </summary>
    public bool AppendRawContentToNote { get; set; } = false;

    /// <summary>
    /// Whether the user has completed the first-launch onboarding guide.
    /// </summary>
    public bool HasCompletedOnboarding { get; set; } = false;

    /// <summary>
    /// Application color theme: "Default" (Follow System), "Dark", or "Light".
    /// </summary>
    public string SelectedTheme { get; set; } = "Default";
}
