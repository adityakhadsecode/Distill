using Distill.Core.Configuration;
using Distill.Core.Downloaders;
using Distill.Core.Formatting;
using Distill.Core.Models;
using Distill.Core.Ocr;
using Distill.Core.SpeechToText;
using Distill.Core.VaultWriter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Distill.Core.Pipeline;

/// <summary>
/// Orchestrates the full Instagram distillation pipeline:
/// Download -> Extract (OCR / Speech) -> Synthesize via Ollama -> Save to Obsidian Vault.
/// </summary>
public class PipelineOrchestrator : IPipelineOrchestrator
{
    private readonly IReelDownloader _downloader;
    private readonly ITextExtractor _textExtractor;
    private readonly ITranscriber _transcriber;
    private readonly INoteFormatter _noteFormatter;
    private readonly IVaultWriter _vaultWriter;
    private readonly DistillSettings _settings;
    private readonly ILogger<PipelineOrchestrator>? _logger;

    public event EventHandler<PipelineJobChangedEventArgs>? JobChanged;

    public PipelineOrchestrator(
        IReelDownloader downloader,
        ITextExtractor textExtractor,
        ITranscriber transcriber,
        INoteFormatter noteFormatter,
        IVaultWriter vaultWriter,
        IOptions<DistillSettings>? settings = null,
        ILogger<PipelineOrchestrator>? logger = null)
    {
        _downloader = downloader;
        _textExtractor = textExtractor;
        _transcriber = transcriber;
        _noteFormatter = noteFormatter;
        _vaultWriter = vaultWriter;
        _settings = settings?.Value ?? new DistillSettings();
        _logger = logger;
    }

    public async Task<PipelineJob> RunJobAsync(PipelineJob job, CancellationToken cancellationToken = default)
    {
        if (job == null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        DownloadResult? downloadResult = null;
        try
        {
            _logger?.LogInformation("Starting pipeline for job {JobId} ({Url})", job.Id, job.Url);

            // Step 1: Downloading
            job.Status = PipelineJobStatus.Downloading;
            job.ProgressPercent = 15;
            job.StatusMessage = "Step 1/4: Downloading media from Instagram...";
            NotifyJobChanged(job);

            downloadResult = await _downloader.DownloadAsync(job.Url, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(downloadResult.Title))
            {
                job.Title = downloadResult.Title;
            }

            // Step 2: Extracting (OCR + STT)
            job.Status = PipelineJobStatus.Extracting;
            job.ProgressPercent = 40;
            job.StatusMessage = "Step 2/4: Extracting visual text & speech...";
            NotifyJobChanged(job);

            var ocrSegments = new List<string>();
            var transcript = string.Empty;
            var sourceType = SourceType.Post;

            if (downloadResult is PostDownloadResult postResult)
            {
                sourceType = SourceType.Post;
                var ocrBatch = await _textExtractor.ExtractTextFromMultipleAsync(postResult.ImageFilePaths, cancellationToken).ConfigureAwait(false);
                foreach (var imgPath in postResult.ImageFilePaths)
                {
                    if (ocrBatch.TryGetValue(imgPath, out var text) && !string.IsNullOrWhiteSpace(text))
                    {
                        ocrSegments.Add(text);
                    }
                }
            }
            else if (downloadResult is ReelDownloadResult reelResult)
            {
                sourceType = SourceType.Reel;
                var ocrBatch = await _textExtractor.ExtractTextFromMultipleAsync(reelResult.FrameFilePaths, cancellationToken).ConfigureAwait(false);
                foreach (var framePath in reelResult.FrameFilePaths)
                {
                    if (ocrBatch.TryGetValue(framePath, out var text) && !string.IsNullOrWhiteSpace(text))
                    {
                        ocrSegments.Add(text);
                    }
                }

                if (!string.IsNullOrWhiteSpace(reelResult.AudioFilePath))
                {
                    transcript = await _transcriber.TranscribeAsync(reelResult.AudioFilePath, cancellationToken).ConfigureAwait(false);
                }
            }

            var rawContent = new RawExtractedContent
            {
                SourceUrl = job.Url,
                SourceType = sourceType,
                OcrTextSegments = ocrSegments,
                TranscriptText = transcript,
                Caption = downloadResult.Caption,
                Title = downloadResult.Title,
                Author = downloadResult.Author
            };

            // Step 3: Formatting with Ollama LLM
            job.Status = PipelineJobStatus.Formatting;
            job.ProgressPercent = 70;
            job.StatusMessage = "Step 3/4: Distilling notes via Ollama LLM...";
            NotifyJobChanged(job);

            var markdownBody = await _noteFormatter.FormatAsync(rawContent, cancellationToken).ConfigureAwait(false);

            // Optional raw content footer
            if (_settings.AppendRawContentToNote && (!string.IsNullOrWhiteSpace(transcript) || ocrSegments.Count > 0))
            {
                markdownBody += "\n\n---\n\n<details>\n<summary><b>Original Extracted Content (OCR & Transcript)</b></summary>\n\n";
                if (!string.IsNullOrWhiteSpace(transcript))
                {
                    markdownBody += $"**Speech Transcript:**\n> {transcript.Trim()}\n\n";
                }
                if (ocrSegments.Count > 0)
                {
                    markdownBody += "**Visual Text (OCR):**\n" + string.Join("\n\n---\n", ocrSegments) + "\n\n";
                }
                markdownBody += "</details>\n";
            }

            // Step 4: Saving to Obsidian Vault
            job.ProgressPercent = 90;
            job.StatusMessage = "Step 4/4: Writing note to Obsidian vault...";
            NotifyJobChanged(job);

            var noteMetadata = new NoteMetadata
            {
                Title = downloadResult.Title ?? job.Title ?? "Instagram Distilled Note",
                SourceUrl = job.Url,
                Author = downloadResult.Author,
                SourceType = sourceType,
                CapturedAtUtc = DateTime.UtcNow,
                Tags = ["instagram", sourceType == SourceType.Reel ? "reel" : "post", "distilled"]
            };

            var createdPath = await _vaultWriter.WriteNoteAsync(markdownBody, noteMetadata, cancellationToken).ConfigureAwait(false);
            var obsidianUri = _vaultWriter.BuildObsidianUri(createdPath);

            // Completed Successfully
            job.Status = PipelineJobStatus.Done;
            job.ProgressPercent = 100;
            job.GeneratedNotePath = createdPath;
            job.ObsidianUri = obsidianUri;
            job.CompletedAt = DateTime.UtcNow;
            job.StatusMessage = $"Saved to Obsidian: {Path.GetFileName(createdPath)}";
            NotifyJobChanged(job);

            _logger?.LogInformation("Completed pipeline successfully for job {JobId} -> '{Path}'", job.Id, createdPath);
            return job;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Distillation pipeline failed for job {JobId} ({Url})", job.Id, job.Url);
            job.Status = PipelineJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.StatusMessage = $"Error: {ex.Message}";
            job.CompletedAt = DateTime.UtcNow;
            NotifyJobChanged(job);
            return job;
        }
        finally
        {
            // Clean up temporary download directory/files
            downloadResult?.Cleanup();
        }
    }

    private void NotifyJobChanged(PipelineJob job)
    {
        try
        {
            JobChanged?.Invoke(this, new PipelineJobChangedEventArgs(job));
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Exception in JobChanged event listener");
        }
    }
}
