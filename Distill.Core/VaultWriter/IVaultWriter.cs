using Distill.Core.Models;

namespace Distill.Core.VaultWriter;

/// <summary>
/// Writes markdown note content and YAML frontmatter to a configured Obsidian vault folder.
/// </summary>
public interface IVaultWriter
{
    /// <summary>
    /// Writes the note to the configured Obsidian vault.
    /// </summary>
    /// <param name="markdownContent">The distilled Markdown body.</param>
    /// <param name="metadata">The metadata to format into YAML frontmatter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The absolute file path of the created Markdown note.</returns>
    Task<string> WriteNoteAsync(string markdownContent, NoteMetadata metadata, CancellationToken cancellationToken = default);
}
