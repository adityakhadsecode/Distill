using Distill.Core.Models;

namespace Distill.Core.VaultWriter;

/// <summary>
/// Writes markdown note content and YAML frontmatter to a configured Obsidian vault folder.
/// </summary>
public interface IVaultWriter
{
    /// <summary>
    /// Writes the note to the configured Obsidian vault with YAML frontmatter.
    /// </summary>
    /// <param name="markdownBody">The distilled Markdown body.</param>
    /// <param name="metadata">The metadata to format into YAML frontmatter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The absolute file path of the created Markdown note.</returns>
    Task<string> WriteNoteAsync(string markdownBody, NoteMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Constructs an obsidian:// deep-link URI to open the created note inside Obsidian.
    /// </summary>
    /// <param name="fullFilePath">Absolute path to the created .md file.</param>
    /// <param name="vaultName">Optional vault name to target explicitly.</param>
    /// <returns>The obsidian:// URI string.</returns>
    string BuildObsidianUri(string fullFilePath, string? vaultName = null);
}
