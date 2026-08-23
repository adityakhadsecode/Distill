namespace Distill.Core.Diagnostics;

public class ToolHealthItem
{
    public string Name { get; set; } = string.Empty;
    public bool IsReady { get; set; }
    public string Details { get; set; } = string.Empty;
    public string? ResolvedPath { get; set; }
}

public class SystemHealthReport
{
    public ToolHealthItem YtDlp { get; set; } = new() { Name = "yt-dlp (Downloader)" };
    public ToolHealthItem Ffmpeg { get; set; } = new() { Name = "ffmpeg (Demux & Frames)" };
    public ToolHealthItem Whisper { get; set; } = new() { Name = "whisper.cpp (Speech-to-Text)" };
    public ToolHealthItem WindowsOcr { get; set; } = new() { Name = "Windows OCR (Text Extraction)" };
    public ToolHealthItem Ollama { get; set; } = new() { Name = "Ollama (Local LLM)" };

    public bool AllPipelineToolsReady => YtDlp.IsReady && Ffmpeg.IsReady && Whisper.IsReady && WindowsOcr.IsReady;
}

public interface ISystemHealthService
{
    /// <summary>
    /// Performs a full health inspection across all external binaries, OCR capabilities, and Ollama connectivity.
    /// </summary>
    Task<SystemHealthReport> CheckHealthAsync(string? ollamaEndpoint = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the local Ollama daemon for currently installed models.
    /// </summary>
    Task<IReadOnlyList<string>> GetInstalledOllamaModelsAsync(string? endpoint = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a pre-configured Whisper GGML model from the official HuggingFace repository directly into the tools/models directory.
    /// </summary>
    Task<string> DownloadWhisperModelAsync(string modelFileName, IProgress<double>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads and configures any missing local tools (yt-dlp, ffmpeg, whisper-cli) in one shot.
    /// </summary>
    Task DownloadMissingToolsAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default);
}
