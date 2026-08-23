namespace Distill.Core.Models;

/// <summary>
/// Result of downloading an Instagram Post (single image or multi-slide carousel).
/// </summary>
public record PostDownloadResult : DownloadResult
{
    /// <summary>
    /// File paths of all downloaded slide images in order.
    /// </summary>
    public required IReadOnlyList<string> ImageFilePaths { get; init; } = [];
}
