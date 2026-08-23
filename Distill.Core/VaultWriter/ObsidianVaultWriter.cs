using System.Text;
using System.Text.RegularExpressions;
using Distill.Core.Configuration;
using Distill.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Distill.Core.VaultWriter;

/// <summary>
/// Writes distilled notes with YAML frontmatter to a configured Obsidian vault directory.
/// </summary>
public partial class ObsidianVaultWriter : IVaultWriter
{
    private readonly DistillSettings _settings;
    private readonly ILogger<ObsidianVaultWriter>? _logger;

    [GeneratedRegex(@"^#\s+(.+)$", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex TitleHeadingRegex();

    [GeneratedRegex(@"[^\w\d\s\-]", RegexOptions.Compiled)]
    private static partial Regex InvalidSlugCharsRegex();

    [GeneratedRegex(@"[\s\-]+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceAndHyphensRegex();

    public ObsidianVaultWriter(
        IOptions<DistillSettings> settings,
        ILogger<ObsidianVaultWriter>? logger = null)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> WriteNoteAsync(string markdownBody, NoteMetadata metadata, CancellationToken cancellationToken = default)
    {
        if (metadata == null)
        {
            throw new ArgumentNullException(nameof(metadata));
        }

        var vaultFolder = ResolveVaultFolder();
        Directory.CreateDirectory(vaultFolder);

        // 1. Extract note title from markdown or fallback
        var title = ExtractTitle(markdownBody, metadata);

        // 2. Generate filesystem safe filename slug
        var fileSlug = GenerateFileSlug(title, metadata.CapturedAtUtc);

        // 3. Resolve collision-free destination file path
        var destinationPath = ResolveCollisionFreePath(vaultFolder, fileSlug);

        // 4. Prepend YAML frontmatter
        var fullNoteContent = BuildFullNoteWithFrontmatter(markdownBody, metadata);

        // 5. Write note file
        _logger?.LogInformation("Writing distilled note to Obsidian vault: '{FilePath}'", destinationPath);
        await File.WriteAllTextAsync(destinationPath, fullNoteContent, Encoding.UTF8, cancellationToken).ConfigureAwait(false);

        return destinationPath;
    }

    public string BuildObsidianUri(string fullFilePath, string? vaultName = null)
    {
        if (string.IsNullOrWhiteSpace(fullFilePath)) return string.Empty;

        if (!string.IsNullOrWhiteSpace(vaultName))
        {
            var fileName = Path.GetFileName(fullFilePath);
            return $"obsidian://open?vault={Uri.EscapeDataString(vaultName)}&file={Uri.EscapeDataString(fileName)}";
        }

        // Obsidian direct path protocol opens any file in its containing vault automatically
        return $"obsidian://open?path={Uri.EscapeDataString(fullFilePath)}";
    }

    private string ResolveVaultFolder()
    {
        if (!string.IsNullOrWhiteSpace(_settings.VaultFolderPath))
        {
            return Path.GetFullPath(_settings.VaultFolderPath);
        }

        // Fallback default path inside MyDocuments
        var defaultVault = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "ObsidianVault",
            "Instagram");

        return defaultVault;
    }

    private static string ExtractTitle(string markdownBody, NoteMetadata metadata)
    {
        if (!string.IsNullOrWhiteSpace(markdownBody))
        {
            var match = TitleHeadingRegex().Match(markdownBody);
            if (match.Success)
            {
                var candidate = match.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    return candidate;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(metadata.Title))
        {
            return metadata.Title.Trim();
        }

        return "Instagram Note " + metadata.CapturedAtUtc.ToString("yyyy-MM-dd HHmmss");
    }

    private static string GenerateFileSlug(string title, DateTime capturedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return $"instagram-note-{capturedAtUtc:yyyyMMdd-HHmmss}";
        }

        // Strip invalid characters
        var cleaned = InvalidSlugCharsRegex().Replace(title, " ").Trim();
        var slug = WhitespaceAndHyphensRegex().Replace(cleaned, "-").Trim('-');

        if (slug.Length > 70)
        {
            slug = slug[..70].Trim('-');
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            return $"instagram-note-{capturedAtUtc:yyyyMMdd-HHmmss}";
        }

        return slug;
    }

    private static string ResolveCollisionFreePath(string vaultFolder, string slug)
    {
        var targetPath = Path.Combine(vaultFolder, $"{slug}.md");
        if (!File.Exists(targetPath))
        {
            return targetPath;
        }

        var counter = 1;
        while (true)
        {
            var candidate = Path.Combine(vaultFolder, $"{slug}-{counter}.md");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
            counter++;
        }
    }

    private static string BuildFullNoteWithFrontmatter(string markdownBody, NoteMetadata metadata)
    {
        var body = markdownBody ?? string.Empty;

        // If body already has frontmatter, do not double-wrap
        if (body.TrimStart().StartsWith("---"))
        {
            return body;
        }

        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"source: \"{metadata.SourceUrl}\"");
        sb.AppendLine($"type: \"{(metadata.SourceType == SourceType.Reel ? "reel" : "post")}\"");
        sb.AppendLine($"date: {metadata.CapturedAtUtc:yyyy-MM-ddTHH:mm:ssZ}");

        if (!string.IsNullOrWhiteSpace(metadata.Author))
        {
            sb.AppendLine($"author: \"{metadata.Author}\"");
        }

        if (metadata.Tags != null && metadata.Tags.Count > 0)
        {
            sb.AppendLine("tags:");
            foreach (var tag in metadata.Tags)
            {
                var cleanTag = tag.TrimStart('#').Trim();
                if (!string.IsNullOrWhiteSpace(cleanTag))
                {
                    sb.AppendLine($"  - {cleanTag}");
                }
            }
        }
        else
        {
            sb.AppendLine("tags:");
            sb.AppendLine("  - instagram");
            sb.AppendLine("  - distilled");
        }

        sb.AppendLine("---");
        sb.AppendLine();
        sb.Append(body.TrimStart());

        return sb.ToString();
    }
}
