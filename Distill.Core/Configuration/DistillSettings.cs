namespace Distill.Core.Configuration;

/// <summary>
/// Configuration options loaded from appsettings.json.
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
}
