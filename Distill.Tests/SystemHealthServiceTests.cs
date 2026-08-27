using System.Net;
using System.Text.Json;
using Distill.Core.Configuration;
using Distill.Core.Diagnostics;
using Distill.Core.Process;
using Distill.Tests.Mocks;
using Microsoft.Extensions.Options;
using Xunit;

namespace Distill.Tests;

public class SystemHealthServiceTests
{
    private readonly FakeProcessRunner _fakeRunner;
    private readonly FakeToolLocator _fakeLocator;
    private readonly FakeHttpMessageHandler _fakeHttpHandler;
    private readonly HttpClient _fakeHttpClient;
    private readonly IOptions<DistillSettings> _settings;
    private readonly SystemHealthService _healthService;

    public SystemHealthServiceTests()
    {
        _fakeRunner = new FakeProcessRunner();
        _fakeLocator = new FakeToolLocator();
        _fakeHttpHandler = new FakeHttpMessageHandler();
        _fakeHttpClient = new HttpClient(_fakeHttpHandler);
        _settings = Options.Create(new DistillSettings());

        _healthService = new SystemHealthService(
            _fakeLocator,
            _fakeRunner,
            _settings,
            _fakeHttpClient);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenToolsPresentAndOllamaOnline_ReportsHealthy()
    {
        // Arrange
        _fakeRunner.CustomHandler = (exe, args, _) =>
        {
            if (args.Contains("--version"))
            {
                return new ProcessResult(0, "2024.08.01", string.Empty);
            }
            if (args.Contains("-version"))
            {
                return new ProcessResult(0, "ffmpeg version 7.0", string.Empty);
            }
            if (args.Contains("-h"))
            {
                return new ProcessResult(0, "usage: whisper [options]", string.Empty);
            }
            return new ProcessResult(0, "OK", string.Empty);
        };

        var tagsJson = JsonSerializer.Serialize(new
        {
            models = new[]
            {
                new { name = "llama3.2:3b" },
                new { name = "qwen2.5:7b" }
            }
        });

        _fakeHttpHandler.Handler = request =>
        {
            if (request.RequestUri?.ToString().Contains("/api/tags") == true)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(tagsJson)
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        var report = await _healthService.CheckHealthAsync();

        // Assert
        Assert.NotNull(report);
        Assert.True(report.YtDlp.IsReady);
        Assert.Contains("2024.08.01", report.YtDlp.Details);
        Assert.True(report.Ffmpeg.IsReady);
        Assert.True(report.Whisper.IsReady);
        Assert.True(report.Ollama.IsReady);
        Assert.Contains("2 model(s)", report.Ollama.Details);
    }

    [Fact]
    public async Task GetInstalledOllamaModelsAsync_WhenOllamaReturnsModels_ParsesCorrectly()
    {
        // Arrange
        var tagsJson = """
        {
            "models": [
                { "name": "llama3.2:3b" },
                { "name": "mistral:latest" },
                { "name": "nomic-embed-text:latest" }
            ]
        }
        """;

        _fakeHttpHandler.Handler = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(tagsJson)
        };

        // Act
        var models = await _healthService.GetInstalledOllamaModelsAsync();

        // Assert
        Assert.Equal(3, models.Count);
        Assert.Contains("llama3.2:3b", models);
        Assert.Contains("mistral:latest", models);
        Assert.Contains("nomic-embed-text:latest", models);
    }

    [Fact]
    public async Task GetInstalledOllamaModelsAsync_WhenOllamaOffline_ReturnsEmptyList()
    {
        // Arrange
        _fakeHttpHandler.Handler = _ => throw new HttpRequestException("Connection refused");

        // Act
        var models = await _healthService.GetInstalledOllamaModelsAsync();

        // Assert
        Assert.Empty(models);
    }

    [Fact]
    public async Task DownloadOrUpdateYtDlpAsync_WhenInvoked_DownloadsExecutable()
    {
        // Arrange
        var reportedMessages = new List<string>();
        var progress = new Progress<string>(msg => reportedMessages.Add(msg));

        _fakeHttpHandler.Handler = request =>
        {
            if (request.RequestUri?.ToString().Contains("yt-dlp.exe") == true)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(new byte[] { 0x4D, 0x5A, 0x90, 0x00 }) // mock MZ header
                };
            }
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        };

        // Act
        await _healthService.DownloadOrUpdateYtDlpAsync(progress);

        // Assert
        var toolsDir = Path.Combine(AppContext.BaseDirectory, "tools");
        var ytdlpPath = Path.Combine(toolsDir, "yt-dlp.exe");
        Assert.True(File.Exists(ytdlpPath));
    }

    [Fact]
    public void ToolLocator_WithCustomPathThatExists_ResolvesCustomPath()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"custom_ytdlp_{Guid.NewGuid():N}.exe");
        File.WriteAllText(tempFile, "mock");
        try
        {
            var settings = new DistillSettings { YtDlpBinaryPath = tempFile };
            var locator = new ToolLocator(Options.Create(settings));

            // Act
            var resolved = locator.ResolveToolPath("yt-dlp.exe");

            // Assert
            Assert.Equal(tempFile, resolved);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ToolLocator_WithNonExistentCustomPath_FallsBackToDefault()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "does_not_exist_xyz.exe");
        var settings = new DistillSettings { YtDlpBinaryPath = nonExistentPath };
        var locator = new ToolLocator(Options.Create(settings));

        // Act
        var resolved = locator.ResolveToolPath("yt-dlp.exe");

        // Assert
        Assert.NotEqual(nonExistentPath, resolved);
    }

    [Fact]
    public void DistillSettings_Defaults_AreConfiguredProperly()
    {
        var settings = new DistillSettings();

        Assert.True(settings.AutoOpenInObsidian);
        Assert.Equal(2, settings.MaxConcurrentJobs);
        Assert.Equal(0.3, settings.SceneChangeThreshold);
        Assert.False(settings.AppendRawContentToNote);
        Assert.Equal("llama3.2:3b", settings.OllamaModelName);
        Assert.Equal("http://localhost:11434", settings.OllamaEndpoint);
        Assert.Equal(string.Empty, settings.YtDlpBinaryPath);
        Assert.Equal(string.Empty, settings.FfmpegBinaryPath);
        Assert.Equal(string.Empty, settings.WhisperBinaryPath);
        Assert.Equal("Default", settings.SelectedTheme);
    }
}
