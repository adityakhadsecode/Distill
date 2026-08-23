namespace Distill.Core.Models;

/// <summary>
/// Metadata attributes serialized into Obsidian YAML frontmatter.
/// </summary>
public record NoteMetadata
{
    public required string SourceUrl { get; init; }
    public SourceType SourceType { get; init; } = SourceType.Post;
    public DateTime CapturedAtUtc { get; init; } = DateTime.UtcNow;
    public IReadOnlyList<string> Tags { get; init; } = ["instagram", "knowledge-extraction"];
    public string? Title { get; init; }
    public string? Author { get; init; }
    public string? Summary { get; init; }
}
