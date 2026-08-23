using Distill.Core.Ocr;
using Xunit;

namespace Distill.Tests;

public class WindowsMediaOcrExtractorTests
{
    [Fact]
    public async Task ExtractTextAsync_WhenImagePathIsNull_ThrowsArgumentException()
    {
        // Arrange
        var extractor = new WindowsMediaOcrExtractor();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => extractor.ExtractTextAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => extractor.ExtractTextAsync("   "));
    }

    [Fact]
    public async Task ExtractTextAsync_WhenImageFileDoesNotExist_ThrowsFileNotFoundException()
    {
        // Arrange
        var extractor = new WindowsMediaOcrExtractor();
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "non_existent_" + Guid.NewGuid().ToString("N") + ".jpg");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => extractor.ExtractTextAsync(nonExistentPath));
    }

    [Fact]
    public async Task ExtractTextFromMultipleAsync_WithEmptyList_ReturnsEmptyDictionary()
    {
        // Arrange
        var extractor = new WindowsMediaOcrExtractor();

        // Act
        var result = await extractor.ExtractTextFromMultipleAsync([]);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExtractTextFromMultipleAsync_WithStub_ProcessesBatchSuccessfully()
    {
        // Arrange
        ITextExtractor extractor = new WindowsMediaOcrExtractorStub();
        var imagePaths = new[]
        {
            "C:\\Temp\\slide1.jpg",
            "C:\\Temp\\slide2.png",
            "C:\\Temp\\slide3.webp"
        };

        // Act
        var results = await extractor.ExtractTextFromMultipleAsync(imagePaths);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(3, results.Count);
        Assert.Contains(imagePaths[0], results.Keys);
        Assert.Contains(imagePaths[1], results.Keys);
        Assert.Contains(imagePaths[2], results.Keys);
        Assert.Contains("slide1.jpg", results[imagePaths[0]]);
        Assert.Contains("slide2.png", results[imagePaths[1]]);
        Assert.Contains("slide3.webp", results[imagePaths[2]]);
    }
}
