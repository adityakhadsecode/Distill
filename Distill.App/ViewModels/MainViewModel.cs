using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Distill.Core.Configuration;
using Distill.Core.Pipeline;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.UI.Dispatching;

namespace Distill.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly DistillSettings _settings;
    private readonly ILogger<MainViewModel>? _logger;
    private readonly DispatcherQueue? _dispatcherQueue;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddJobCommand))]
    private string _instagramUrl = string.Empty;

    [ObservableProperty]
    private ObservableCollection<PipelineJobItemViewModel> _jobs = [];

    // Settings View Properties
    [ObservableProperty]
    private string _vaultFolderPath;

    [ObservableProperty]
    private string _ollamaModelName;

    [ObservableProperty]
    private string _ollamaEndpoint;

    [ObservableProperty]
    private string _whisperModelPath;

    [ObservableProperty]
    private int _whisperThreadCount;

    [ObservableProperty]
    private string _whisperLanguage;

    [ObservableProperty]
    private bool _isSettingsSavedVisible;

    public bool HasJobs => Jobs.Count > 0;
    public bool HasNoJobs => Jobs.Count == 0;
    public int ActiveJobsCount => Jobs.Count(j => j.IsActive);

    public MainViewModel(
        IPipelineOrchestrator orchestrator,
        IOptions<DistillSettings> settings,
        ILogger<MainViewModel>? logger = null)
    {
        _orchestrator = orchestrator;
        _settings = settings.Value;
        _logger = logger;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        // Load settings values
        _vaultFolderPath = _settings.VaultFolderPath;
        _ollamaModelName = _settings.OllamaModelName;
        _ollamaEndpoint = _settings.OllamaEndpoint;
        _whisperModelPath = _settings.WhisperModelPath;
        _whisperThreadCount = _settings.WhisperThreadCount;
        _whisperLanguage = _settings.WhisperLanguage;

        // Subscribe to pipeline orchestrator job changes
        _orchestrator.JobChanged += OnPipelineJobChanged;
    }

    private bool CanAddJob => !string.IsNullOrWhiteSpace(InstagramUrl);

    [RelayCommand(CanExecute = nameof(CanAddJob))]
    private void AddJob()
    {
        var rawUrl = InstagramUrl.Trim();
        if (string.IsNullOrWhiteSpace(rawUrl)) return;

        var job = new PipelineJob
        {
            Url = rawUrl,
            Status = PipelineJobStatus.Queued,
            StatusMessage = "Queued in distillation pipeline..."
        };

        var jobVm = new PipelineJobItemViewModel(job);
        Jobs.Insert(0, jobVm);
        InstagramUrl = string.Empty;

        OnPropertyChanged(nameof(HasJobs));
        OnPropertyChanged(nameof(HasNoJobs));
        OnPropertyChanged(nameof(ActiveJobsCount));

        // Fire-and-forget background task so UI stays completely responsive
        _ = Task.Run(async () =>
        {
            try
            {
                await _orchestrator.RunJobAsync(job).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unhandled exception in background job execution for {Url}", rawUrl);
            }
        });
    }

    [RelayCommand]
    private void ClearCompleted()
    {
        var completedList = Jobs.Where(j => j.IsDone || j.IsFailed).ToList();
        foreach (var item in completedList)
        {
            Jobs.Remove(item);
        }

        OnPropertyChanged(nameof(HasJobs));
        OnPropertyChanged(nameof(HasNoJobs));
        OnPropertyChanged(nameof(ActiveJobsCount));
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        // Update in-memory settings
        _settings.VaultFolderPath = VaultFolderPath?.Trim() ?? string.Empty;
        _settings.OllamaModelName = OllamaModelName?.Trim() ?? "llama3.2:3b";
        _settings.OllamaEndpoint = OllamaEndpoint?.Trim() ?? "http://localhost:11434";
        _settings.WhisperModelPath = WhisperModelPath?.Trim() ?? "models/ggml-base.en.bin";
        _settings.WhisperThreadCount = WhisperThreadCount > 0 ? WhisperThreadCount : 4;
        _settings.WhisperLanguage = WhisperLanguage?.Trim() ?? "en";

        // Persist to appsettings.json in application folder
        try
        {
            var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            JsonObject jsonRoot;

            if (File.Exists(appSettingsPath))
            {
                var existingJson = await File.ReadAllTextAsync(appSettingsPath);
                jsonRoot = JsonNode.Parse(existingJson)?.AsObject() ?? [];
            }
            else
            {
                jsonRoot = [];
            }

            var distillNode = new JsonObject
            {
                ["VaultFolderPath"] = _settings.VaultFolderPath,
                ["OllamaModelName"] = _settings.OllamaModelName,
                ["OllamaEndpoint"] = _settings.OllamaEndpoint,
                ["WhisperBinaryPath"] = _settings.WhisperBinaryPath,
                ["WhisperModelPath"] = _settings.WhisperModelPath,
                ["WhisperThreadCount"] = _settings.WhisperThreadCount,
                ["WhisperLanguage"] = _settings.WhisperLanguage
            };

            jsonRoot[DistillSettings.SectionName] = distillNode;

            var options = new JsonSerializerOptions { WriteIndented = true };
            await File.WriteAllTextAsync(appSettingsPath, jsonRoot.ToJsonString(options));

            IsSettingsSavedVisible = true;
            _logger?.LogInformation("Settings saved successfully to {Path}", appSettingsPath);

            // Auto-hide success badge after 3 seconds
            _ = Task.Delay(3000).ContinueWith(_ =>
            {
                _dispatcherQueue?.TryEnqueue(() => IsSettingsSavedVisible = false);
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to persist settings to appsettings.json");
        }
    }

    private void OnPipelineJobChanged(object? sender, PipelineJobChangedEventArgs e)
    {
        void UpdateJobUi()
        {
            var targetVm = Jobs.FirstOrDefault(j => j.Id == e.Job.Id);
            targetVm?.UpdateFrom(e.Job);

            OnPropertyChanged(nameof(ActiveJobsCount));
        }

        if (_dispatcherQueue != null && !_dispatcherQueue.HasThreadAccess)
        {
            _dispatcherQueue.TryEnqueue(UpdateJobUi);
        }
        else
        {
            UpdateJobUi();
        }
    }
}
