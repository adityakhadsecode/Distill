using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Distill.Core.Pipeline;

namespace Distill.App.ViewModels;

public partial class PipelineJobItemViewModel : ObservableObject
{
    public Guid Id { get; }

    [ObservableProperty]
    private string _url;

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private PipelineJobStatus _status;

    [ObservableProperty]
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

    public bool IsActive => Status is PipelineJobStatus.Downloading or PipelineJobStatus.Extracting or PipelineJobStatus.Formatting;
    public bool IsDone => Status == PipelineJobStatus.Done;
    public bool IsFailed => Status == PipelineJobStatus.Failed;
    public bool IsQueued => Status == PipelineJobStatus.Queued;

    public string StatusBadgeText => Status switch
    {
        PipelineJobStatus.Queued => "Queued",
        PipelineJobStatus.Downloading => "Downloading",
        PipelineJobStatus.Extracting => "Extracting",
        PipelineJobStatus.Formatting => "AI Distilling",
        PipelineJobStatus.Done => "Done",
        PipelineJobStatus.Failed => "Failed",
        _ => Status.ToString()
    };

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

        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(IsDone));
        OnPropertyChanged(nameof(IsFailed));
        OnPropertyChanged(nameof(IsQueued));
        OnPropertyChanged(nameof(StatusBadgeText));
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
                    // Ignore shell execute fallback error
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
            // Ignore shell execute fallback error
        }
    }
}
