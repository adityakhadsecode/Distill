using System.Text.Json;
using Distill.Core.Exceptions;
using Distill.Core.Models;
using Distill.Core.Process;
using Microsoft.Extensions.Logging;

namespace Distill.Core.Downloaders;

/// <summary>
/// Downloads Instagram Reels and Posts using yt-dlp, direct HTTP image fetching, and extracts audio/frames with ffmpeg.
/// </summary>
public class YtDlpReelDownloader : IReelDownloader
{
    private readonly IProcessRunner _processRunner;
    private readonly IToolLocator _toolLocator;
    private readonly HttpClient _httpClient;
    private readonly ILogger<YtDlpReelDownloader>? _logger;

    public YtDlpReelDownloader(
        IProcessRunner processRunner,
        IToolLocator toolLocator,
        HttpClient? httpClient = null,
        ILogger<YtDlpReelDownloader>? logger = null)
    {
        _processRunner = processRunner;
        _toolLocator = toolLocator;
        _httpClient = httpClient ?? new HttpClient();
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
                return await DownloadPostAsync(url, workingDir, ytDlpPath, ffmpegPath, cancellationToken).ConfigureAwait(false);
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
        string ffmpegPath,
        CancellationToken cancellationToken)
    {
        // 1. Run yt-dlp metadata-only extraction (--dump-single-json)
        var arguments = $"--dump-single-json --no-warnings \"{url}\"";
        var result = await _processRunner.RunAsync(ytDlpPath, arguments, workingDir, cancellationToken).ConfigureAwait(false);
        EnsureSuccess(result, url);

        using var doc = JsonDocument.Parse(result.StandardOutput);
        var root = doc.RootElement;

        var title = root.TryGetProperty("title", out var t) ? t.GetString() : null;
        var author = root.TryGetProperty("uploader", out var u) ? "@" + u.GetString()?.TrimStart('@') : null;
        var caption = root.TryGetProperty("description", out var d) ? d.GetString() : null;

        var imageFilePaths = new List<string>();

        // 2 & 3. Process entries if carousel, otherwise process root object
        var entries = new List<JsonElement>();
        if (root.TryGetProperty("entries", out var entriesProp) && entriesProp.ValueKind == JsonValueKind.Array)
        {
            entries.AddRange(entriesProp.EnumerateArray());
        }
        else
        {
            entries.Add(root);
        }

        var slideIndex = 1;
        foreach (var entry in entries)
        {
            if (IsVideoEntry(entry))
            {
                // Video slide in carousel: download video via yt-dlp and extract representative frame via ffmpeg
                var slideUrl = entry.TryGetProperty("webpage_url", out var wp) ? wp.GetString() : null;
                if (string.IsNullOrWhiteSpace(slideUrl))
                {
                    slideUrl = entry.TryGetProperty("url", out var entryUrl) ? entryUrl.GetString() : url;
                }

                var videoFile = Path.Combine(workingDir, $"slide_{slideIndex:D3}.mp4");
                var videoArgs = $"--no-warnings --no-playlist -f \"b/bestvideo+bestaudio/best\" -o \"{videoFile}\" \"{slideUrl}\"";
                var videoResult = await _processRunner.RunAsync(ytDlpPath, videoArgs, workingDir, cancellationToken).ConfigureAwait(false);
                EnsureSuccess(videoResult, slideUrl ?? url);

                var framePath = Path.Combine(workingDir, $"slide_{slideIndex:D3}.jpg");
                var frameArgs = $"-i \"{videoFile}\" -vframes 1 -y \"{framePath}\"";
                await _processRunner.RunAsync(ffmpegPath, frameArgs, workingDir, cancellationToken).ConfigureAwait(false);

                if (File.Exists(framePath))
                {
                    imageFilePaths.Add(framePath);
                }
                else if (File.Exists(videoFile))
                {
                    imageFilePaths.Add(videoFile);
                }
            }
            else
            {
                // Image slide: download highest-resolution thumbnail directly via HTTP
                var bestImageUrl = GetBestImageUrl(entry);
                if (!string.IsNullOrWhiteSpace(bestImageUrl))
                {
                    var imagePath = Path.Combine(workingDir, $"slide_{slideIndex:D3}.jpg");
                    using var response = await _httpClient.GetAsync(bestImageUrl, cancellationToken).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();

                    await using var fileStream = File.Create(imagePath);
                    await response.Content.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
                    imageFilePaths.Add(imagePath);
                }
            }

            slideIndex++;
        }

        return new PostDownloadResult
        {
            SourceUrl = url,
            WorkingDirectory = workingDir,
            ImageFilePaths = imageFilePaths.OrderBy(f => f).ToList(),
            Title = title ?? "Instagram Post",
            Author = author,
            Caption = caption
        };
    }

    private static bool IsVideoEntry(JsonElement entry)
    {
        if (entry.TryGetProperty("formats", out var formatsProp) && formatsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var fmt in formatsProp.EnumerateArray())
            {
                if (fmt.TryGetProperty("vcodec", out var vcodecProp))
                {
                    var vcodec = vcodecProp.GetString();
                    if (!string.IsNullOrWhiteSpace(vcodec) && !vcodec.Equals("none", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static string? GetBestImageUrl(JsonElement entry)
    {
        // 1. Search thumbnails array for highest resolution
        if (entry.TryGetProperty("thumbnails", out var thumbsProp) && thumbsProp.ValueKind == JsonValueKind.Array)
        {
            string? bestUrl = null;
            var maxDimension = -1;

            foreach (var thumb in thumbsProp.EnumerateArray())
            {
                var thumbUrl = thumb.TryGetProperty("url", out var u) ? u.GetString() : null;
                if (string.IsNullOrWhiteSpace(thumbUrl)) continue;

                var width = thumb.TryGetProperty("width", out var w) && w.TryGetInt32(out var widthVal) ? widthVal : 0;
                var height = thumb.TryGetProperty("height", out var h) && h.TryGetInt32(out var heightVal) ? heightVal : 0;
                var dimension = width * height;

                if (dimension > maxDimension || bestUrl == null)
                {
                    maxDimension = dimension;
                    bestUrl = thumbUrl;
                }
            }

            if (!string.IsNullOrWhiteSpace(bestUrl))
            {
                return bestUrl;
            }
        }

        // 2. Fallback to entry "url" if thumbnail array not present
        if (entry.TryGetProperty("url", out var urlProp) && urlProp.GetString() is { } directUrl && !string.IsNullOrWhiteSpace(directUrl))
        {
            return directUrl;
        }

        return null;
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
