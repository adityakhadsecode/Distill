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
    public async Task ObsidianVaultWriterStub_WritesMarkdownWithFrontmatter()
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
            MediaType = "instagram_reel",
            Tags = new[] { "tag1", "tag2" }
        };

        const string markdownBody = "## Key Learnings\n- Point A\n- Point B";

        // Act
        var filePath = await writer.WriteNoteAsync(markdownBody, metadata);

        // Assert
        Assert.True(File.Exists(filePath));
        var content = await File.ReadAllTextAsync(filePath);

        Assert.StartsWith("---", content);
        Assert.Contains("title: \"Obsidian Capture Note\"", content);
        Assert.Contains("source_url: \"https://instagram.com/reel/abc987\"", content);
        Assert.Contains("author: \"@educator\"", content);
        Assert.Contains("  - tag1", content);
        Assert.Contains("## Key Learnings", content);
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
