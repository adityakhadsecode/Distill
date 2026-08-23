using Distill.Core.Models;
using Microsoft.Extensions.Logging;

namespace Distill.Core.Downloaders;

/// <summary>
/// Stub implementation of <see cref="IReelDownloader"/>.
/// </summary>
public class ReelDownloaderStub : IReelDownloader
{
    private readonly ILogger<ReelDownloaderStub>? _logger;

    public ReelDownloaderStub(ILogger<ReelDownloaderStub>? logger = null)
    {
        _logger = logger;
    }

    public Task<DownloadResult> DownloadAsync(string url, CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Executing ReelDownloaderStub for URL: {Url}", url);

        var tempDir = Path.Combine(Path.GetTempPath(), "Distill", "Stub_" + Guid.NewGuid().ToString("N"));
        var isReel = url.Contains("/reel/", StringComparison.OrdinalIgnoreCase) || 
                     url.Contains("/reels/", StringComparison.OrdinalIgnoreCase);

        if (isReel)
        {
            var reelResult = new ReelDownloadResult
            {
                SourceUrl = url,
                Title = "Sample Reel Distillation",
                Author = "@creator",
                Caption = "Sample educational reel caption.",
                WorkingDirectory = tempDir,
                VideoFilePath = Path.Combine(tempDir, "video.mp4"),
                AudioFilePath = Path.Combine(tempDir, "audio.wav"),
                FrameFilePaths = [Path.Combine(tempDir, "frames", "frame_001.jpg")]
            };
            return Task.FromResult<DownloadResult>(reelResult);
        }
        else
        {
            var postResult = new PostDownloadResult
            {
                SourceUrl = url,
                Title = "Sample Carousel Post Distillation",
                Author = "@creator",
                Caption = "Sample educational carousel post caption.",
                WorkingDirectory = tempDir,
                ImageFilePaths = [Path.Combine(tempDir, "slide_001.jpg")]
            };
            return Task.FromResult<DownloadResult>(postResult);
        }
    }
}
