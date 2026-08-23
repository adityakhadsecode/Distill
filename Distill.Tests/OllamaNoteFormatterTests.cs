using System.Net;
using System.Text.Json;
using Distill.Core.Configuration;
using Distill.Core.Exceptions;
using Distill.Core.Formatting;
using Distill.Core.Models;
using Distill.Tests.Mocks;
using Microsoft.Extensions.Options;
using Xunit;

namespace Distill.Tests;

public class OllamaNoteFormatterTests
{
    private readonly FakeHttpMessageHandler _httpHandler;
    private readonly HttpClient _httpClient;
    private readonly IOptions<DistillSettings> _settings;

    public OllamaNoteFormatterTests()
    {
        _httpHandler = new FakeHttpMessageHandler();
        _httpClient = new HttpClient(_httpHandler);
        _settings = Options.Create(new DistillSettings
        {
            OllamaEndpoint = "http://localhost:11434",
            OllamaModelName = "llama3.2:3b"
        });
    }

    [Fact]
    public async Task FormatAsync_WhenSuccessful_ReturnsCleanMarkdown()
    {
        // Arrange
        const string expectedMarkdown = "# Clean Architecture\n\n## Summary\nKey points on separation of concerns.\n\n## Key Takeaways\n- Decouple I/O from domain.";
        _httpHandler.Handler = request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("http://localhost:11434/api/generate", request.RequestUri?.ToString());

            var responseJson = JsonSerializer.Serialize(new
            {
                model = "llama3.2:3b",
                response = expectedMarkdown,
                done = true
            });

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
            };
        };

        var formatter = new OllamaNoteFormatter(_httpClient, _settings);
        var content = new RawExtractedContent
        {
            SourceUrl = "https://instagram.com/reel/123",
            SourceType = SourceType.Reel,
            TranscriptText = "Hello guys today we talk about clean code.",
            OcrTextSegments = ["Slide 1: Clean Code", "Slide 2: Invariants"]
        };

        // Act
        var result = await formatter.FormatAsync(content);

        // Assert
        Assert.Equal(expectedMarkdown, result);
        Assert.Single(_httpHandler.Requests);
    }

    [Fact]
    public async Task FormatAsync_ConstructsPromptWithInstructionsAndContent()
    {
        // Arrange
        string? capturedRequestBody = null;
        _httpHandler.Handler = request =>
        {
            capturedRequestBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            var responseJson = JsonSerializer.Serialize(new { response = "# Title\n## Summary\n..." });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
            };
        };

        var formatter = new OllamaNoteFormatter(_httpClient, _settings);
        var content = new RawExtractedContent
        {
            SourceUrl = "https://instagram.com/p/carousel123",
            SourceType = SourceType.Post,
            Title = "Top 5 Design Tips",
            Author = "@designer",
            Caption = "Check out these tips! Link in bio #design #tips",
            OcrTextSegments = ["Tip 1: Contrast", "Tip 2: Hierarchy"]
        };

        // Act
        await formatter.FormatAsync(content);

        // Assert
        Assert.NotNull(capturedRequestBody);
        Assert.Contains("llama3.2:3b", capturedRequestBody);
        Assert.Contains("stream\":false", capturedRequestBody);
        Assert.Contains("Strip ALL social media boilerplate", capturedRequestBody);
        Assert.Contains("Top 5 Design Tips", capturedRequestBody);
        Assert.Contains("@designer", capturedRequestBody);
        Assert.Contains("Tip 1: Contrast", capturedRequestBody);
    }

    [Fact]
    public async Task FormatAsync_WhenFirstAttemptFails_RetriesOnceAndSucceeds()
    {
        // Arrange
        var attempt = 0;
        _httpHandler.Handler = _ =>
        {
            attempt++;
            if (attempt == 1)
            {
                throw new HttpRequestException("Transient connection reset");
            }

            var responseJson = JsonSerializer.Serialize(new { response = "# Distilled Note\n## Summary\nSuccess after retry." });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
            };
        };

        var formatter = new OllamaNoteFormatter(_httpClient, _settings);
        var content = new RawExtractedContent { SourceUrl = "https://instagram.com/reel/abc" };

        // Act
        var result = await formatter.FormatAsync(content);

        // Assert
        Assert.Equal(2, attempt);
        Assert.Contains("Success after retry", result);
    }

    [Fact]
    public async Task FormatAsync_WhenOllamaUnreachable_ThrowsOllamaConnectionException()
    {
        // Arrange
        _httpHandler.Handler = _ => throw new HttpRequestException("No connection could be made because the target machine actively refused it");

        var formatter = new OllamaNoteFormatter(_httpClient, _settings);
        var content = new RawExtractedContent { SourceUrl = "https://instagram.com/reel/fail" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<OllamaConnectionException>(() => formatter.FormatAsync(content));
        Assert.Equal("http://localhost:11434", ex.Endpoint);
        Assert.Contains("ollama serve", ex.Message);
    }
}
