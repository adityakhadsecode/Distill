using Distill.Core.Configuration;
using Distill.Core.Models;
using Distill.Core.VaultWriter;
using Microsoft.Extensions.Options;
using Xunit;

namespace Distill.Tests;

public class ObsidianVaultWriterTests : IDisposable
{
    private readonly string _testVaultDir;

    public ObsidianVaultWriterTests()
    {
        _testVaultDir = Path.Combine(Path.GetTempPath(), "Distill_VaultTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testVaultDir);
    }

    [Fact]
    public async Task WriteNoteAsync_ExtractsTitleFromFirstHeading_AndGeneratesCleanSlug()
    {
        // Arrange
        var settings = Options.Create(new DistillSettings
        {
            VaultFolderPath = _testVaultDir
        });
        var writer = new ObsidianVaultWriter(settings);

        const string markdown = @"# Master Clean Architecture in 5 Steps

## Summary
A deep dive into domain driven design.

## Key Takeaways
- Decouple framework dependencies
";
        var meta = new NoteMetadata
        {
            SourceUrl = "https://www.instagram.com/reel/C8abc123",
            SourceType = SourceType.Reel,
            Author = "@cleancoder",
            CapturedAtUtc = new DateTime(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc),
            Tags = ["software-engineering", "architecture"]
        };

        // Act
        var createdPath = await writer.WriteNoteAsync(markdown, meta);

        // Assert
        Assert.True(File.Exists(createdPath));
        var fileName = Path.GetFileName(createdPath);
        Assert.Equal("Master-Clean-Architecture-in-5-Steps.md", fileName, ignoreCase: true);

        var fileContent = await File.ReadAllTextAsync(createdPath);
        Assert.StartsWith("---", fileContent);
        Assert.Contains("source: \"https://www.instagram.com/reel/C8abc123\"", fileContent);
        Assert.Contains("type: \"reel\"", fileContent);
        Assert.Contains("author: \"@cleancoder\"", fileContent);
        Assert.Contains("tags:\n  - software-engineering\n  - architecture", fileContent.Replace("\r\n", "\n"));
        Assert.Contains("# Master Clean Architecture in 5 Steps", fileContent);
    }

    [Fact]
    public async Task WriteNoteAsync_WhenNoHeadingPresent_FallsBackToMetadataTitle()
    {
        // Arrange
        var settings = Options.Create(new DistillSettings { VaultFolderPath = _testVaultDir });
        var writer = new ObsidianVaultWriter(settings);

        const string markdown = "Plain text body without h1 heading.";
        var meta = new NoteMetadata
        {
            Title = "Infographic: React Hooks Cheat Sheet",
            SourceUrl = "https://www.instagram.com/p/carousel456",
            SourceType = SourceType.Post
        };

        // Act
        var createdPath = await writer.WriteNoteAsync(markdown, meta);

        // Assert
        Assert.True(File.Exists(createdPath));
        var fileName = Path.GetFileName(createdPath);
        Assert.Contains("React-Hooks-Cheat-Sheet", fileName, StringComparison.OrdinalIgnoreCase);

        var fileContent = await File.ReadAllTextAsync(createdPath);
        Assert.Contains("type: \"post\"", fileContent);
        Assert.Contains("Plain text body without h1 heading.", fileContent);
    }

    [Fact]
    public async Task WriteNoteAsync_WhenCollisionOccurs_AppendsNumericSuffix()
    {
        // Arrange
        var settings = Options.Create(new DistillSettings { VaultFolderPath = _testVaultDir });
        var writer = new ObsidianVaultWriter(settings);

        const string markdown = "# Common Title\n\nContent";
        var meta = new NoteMetadata
        {
            SourceUrl = "https://instagram.com/p/1",
            SourceType = SourceType.Post
        };

        // Act
        var firstPath = await writer.WriteNoteAsync(markdown, meta);
        var secondPath = await writer.WriteNoteAsync(markdown, meta);
        var thirdPath = await writer.WriteNoteAsync(markdown, meta);

        // Assert
        Assert.Equal("Common-Title.md", Path.GetFileName(firstPath), ignoreCase: true);
        Assert.Equal("Common-Title-1.md", Path.GetFileName(secondPath), ignoreCase: true);
        Assert.Equal("Common-Title-2.md", Path.GetFileName(thirdPath), ignoreCase: true);
    }

    [Fact]
    public async Task WriteNoteAsync_CreatesTargetSubdirectoryIfMissing()
    {
        // Arrange
        var nestedDir = Path.Combine(_testVaultDir, "SubFolder", "DeepFolder");
        var settings = Options.Create(new DistillSettings { VaultFolderPath = nestedDir });
        var writer = new ObsidianVaultWriter(settings);

        var meta = new NoteMetadata
        {
            Title = "Nested Note",
            SourceUrl = "https://instagram.com/p/deep"
        };

        // Act
        var createdPath = await writer.WriteNoteAsync("# Nested Note\n\nContent", meta);

        // Assert
        Assert.True(Directory.Exists(nestedDir));
        Assert.True(File.Exists(createdPath));
    }

    [Fact]
    public void BuildObsidianUri_ConstructsDirectPathAndVaultProtocols()
    {
        // Arrange
        var settings = Options.Create(new DistillSettings());
        var writer = new ObsidianVaultWriter(settings);
        const string filePath = "C:\\Notes\\Distilled.md";

        // Act
        var directPathUri = writer.BuildObsidianUri(filePath);
        var vaultSpecificUri = writer.BuildObsidianUri(filePath, "KnowledgeVault");

        // Assert
        Assert.StartsWith("obsidian://open?path=", directPathUri);
        Assert.Equal("obsidian://open?vault=KnowledgeVault&file=Distilled.md", vaultSpecificUri);
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
            // Ignore test cleanup errors
        }
    }
}
