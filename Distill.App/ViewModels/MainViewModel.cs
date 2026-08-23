using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Distill.Core.Configuration;
using Distill.Core.Downloaders;
using Distill.Core.Formatting;
using Distill.Core.Models;
using Distill.Core.Ocr;
using Distill.Core.SpeechToText;
using Distill.Core.VaultWriter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Distill.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IReelDownloader _downloader;
    private readonly ITextExtractor _textExtractor;
    private readonly ITranscriber _transcriber;
    private readonly INoteFormatter _noteFormatter;
    private readonly IVaultWriter _vaultWriter;
    private readonly DistillSettings _settings;
    private readonly ILogger<MainViewModel>? _logger;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DistillCommand))]
    private string _instagramUrl = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Ready to distill notes.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DistillCommand))]
    private bool _isProcessing;

    [ObservableProperty]
    private string? _generatedNotePath;

    [ObservableProperty]
    private bool _canOpenInObsidian;

    public MainViewModel(
        IReelDownloader downloader,
        ITextExtractor textExtractor,
        ITranscriber transcriber,
        INoteFormatter noteFormatter,
        IVaultWriter vaultWriter,
        IOptions<DistillSettings> settings,
        ILogger<MainViewModel>? logger = null)
    {
        _downloader = downloader;
        _textExtractor = textExtractor;
        _transcriber = transcriber;
        _noteFormatter = noteFormatter;
        _vaultWriter = vaultWriter;
        _settings = settings.Value;
        _logger = logger;
    }

    private bool CanDistill => !IsProcessing && !string.IsNullOrWhiteSpace(InstagramUrl);

    [RelayCommand(CanExecute = nameof(CanDistill))]
    private async Task DistillAsync(CancellationToken cancellationToken)
    {
        DownloadResult? downloadResult = null;
        try
        {
            IsProcessing = true;
            CanOpenInObsidian = false;
            GeneratedNotePath = null;
            StatusMessage = "1/5: Downloading media from Instagram...";

            downloadResult = await _downloader.DownloadAsync(InstagramUrl, cancellationToken);

            StatusMessage = "2/5: Extracting text & audio speech...";
            var ocrSegments = new List<string>();
            var transcript = string.Empty;
            var mediaType = "instagram_post";

            if (downloadResult is PostDownloadResult postResult)
            {
                mediaType = "instagram_post";
                foreach (var img in postResult.ImageFilePaths)
                {
                    var text = await _textExtractor.ExtractTextAsync(img, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        ocrSegments.Add(text);
                    }
                }
            }
            else if (downloadResult is ReelDownloadResult reelResult)
            {
                mediaType = "instagram_reel";
                foreach (var frame in reelResult.FrameFilePaths)
                {
                    var text = await _textExtractor.ExtractTextAsync(frame, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        ocrSegments.Add(text);
                    }
                }

                if (!string.IsNullOrWhiteSpace(reelResult.AudioFilePath))
                {
                    transcript = await _transcriber.TranscribeAsync(reelResult.AudioFilePath, cancellationToken);
                }
            }

            var extractedContent = new ExtractedContent
            {
                SpokenTranscript = transcript,
                OcrTextSegments = ocrSegments,
                RawCaption = downloadResult.Caption ?? string.Empty
            };

            var noteMetadata = new NoteMetadata
            {
                Title = downloadResult.Title ?? "Instagram Distilled Note",
                SourceUrl = InstagramUrl,
                Author = downloadResult.Author,
                MediaType = mediaType,
                CreatedAt = DateTime.UtcNow
            };

            StatusMessage = "3/5: Synthesizing notes with Ollama LLM...";
            var markdownBody = await _noteFormatter.FormatNoteAsync(extractedContent, noteMetadata, cancellationToken);

            StatusMessage = "4/5: Writing note to Obsidian vault...";
            var createdPath = await _vaultWriter.WriteNoteAsync(markdownBody, noteMetadata, cancellationToken);

            GeneratedNotePath = createdPath;
            CanOpenInObsidian = true;
            StatusMessage = $"5/5: Done! Saved to: {Path.GetFileName(createdPath)}";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing Instagram URL {Url}", InstagramUrl);
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            // Clean up temporary download artifacts
            downloadResult?.Cleanup();
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private void OpenInObsidian()
    {
        if (string.IsNullOrWhiteSpace(GeneratedNotePath)) return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = GeneratedNotePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to open note in Obsidian");
            StatusMessage = $"Failed to open note: {ex.Message}";
        }
    }
}
