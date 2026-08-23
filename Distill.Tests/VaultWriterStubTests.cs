using Distill.Core.Configuration;
using Distill.Core.Models;
using Distill.Core.VaultWriter;
using Microsoft.Extensions.Options;
using Xunit;

namespace Distill.Tests;

public class VaultWriterStubTests : IDisposable
{
    private readonly string _testVaultDir;

    public VaultWriterStubTests()
    {
        _testVaultDir = Path.Combine(Path.GetTempPath(), "DistillTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testVaultDir);
    }

    [Fact]
    public async Task ObsidianVaultWriterStub_ReturnsValidFilePath()
    {
        // Arrange
        var settings = Options.Create(new DistillSettings
        {
            VaultFolderPath = _testVaultDir
        });
        IVaultWriter writer = new ObsidianVaultWriterStub(settings);

        var metadata = new NoteMetadata
        {
            Title = "Obsidian Capture Note",
            SourceUrl = "https://instagram.com/reel/abc987",
            Author = "@educator",
            SourceType = SourceType.Reel,
            Tags = ["tag1", "tag2"]
        };

        const string markdownBody = "## Key Learnings\n- Point A\n- Point B";

        // Act
        var filePath = await writer.WriteNoteAsync(markdownBody, metadata);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(filePath));
        Assert.StartsWith(_testVaultDir, filePath);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testVaultDir))
            {
                Directory.Delete(_testVaultDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors during test teardown
        }
    }
}
