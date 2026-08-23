using Distill.Core.Configuration;
using Distill.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Distill.Core.VaultWriter;

/// <summary>
/// Stub implementation of <see cref="IVaultWriter"/> for testing and demonstration.
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
        _logger?.LogInformation("Executing ObsidianVaultWriterStub to: {VaultPath}", _settings.VaultFolderPath);
        var stubPath = Path.Combine(
            string.IsNullOrWhiteSpace(_settings.VaultFolderPath) ? Path.GetTempPath() : _settings.VaultFolderPath,
            $"Distilled_Note_{DateTime.UtcNow:yyyyMMdd_HHmmss}.md");

        return Task.FromResult(stubPath);
    }

    public string BuildObsidianUri(string fullFilePath, string? vaultName = null)
    {
        if (!string.IsNullOrWhiteSpace(vaultName))
        {
            return $"obsidian://open?vault={Uri.EscapeDataString(vaultName)}&file={Uri.EscapeDataString(Path.GetFileName(fullFilePath))}";
        }

        return $"obsidian://open?path={Uri.EscapeDataString(fullFilePath)}";
    }
}
