using Distill.Core.Configuration;
using Distill.Core.Downloaders;
using Distill.Core.Exceptions;
using Distill.Core.Formatting;
using Distill.Core.Ocr;
using Distill.Core.Pipeline;
using Distill.Core.SpeechToText;
using Distill.Core.VaultWriter;
using Microsoft.Extensions.Options;
using Xunit;

namespace Distill.Tests;

public class PipelineOrchestratorTests
{
    [Fact]
    public async Task RunJobAsync_RunsFullPipeline_AndFiresStateChanges()
    {
        // Arrange
        var settings = Options.Create(new DistillSettings());
        var downloader = new ReelDownloaderStub();
        var ocr = new WindowsMediaOcrExtractorStub();
        var stt = new WhisperCppTranscriberStub();
        var formatter = new OllamaNoteFormatterStub(settings);
        var vault = new ObsidianVaultWriterStub(settings);

        var orchestrator = new PipelineOrchestrator(downloader, ocr, stt, formatter, vault);
        var observedStatuses = new List<PipelineJobStatus>();

        orchestrator.JobChanged += (s, e) =>
        {
            observedStatuses.Add(e.Job.Status);
        };

        var job = new PipelineJob
        {
            Url = "https://www.instagram.com/reel/C8test123"
        };

        // Act
        var result = await orchestrator.RunJobAsync(job);

        // Assert
        Assert.Equal(PipelineJobStatus.Done, result.Status);
        Assert.Equal(100, result.ProgressPercent);
        Assert.False(string.IsNullOrWhiteSpace(result.GeneratedNotePath));
        Assert.False(string.IsNullOrWhiteSpace(result.ObsidianUri));

        Assert.Contains(PipelineJobStatus.Downloading, observedStatuses);
        Assert.Contains(PipelineJobStatus.Extracting, observedStatuses);
        Assert.Contains(PipelineJobStatus.Formatting, observedStatuses);
        Assert.Contains(PipelineJobStatus.Done, observedStatuses);
    }

    [Fact]
    public async Task RunJobAsync_WhenExceptionOccurs_MarksJobAsFailed()
    {
        // Arrange
        var settings = Options.Create(new DistillSettings());
        var failingDownloader = new FailingDownloader();
        var ocr = new WindowsMediaOcrExtractorStub();
        var stt = new WhisperCppTranscriberStub();
        var formatter = new OllamaNoteFormatterStub(settings);
        var vault = new ObsidianVaultWriterStub(settings);

        var orchestrator = new PipelineOrchestrator(failingDownloader, ocr, stt, formatter, vault);
        var job = new PipelineJob { Url = "https://instagram.com/p/private" };

        // Act
        var result = await orchestrator.RunJobAsync(job);

        // Assert
        Assert.Equal(PipelineJobStatus.Failed, result.Status);
        Assert.Contains("private account", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private class FailingDownloader : IReelDownloader
    {
        public Task<Distill.Core.Models.DownloadResult> DownloadAsync(string instagramUrl, CancellationToken cancellationToken = default)
        {
            throw new PrivateMediaException("This Instagram post is from a private account.");
        }
    }
}
