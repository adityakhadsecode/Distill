using Distill.Core.Configuration;
using Distill.Core.Downloaders;
using Distill.Core.Formatting;
using Distill.Core.Models;
using Distill.Core.Ocr;
using Distill.Core.SpeechToText;
using Microsoft.Extensions.Options;
using Xunit;

namespace Distill.Tests;

public class StubPipelineTests
{
    [Fact]
    public async Task ReelDownloaderStub_ReturnsValidDownloadResult()
    {
        // Arrange
        IReelDownloader downloader = new ReelDownloaderStub();
        const string testUrl = "https://www.instagram.com/reel/C8xyz123";

        // Act
        var result = await downloader.DownloadAsync(testUrl);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testUrl, result.SourceUrl);
        var reelResult = Assert.IsType<ReelDownloadResult>(result);
        Assert.False(string.IsNullOrWhiteSpace(reelResult.VideoFilePath));
        Assert.False(string.IsNullOrWhiteSpace(reelResult.AudioFilePath));
    }

    [Fact]
    public async Task WindowsMediaOcrExtractorStub_ReturnsExtractedText()
    {
        // Arrange
        ITextExtractor extractor = new WindowsMediaOcrExtractorStub();
        const string fakeImagePath = "C:\\Temp\\slide1.jpg";

        // Act
        var text = await extractor.ExtractTextAsync(fakeImagePath);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.Contains("slide1.jpg", text);
    }

    [Fact]
    public async Task WhisperCppTranscriberStub_ReturnsTranscriptText()
    {
        // Arrange
        ITranscriber transcriber = new WhisperCppTranscriberStub();
        const string fakeAudioPath = "C:\\Temp\\audio.wav";

        // Act
        var transcript = await transcriber.TranscribeAsync(fakeAudioPath);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(transcript));
        Assert.Contains("audio.wav", transcript);
    }

    [Fact]
    public async Task OllamaNoteFormatterStub_GeneratesMarkdown()
    {
        // Arrange
        var settings = Options.Create(new DistillSettings
        {
            OllamaModelName = "llama3.2:3b",
            OllamaEndpoint = "http://localhost:11434"
        });
        INoteFormatter formatter = new OllamaNoteFormatterStub(settings);

        var content = new ExtractedContent
        {
            SpokenTranscript = "Spoken video content",
            OcrTextSegments = new[] { "Slide text line 1" }
        };

        var metadata = new NoteMetadata
        {
            Title = "Test Distilled Title",
            SourceUrl = "https://instagram.com/p/123",
            Author = "@tester"
        };

        // Act
        var markdown = await formatter.FormatNoteAsync(content, metadata);

        // Assert
        Assert.Contains("# Test Distilled Title", markdown);
        Assert.Contains("Spoken video content", markdown);
        Assert.Contains("Slide text line 1", markdown);
    }
}
