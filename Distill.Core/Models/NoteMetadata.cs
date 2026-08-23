namespace Distill.Core.Models;

/// <summary>
/// Metadata attributes serialized into Obsidian YAML frontmatter.
/// </summary>
public record NoteMetadata
{
    public required string Title { get; init; }
    public required string SourceUrl { get; init; }
    public string? Author { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string MediaType { get; init; } = "instagram_reel"; // or "instagram_post"
    public IReadOnlyList<string> Tags { get; init; } = ["instagram", "knowledge-extraction"];
    public string? Summary { get; init; }
}
