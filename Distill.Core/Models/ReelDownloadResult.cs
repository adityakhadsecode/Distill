namespace Distill.Core.Models;

/// <summary>
/// Result of downloading and demuxing an Instagram Reel video.
/// </summary>
public record ReelDownloadResult : DownloadResult
{
    /// <summary>
    /// File path of the downloaded full video file (MP4).
    /// </summary>
    public required string VideoFilePath { get; init; }

    /// <summary>
    /// File path of the extracted 16kHz mono WAV audio track.
    /// </summary>
    public required string AudioFilePath { get; init; }

    /// <summary>
    /// Ordered file paths of the extracted video keyframes (JPG).
    /// </summary>
    public required IReadOnlyList<string> FrameFilePaths { get; init; } = [];
}
