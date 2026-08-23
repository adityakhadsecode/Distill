namespace Distill.Core.Models;

/// <summary>
/// Represents the downloaded media and raw metadata retrieved from Instagram.
/// </summary>
public record DownloadResult
{
    public required string SourceUrl { get; init; }
    public string? Title { get; init; }
    public string? Author { get; init; }
    public string? Caption { get; init; }
    public bool IsReel { get; init; }
    public string? VideoFilePath { get; init; }
    public string? AudioFilePath { get; init; }
    public IReadOnlyList<string> ImageFilePaths { get; init; } = [];
    public string WorkingDirectory { get; init; } = string.Empty;
}
