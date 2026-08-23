using System.Text.Json;
using Distill.Core.Exceptions;
using Distill.Core.Models;
using Distill.Core.Process;
using Microsoft.Extensions.Logging;

namespace Distill.Core.Downloaders;

/// <summary>
/// Downloads Instagram Reels and Posts using yt-dlp and extracts audio/frames with ffmpeg.
/// </summary>
public class YtDlpReelDownloader : IReelDownloader
{
    private readonly IProcessRunner _processRunner;
    private readonly IToolLocator _toolLocator;
    private readonly ILogger<YtDlpReelDownloader>? _logger;

    public YtDlpReelDownloader(
        IProcessRunner processRunner,
        IToolLocator toolLocator,
        ILogger<YtDlpReelDownloader>? logger = null)
    {
        _processRunner = processRunner;
        _toolLocator = toolLocator;
        _logger = logger;
    }

    public async Task<DownloadResult> DownloadAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("Instagram URL cannot be null or empty.", nameof(url));
        }

        var isReel = url.Contains("/reel/", StringComparison.OrdinalIgnoreCase) ||
                     url.Contains("/reels/", StringComparison.OrdinalIgnoreCase);

        var workingDir = Path.Combine(Path.GetTempPath(), "Distill", "Downloads", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDir);

        _logger?.LogInformation("Starting download for URL: {Url} (Type: {Type}) in {Dir}", 
            url, isReel ? "Reel" : "Post", workingDir);

        var ytDlpPath = _toolLocator.ResolveToolPath("yt-dlp.exe");
        var ffmpegPath = _toolLocator.ResolveToolPath("ffmpeg.exe");

        try
        {
            if (isReel)
            {
                return await DownloadReelAsync(url, workingDir, ytDlpPath, ffmpegPath, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await DownloadPostAsync(url, workingDir, ytDlpPath, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not DistillDownloadException)
        {
            _logger?.LogError(ex, "Unexpected failure downloading URL: {Url}", url);
            throw new DistillDownloadException($"Unexpected error occurred during download of '{url}': {ex.Message}", ex);
        }
    }

    private async Task<PostDownloadResult> DownloadPostAsync(
        string url,
        string workingDir,
        string ytDlpPath,
        CancellationToken cancellationToken)
    {
        var outputTemplate = Path.Combine(workingDir, "slide_%(autonumber)03d.%(ext)s");
        var arguments = $"--no-warnings --write-info-json --no-playlist -o \"{outputTemplate}\" \"{url}\"";

        var result = await _processRunner.RunAsync(ytDlpPath, arguments, workingDir, cancellationToken).ConfigureAwait(false);
        EnsureSuccess(result, url);

        var imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
        var imageFiles = Directory.GetFiles(workingDir)
            .Where(f => imageExtensions.Contains(Path.GetExtension(f)))
            .OrderBy(f => f)
            .ToList();

        var (title, author, caption) = ParseMetadataFromWorkingDir(workingDir, url);

        return new PostDownloadResult
        {
            SourceUrl = url,
            WorkingDirectory = workingDir,
            ImageFilePaths = imageFiles,
            Title = title ?? "Instagram Post",
            Author = author,
            Caption = caption
        };
    }

    private async Task<ReelDownloadResult> DownloadReelAsync(
        string url,
        string workingDir,
        string ytDlpPath,
        string ffmpegPath,
        CancellationToken cancellationToken)
    {
        var videoOutputTemplate = Path.Combine(workingDir, "video.%(ext)s");
        var ytDlpArgs = $"--no-warnings --write-info-json --no-playlist -f \"b/bestvideo+bestaudio/best\" -o \"{videoOutputTemplate}\" \"{url}\"";

        var ytResult = await _processRunner.RunAsync(ytDlpPath, ytDlpArgs, workingDir, cancellationToken).ConfigureAwait(false);
        EnsureSuccess(ytResult, url);

        var videoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".mp4", ".mkv", ".webm", ".mov" };
        var videoFile = Directory.GetFiles(workingDir)
            .FirstOrDefault(f => videoExtensions.Contains(Path.GetExtension(f)));

        if (string.IsNullOrEmpty(videoFile))
        {
            throw new DistillDownloadException($"yt-dlp completed successfully, but no video file was found in '{workingDir}'.");
        }

        // 1. Demux 16kHz mono audio (.wav) for speech transcription
        var audioFilePath = Path.Combine(workingDir, "audio.wav");
        var audioArgs = $"-i \"{videoFile}\" -vn -acodec pcm_s16le -ar 16000 -ac 1 -y \"{audioFilePath}\"";
        var audioResult = await _processRunner.RunAsync(ffmpegPath, audioArgs, workingDir, cancellationToken).ConfigureAwait(false);
        EnsureSuccess(audioResult, url, "FFmpeg audio extraction failed");

        // 2. Extract video keyframes
        var framesDir = Path.Combine(workingDir, "frames");
        Directory.CreateDirectory(framesDir);

        var framePattern = Path.Combine(framesDir, "frame_%03d.jpg");
        
        // Try scene-change extraction first
        var sceneArgs = $"-i \"{videoFile}\" -vf \"select='gt(scene,0.3)',showinfo\" -vsync vfr \"{framePattern}\"";
        await _processRunner.RunAsync(ffmpegPath, sceneArgs, workingDir, cancellationToken).ConfigureAwait(false);

        var frameFiles = Directory.GetFiles(framesDir, "*.jpg").OrderBy(f => f).ToList();

        // Fallback: If scene detection extracted no frames, sample every 2 seconds
        if (frameFiles.Count == 0)
        {
            _logger?.LogInformation("Scene detection produced 0 frames; falling back to 2s interval sampling.");
            var fallbackArgs = $"-i \"{videoFile}\" -vf \"fps=1/2\" \"{framePattern}\"";
            var fallbackResult = await _processRunner.RunAsync(ffmpegPath, fallbackArgs, workingDir, cancellationToken).ConfigureAwait(false);
            EnsureSuccess(fallbackResult, url, "FFmpeg frame extraction failed");

            frameFiles = Directory.GetFiles(framesDir, "*.jpg").OrderBy(f => f).ToList();
        }

        var (title, author, caption) = ParseMetadataFromWorkingDir(workingDir, url);

        return new ReelDownloadResult
        {
            SourceUrl = url,
            WorkingDirectory = workingDir,
            VideoFilePath = videoFile,
            AudioFilePath = audioFilePath,
            FrameFilePaths = frameFiles,
            Title = title ?? "Instagram Reel",
            Author = author,
            Caption = caption
        };
    }

    private static void EnsureSuccess(ProcessResult result, string url, string? errorContext = null)
    {
        if (result.Success) return;

        var combinedError = $"{result.StandardError} {result.StandardOutput}".ToLowerInvariant();

        if (combinedError.Contains("login") || combinedError.Contains("private") || 
            combinedError.Contains("requires authentication") || combinedError.Contains("sign in"))
        {
            throw new PrivateMediaException($"Instagram media at '{url}' is private or requires user authentication.\nDetails: {result.StandardError.Trim()}");
        }

        if (combinedError.Contains("429") || combinedError.Contains("rate limit") || 
            combinedError.Contains("too many requests") || combinedError.Contains("temporarily blocked"))
        {
            throw new RateLimitException($"Instagram rate limit or temporary block encountered for '{url}'.\nDetails: {result.StandardError.Trim()}");
        }

        if (combinedError.Contains("404") || combinedError.Contains("not found") || 
            combinedError.Contains("does not exist") || combinedError.Contains("deleted") || 
            combinedError.Contains("unavailable"))
        {
            throw new MediaNotFoundException($"Instagram post or reel at '{url}' could not be found or has been deleted.\nDetails: {result.StandardError.Trim()}");
        }

        var prefix = errorContext != null ? $"{errorContext}: " : string.Empty;
        throw new DistillDownloadException($"{prefix}Process failed with exit code {result.ExitCode}.\n{result.StandardError.Trim()}");
    }

    private (string? Title, string? Author, string? Caption) ParseMetadataFromWorkingDir(string workingDir, string fallbackUrl)
    {
        try
        {
            var infoJsonFile = Directory.GetFiles(workingDir, "*.info.json").FirstOrDefault();
            if (infoJsonFile == null || !File.Exists(infoJsonFile))
            {
                return (null, null, null);
            }

            using var doc = JsonDocument.Parse(File.ReadAllText(infoJsonFile));
            var root = doc.RootElement;

            string? title = root.TryGetProperty("title", out var t) ? t.GetString() : null;
            string? author = root.TryGetProperty("uploader", out var u) ? "@" + u.GetString()?.TrimStart('@') : null;
            string? caption = root.TryGetProperty("description", out var d) ? d.GetString() : null;

            return (title, author, caption);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to parse info.json metadata for {Url}", fallbackUrl);
            return (null, null, null);
        }
    }
}
