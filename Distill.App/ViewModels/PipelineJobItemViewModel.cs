using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Distill.Core.Pipeline;

namespace Distill.App.ViewModels;

public partial class PipelineJobItemViewModel : ObservableObject
{
    public Guid Id { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    private string _url;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    private string _title;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsActive))]
    [NotifyPropertyChangedFor(nameof(IsDone))]
    [NotifyPropertyChangedFor(nameof(IsFailed))]
    [NotifyPropertyChangedFor(nameof(IsQueued))]
    [NotifyPropertyChangedFor(nameof(StatusBadgeText))]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(StatusGlyph))]
    [NotifyPropertyChangedFor(nameof(StageStepText))]
    [NotifyPropertyChangedFor(nameof(ProgressText))]
    private PipelineJobStatus _status;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    private int _progressPercent;

    [ObservableProperty]
    private string _statusMessage;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _generatedNotePath;

    [ObservableProperty]
    private string? _obsidianUri;

    [ObservableProperty]
    private DateTime _createdAt;

    [ObservableProperty]
    private DateTime? _completedAt;

    [ObservableProperty]
    private string _rawOcrPreview = string.Empty;

    [ObservableProperty]
    private string _rawTranscriptPreview = string.Empty;

    [ObservableProperty]
    private bool _isDetailsExpanded;

    public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? Url : Title;
    public double Progress => ProgressPercent;
    public string ProgressText => StageStepText;
    public string StatusText => StatusBadgeText;

    public bool IsActive => Status is PipelineJobStatus.Downloading or PipelineJobStatus.Extracting or PipelineJobStatus.Formatting;
    public bool IsDone => Status == PipelineJobStatus.Done;
    public bool IsFailed => Status == PipelineJobStatus.Failed;
    public bool IsQueued => Status == PipelineJobStatus.Queued;

    public string StatusBadgeText => Status switch
    {
        PipelineJobStatus.Queued => "Queued",
        PipelineJobStatus.Downloading => "Downloading",
        PipelineJobStatus.Extracting => "Extracting",
        PipelineJobStatus.Formatting => "Distilling",
        PipelineJobStatus.Done => "Completed",
        PipelineJobStatus.Failed => "Failed",
        _ => Status.ToString()
    };

    public string StatusGlyph => Status switch
    {
        PipelineJobStatus.Queued => "\uE823",      // Clock
        PipelineJobStatus.Downloading => "\uE896", // Download
        PipelineJobStatus.Extracting => "\uE8B7",  // Reading / OCR
        PipelineJobStatus.Formatting => "\uE946",  // Processing / AI
        PipelineJobStatus.Done => "\uE73E",        // CheckMark
        PipelineJobStatus.Failed => "\uE711",      // Error / Cancel
        _ => "\uE712"
    };

    public string StageStepText => Status switch
    {
        PipelineJobStatus.Queued => "Queued in pipeline",
        PipelineJobStatus.Downloading => "Downloading media & metadata",
        PipelineJobStatus.Extracting => "Running OCR & speech transcription",
        PipelineJobStatus.Formatting => "Synthesizing note via Ollama",
        PipelineJobStatus.Done => "Note saved to Obsidian",
        PipelineJobStatus.Failed => "Failed to process",
        _ => StatusMessage
    };

    public string FormattedTime => CreatedAt.ToLocalTime().ToString("t");

    public PipelineJobItemViewModel(PipelineJob job)
    {
        Id = job.Id;
        _url = job.Url;
        _title = string.IsNullOrWhiteSpace(job.Title) ? job.Url : job.Title;
        _status = job.Status;
        _progressPercent = job.ProgressPercent;
        _statusMessage = job.StatusMessage;
        _errorMessage = job.ErrorMessage;
        _generatedNotePath = job.GeneratedNotePath;
        _obsidianUri = job.ObsidianUri;
        _createdAt = job.CreatedAt;
        _completedAt = job.CompletedAt;
    }

    public void UpdateFrom(PipelineJob job)
    {
        Url = job.Url;
        if (!string.IsNullOrWhiteSpace(job.Title))
        {
            Title = job.Title;
        }
        Status = job.Status;
        ProgressPercent = job.ProgressPercent;
        StatusMessage = job.StatusMessage;
        ErrorMessage = job.ErrorMessage;
        GeneratedNotePath = job.GeneratedNotePath;
        ObsidianUri = job.ObsidianUri;
        CompletedAt = job.CompletedAt;
    }

    [RelayCommand]
    private void OpenInObsidian()
    {
        if (string.IsNullOrWhiteSpace(GeneratedNotePath) && string.IsNullOrWhiteSpace(ObsidianUri)) return;

        try
        {
            if (!string.IsNullOrWhiteSpace(ObsidianUri))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = ObsidianUri,
                    UseShellExecute = true
                });
            }
            else if (!string.IsNullOrWhiteSpace(GeneratedNotePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = GeneratedNotePath,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(GeneratedNotePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = GeneratedNotePath,
                        UseShellExecute = true
                    });
                }
                catch
                {
                    // Ignore fallback error
                }
            }
        }
    }

    [RelayCommand]
    private void OpenFile()
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
        catch
        {
            // Ignore fallback error
        }
    }

    [RelayCommand]
    private void OpenFolder()
    {
        if (string.IsNullOrWhiteSpace(GeneratedNotePath)) return;

        try
        {
            var folder = Path.GetDirectoryName(GeneratedNotePath);
            if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Ignore fallback error
        }
    }
}
