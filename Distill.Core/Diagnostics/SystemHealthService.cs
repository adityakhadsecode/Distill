using System.IO.Compression;
using System.Text.Json;
using Distill.Core.Configuration;
using Distill.Core.Process;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Windows.Media.Ocr;

namespace Distill.Core.Diagnostics;

public class SystemHealthService : ISystemHealthService
{
    private readonly IToolLocator _toolLocator;
    private readonly IProcessRunner _processRunner;
    private readonly HttpClient _httpClient;
    private readonly DistillSettings _settings;
    private readonly ILogger<SystemHealthService>? _logger;

    public SystemHealthService(
        IToolLocator toolLocator,
        IProcessRunner processRunner,
        IOptions<DistillSettings> settings,
        HttpClient? httpClient = null,
        ILogger<SystemHealthService>? logger = null)
    {
        _toolLocator = toolLocator;
        _processRunner = processRunner;
        _settings = settings.Value;
        _httpClient = httpClient ?? new HttpClient();
        _logger = logger;
    }

    public async Task<SystemHealthReport> CheckHealthAsync(string? ollamaEndpoint = null, CancellationToken cancellationToken = default)
    {
        var report = new SystemHealthReport();

        // 1. Check yt-dlp
        try
        {
            var ytdlpPath = _toolLocator.ResolveToolPath("yt-dlp.exe");
            var result = await _processRunner.RunAsync(ytdlpPath, "--version", null, cancellationToken).ConfigureAwait(false);
            if (result.Success)
            {
                report.YtDlp.IsReady = true;
                report.YtDlp.Details = $"Version {result.StandardOutput.Trim()}";
                report.YtDlp.ResolvedPath = ytdlpPath;
            }
            else
            {
                report.YtDlp.IsReady = false;
                report.YtDlp.Details = "Found, but failed to run";
                report.YtDlp.ResolvedPath = ytdlpPath;
            }
        }
        catch (Exception ex)
        {
            report.YtDlp.IsReady = false;
            report.YtDlp.Details = "Not found (missing in /tools or PATH)";
            _logger?.LogDebug(ex, "yt-dlp health check failed.");
        }

        // 2. Check ffmpeg
        try
        {
            var ffmpegPath = _toolLocator.ResolveToolPath("ffmpeg.exe");
            var result = await _processRunner.RunAsync(ffmpegPath, "-version", null, cancellationToken).ConfigureAwait(false);
            if (result.Success)
            {
                report.Ffmpeg.IsReady = true;
                report.Ffmpeg.Details = "Installed & ready";
                report.Ffmpeg.ResolvedPath = ffmpegPath;
            }
            else
            {
                report.Ffmpeg.IsReady = false;
                report.Ffmpeg.Details = "Found, but failed to run";
                report.Ffmpeg.ResolvedPath = ffmpegPath;
            }
        }
        catch (Exception ex)
        {
            report.Ffmpeg.IsReady = false;
            report.Ffmpeg.Details = "Not found (missing in /tools or PATH)";
            _logger?.LogDebug(ex, "ffmpeg health check failed.");
        }

        // 3. Check whisper.cpp
        try
        {
            var whisperPath = string.IsNullOrWhiteSpace(_settings.WhisperBinaryPath)
                ? _toolLocator.ResolveToolPath("whisper-cli.exe")
                : _settings.WhisperBinaryPath;

            var result = await _processRunner.RunAsync(whisperPath, "-h", null, cancellationToken).ConfigureAwait(false);
            if (result.ExitCode == 0 || result.StandardError.Contains("usage", StringComparison.OrdinalIgnoreCase) || result.StandardOutput.Contains("usage", StringComparison.OrdinalIgnoreCase))
            {
                report.Whisper.IsReady = true;
                report.Whisper.Details = "Ready";
                report.Whisper.ResolvedPath = whisperPath;
            }
            else
            {
                report.Whisper.IsReady = true;
                report.Whisper.Details = "Found";
                report.Whisper.ResolvedPath = whisperPath;
            }
        }
        catch (Exception ex)
        {
            report.Whisper.IsReady = false;
            report.Whisper.Details = "Not found (missing whisper-cli.exe)";
            _logger?.LogDebug(ex, "whisper health check failed.");
        }

        // 4. Check Windows Native OCR
        try
        {
            var languages = OcrEngine.AvailableRecognizerLanguages;
            if (languages != null && languages.Count > 0)
            {
                report.WindowsOcr.IsReady = true;
                var langTags = string.Join(", ", languages.Select(l => l.LanguageTag));
                report.WindowsOcr.Details = $"{languages.Count} language(s) installed ({langTags})";
            }
            else
            {
                report.WindowsOcr.IsReady = false;
                report.WindowsOcr.Details = "No OCR language pack installed in Windows settings";
            }
        }
        catch (Exception ex)
        {
            report.WindowsOcr.IsReady = false;
            report.WindowsOcr.Details = $"OCR initialization error: {ex.Message}";
        }

        // 5. Check Ollama Connectivity
        var targetEndpoint = !string.IsNullOrWhiteSpace(ollamaEndpoint) ? ollamaEndpoint : _settings.OllamaEndpoint;
        try
        {
            var url = $"{targetEndpoint.TrimEnd('/')}/api/tags";
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
            using var response = await _httpClient.GetAsync(url, linked.Token).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(linked.Token).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(content);
                var modelsCount = doc.RootElement.TryGetProperty("models", out var modelsProp) && modelsProp.ValueKind == JsonValueKind.Array
                    ? modelsProp.GetArrayLength()
                    : 0;

                report.Ollama.IsReady = true;
                report.Ollama.Details = $"Online ({modelsCount} model(s) available)";
                report.Ollama.ResolvedPath = targetEndpoint;
            }
            else
            {
                report.Ollama.IsReady = false;
                report.Ollama.Details = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
            }
        }
        catch (Exception ex)
        {
            report.Ollama.IsReady = false;
            report.Ollama.Details = "Offline (Ollama service not running)";
            _logger?.LogDebug(ex, "Ollama connection check failed.");
        }

