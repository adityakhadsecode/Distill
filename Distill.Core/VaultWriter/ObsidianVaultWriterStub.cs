using System.Text;
using Distill.Core.Configuration;
using Distill.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Distill.Core.VaultWriter;

/// <summary>
/// Stub implementation of <see cref="IVaultWriter"/> for Obsidian note generation.
/// </summary>
public class ObsidianVaultWriterStub : IVaultWriter
{
    private readonly DistillSettings _settings;
    private readonly ILogger<ObsidianVaultWriterStub>? _logger;

    public ObsidianVaultWriterStub(IOptions<DistillSettings> settings, ILogger<ObsidianVaultWriterStub>? logger = null)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public Task<string> WriteNoteAsync(string markdownContent, NoteMetadata metadata, CancellationToken cancellationToken = default)
    {
        var targetDir = string.IsNullOrWhiteSpace(_settings.VaultFolderPath)
            ? Path.Combine(Path.GetTempPath(), "Distill", "Vault")
            : _settings.VaultFolderPath;

        Directory.CreateDirectory(targetDir);

        var safeSlug = string.Join("_", metadata.Title.Split(Path.GetInvalidFileNameChars()));
        var filePath = Path.Combine(targetDir, $"{metadata.CreatedAt:yyyy-MM-dd}-{safeSlug}.md");

        _logger?.LogInformation("Writing Obsidian note to {FilePath}", filePath);

        var fullNote = new StringBuilder();
        fullNote.AppendLine("---");
        fullNote.AppendLine($"title: \"{metadata.Title}\"");
        fullNote.AppendLine($"date: {metadata.CreatedAt:yyyy-MM-ddTHH:mm:ss}");
        fullNote.AppendLine($"source_url: \"{metadata.SourceUrl}\"");
        fullNote.AppendLine($"author: \"{metadata.Author ?? "unknown"}\"");
        fullNote.AppendLine($"type: \"{metadata.MediaType}\"");
        fullNote.AppendLine("tags:");
        foreach (var tag in metadata.Tags)
        {
            fullNote.AppendLine($"  - {tag}");
        }
        fullNote.AppendLine("---");
        fullNote.AppendLine();
        fullNote.Append(markdownContent);

        File.WriteAllText(filePath, fullNote.ToString(), Encoding.UTF8);

        return Task.FromResult(filePath);
    }
}
