namespace Distill.Core.Models;

/// <summary>
/// The source type of the extracted Instagram media.
/// </summary>
public enum SourceType
{
    Post,
    Reel
}

/// <summary>
/// Raw extracted multimodal content from an Instagram post or reel prepared for AI distillation.
/// </summary>
public record RawExtractedContent
{
    public required string SourceUrl { get; init; }
    public SourceType SourceType { get; init; } = SourceType.Post;
    public IReadOnlyList<string> OcrTextSegments { get; init; } = [];
    public string OcrText => string.Join(Environment.NewLine, OcrTextSegments);
    public string? TranscriptText { get; init; }
    public string? Caption { get; init; }
    public string? Title { get; init; }
    public string? Author { get; init; }
}