        return report;
    }

    public async Task<IReadOnlyList<string>> GetInstalledOllamaModelsAsync(string? endpoint = null, CancellationToken cancellationToken = default)
    {
        var targetEndpoint = !string.IsNullOrWhiteSpace(endpoint) ? endpoint : _settings.OllamaEndpoint;
        var url = $"{targetEndpoint.TrimEnd('/')}/api/tags";

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
            using var response = await _httpClient.GetAsync(url, linked.Token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<string>();
            }

            var content = await response.Content.ReadAsStringAsync(linked.Token).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            var modelNames = new List<string>();
            if (doc.RootElement.TryGetProperty("models", out var modelsProp) && modelsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var model in modelsProp.EnumerateArray())
                {
                    if (model.TryGetProperty("name", out var nameProp) && nameProp.GetString() is { } name && !string.IsNullOrWhiteSpace(name))
                    {
                        modelNames.Add(name);
                    }
                }
            }

            return modelNames;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to query installed Ollama models from {Endpoint}", targetEndpoint);
            return Array.Empty<string>();
        }
    }

    public async Task<string> DownloadWhisperModelAsync(string modelFileName, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        var baseDir = AppContext.BaseDirectory;
        var modelsDir = Path.Combine(baseDir, "tools", "models");
        Directory.CreateDirectory(modelsDir);

        var destinationPath = Path.Combine(modelsDir, modelFileName);
        var url = $"https://huggingface.co/ggerganov/whisper.cpp/resolve/main/{modelFileName}";

        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

        var buffer = new byte[81920];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
            totalRead += bytesRead;

            if (totalBytes > 0 && progress != null)
            {
                var percentage = (double)totalRead / totalBytes * 100.0;
                progress.Report(percentage);
            }
        }

        return destinationPath;
    }

    public async Task DownloadMissingToolsAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        var toolsDir = Path.Combine(AppContext.BaseDirectory, "tools");
        Directory.CreateDirectory(toolsDir);

        // 1. yt-dlp.exe
        var ytdlpPath = Path.Combine(toolsDir, "yt-dlp.exe");
        if (!File.Exists(ytdlpPath))
        {
            progress?.Report("Downloading yt-dlp.exe...");
            using var resp = await _httpClient.GetAsync("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe", cancellationToken).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            await using var fs = File.Create(ytdlpPath);
            await resp.Content.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
            progress?.Report("Downloaded yt-dlp.exe.");
        }

        // 2. ffmpeg.exe
        var ffmpegPath = Path.Combine(toolsDir, "ffmpeg.exe");
        if (!File.Exists(ffmpegPath))
        {
            progress?.Report("Downloading ffmpeg...");
            var tempZip = Path.Combine(Path.GetTempPath(), $"ffmpeg_{Guid.NewGuid():N}.zip");
            var tempExtract = Path.Combine(Path.GetTempPath(), $"ffmpeg_ext_{Guid.NewGuid():N}");

            try
            {
                using var resp = await _httpClient.GetAsync("https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip", cancellationToken).ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();
                await using (var fs = File.Create(tempZip))
                {
                    await resp.Content.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
                }

                progress?.Report("Extracting ffmpeg.exe...");
                ZipFile.ExtractToDirectory(tempZip, tempExtract, true);

                var found = Directory.GetFiles(tempExtract, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();
                if (found != null)
                {
                    File.Copy(found, ffmpegPath, true);
                    progress?.Report("Configured ffmpeg.exe.");
                }
            }
            finally
            {
                if (File.Exists(tempZip)) File.Delete(tempZip);
                if (Directory.Exists(tempExtract)) Directory.Delete(tempExtract, true);
            }
        }

        // 3. whisper-cli.exe
        var whisperPath = Path.Combine(toolsDir, "whisper-cli.exe");
        if (!File.Exists(whisperPath))
        {
            progress?.Report("Downloading whisper.cpp binaries...");
            var tempZip = Path.Combine(Path.GetTempPath(), $"whisper_{Guid.NewGuid():N}.zip");
            var tempExtract = Path.Combine(Path.GetTempPath(), $"whisper_ext_{Guid.NewGuid():N}");

            try
            {
                using var resp = await _httpClient.GetAsync("https://github.com/ggerganov/whisper.cpp/releases/latest/download/whisper-bin-x64.zip", cancellationToken).ConfigureAwait(false);
                if (resp.IsSuccessStatusCode)
                {
                    await using (var fs = File.Create(tempZip))
                    {
                        await resp.Content.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
                    }

                    ZipFile.ExtractToDirectory(tempZip, tempExtract, true);
                    var found = Directory.GetFiles(tempExtract, "whisper-cli.exe", SearchOption.AllDirectories).FirstOrDefault()
                                ?? Directory.GetFiles(tempExtract, "main.exe", SearchOption.AllDirectories).FirstOrDefault();

                    if (found != null)
                    {
                        File.Copy(found, whisperPath, true);
                    }

                    foreach (var dll in Directory.GetFiles(tempExtract, "*.dll", SearchOption.AllDirectories))
                    {
                        File.Copy(dll, Path.Combine(toolsDir, Path.GetFileName(dll)), true);
                    }
                    progress?.Report("Configured whisper.cpp.");
                }
            }
            finally
            {
                if (File.Exists(tempZip)) File.Delete(tempZip);
                if (Directory.Exists(tempExtract)) Directory.Delete(tempExtract, true);
            }
        }

        progress?.Report("All tools are up to date!");
    }
}
