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

        var result = new DownloadResult
        {
            SourceUrl = url,
            Title = "Sample Distilled Post",
            Author = "@creator",
            Caption = "Sample caption extracted from Instagram post/reel.",
            IsReel = url.Contains("/reel/", StringComparison.OrdinalIgnoreCase),
            WorkingDirectory = Path.Combine(Path.GetTempPath(), "Distill", "Stub")
        };

        return Task.FromResult(result);
    }
}
