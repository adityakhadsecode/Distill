using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Distill.Core.Configuration;
using Distill.Core.Diagnostics;
using Distill.Core.Pipeline;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.UI.Dispatching;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Distill.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly ISystemHealthService _healthService;
    private readonly DistillSettings _settings;
    private readonly ILogger<MainViewModel>? _logger;
    private readonly DispatcherQueue? _dispatcherQueue;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddJobCommand))]
    private string _instagramUrl = string.Empty;

    [ObservableProperty]
    private ObservableCollection<PipelineJobItemViewModel> _jobs = [];

    // Diagnostics & System Health
    [ObservableProperty]
    private SystemHealthReport _healthReport = new();

    [ObservableProperty]
    private bool _isHealthCheckRunning;

    [ObservableProperty]
    private string _healthStatusMessage = "Checking system tools and AI models...";

    [ObservableProperty]
    private bool _isUpdatingTools;

    [ObservableProperty]
    private string _toolsUpdateStatus = string.Empty;

    // Obsidian Vault Destination
    [ObservableProperty]
    private string _vaultFolderPath;

    // Ollama Synthesis & Discovery
    [ObservableProperty]
    private string _ollamaModelName;

    [ObservableProperty]
    private string _ollamaEndpoint;

    [ObservableProperty]
    private ObservableCollection<string> _installedOllamaModels = [];

    [ObservableProperty]
    private bool _isOllamaOnline;

    [ObservableProperty]
    private string _ollamaStatusText = "Connecting...";

    [ObservableProperty]
    private bool _isFetchingOllamaModels;

    // Whisper.cpp Settings & Model Downloader
    [ObservableProperty]
    private string _whisperModelPath;

    [ObservableProperty]
    private int _whisperThreadCount;

    [ObservableProperty]
    private string _whisperLanguage;

    [ObservableProperty]
    private ObservableCollection<string> _presetWhisperModels =
    [
        "models/ggml-base.en.bin",
        "models/ggml-small.en.bin",
        "models/ggml-tiny.en.bin",
        "models/ggml-medium.en.bin"
    ];

    [ObservableProperty]
    private bool _isDownloadingWhisperModel;

    [ObservableProperty]
    private double _whisperDownloadProgress;

    [ObservableProperty]
    private string _whisperDownloadStatus = string.Empty;

    // Pipeline & Automation Preferences
    [ObservableProperty]
    private bool _autoOpenInObsidian;

    [ObservableProperty]
    private int _maxConcurrentJobs;

    [ObservableProperty]
    private double _sceneChangeThreshold;

    [ObservableProperty]
    private bool _appendRawContentToNote;

    [ObservableProperty]
    private bool _isSettingsSavedVisible;

    public bool HasJobs => Jobs.Count > 0;
    public bool HasNoJobs => Jobs.Count == 0;
    public int ActiveJobsCount => Jobs.Count(j => j.IsActive);

    public MainViewModel(
        IPipelineOrchestrator orchestrator,
        ISystemHealthService healthService,
        IOptions<DistillSettings> settings,
        ILogger<MainViewModel>? logger = null)
    {
        _orchestrator = orchestrator;
        _healthService = healthService;
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
        _autoOpenInObsidian = _settings.AutoOpenInObsidian;
        _maxConcurrentJobs = _settings.MaxConcurrentJobs;
        _sceneChangeThreshold = _settings.SceneChangeThreshold;
        _appendRawContentToNote = _settings.AppendRawContentToNote;

        // Subscribe to pipeline orchestrator job changes
        _orchestrator.JobChanged += OnPipelineJobChanged;

        // Run initial diagnostics & Ollama discovery on load
        _ = RunInitialHealthCheckAsync();
    }

    private async Task RunInitialHealthCheckAsync()
    {
        await RefreshHealthReportAsync();
        await RefreshOllamaModelsAsync();
    }

    [RelayCommand]
    public async Task RefreshHealthReportAsync()
    {
        IsHealthCheckRunning = true;
        HealthStatusMessage = "Checking local tools and services...";

        try
        {
            var report = await _healthService.CheckHealthAsync(OllamaEndpoint);
            HealthReport = report;
            IsOllamaOnline = report.Ollama.IsReady;
            OllamaStatusText = report.Ollama.Details;
            HealthStatusMessage = report.AllPipelineToolsReady ? "All local tools ready" : "Some tools need attention";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error refreshing system health");
            HealthStatusMessage = "Health check failed";
        }
        finally
        {
            IsHealthCheckRunning = false;
        }
    }

    [RelayCommand]
    public async Task RefreshOllamaModelsAsync()
    {
        IsFetchingOllamaModels = true;
        try
        {
            var models = await _healthService.GetInstalledOllamaModelsAsync(OllamaEndpoint);
            InstalledOllamaModels.Clear();
            foreach (var m in models)
            {
                InstalledOllamaModels.Add(m);
            }

            IsOllamaOnline = models.Count > 0;
            OllamaStatusText = IsOllamaOnline ? $"Connected ({models.Count} models)" : "Offline / No models found";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching Ollama models");
            IsOllamaOnline = false;
            OllamaStatusText = "Offline (Ollama service not running)";
        }
        finally
        {
            IsFetchingOllamaModels = false;
        }
    }

    [RelayCommand]
    public async Task DownloadMissingToolsAsync()
    {
        IsUpdatingTools = true;
        ToolsUpdateStatus = "Starting tools download...";

        try
        {
            var progress = new Progress<string>(msg =>
            {
                _dispatcherQueue?.TryEnqueue(() => ToolsUpdateStatus = msg);
            });

            await _healthService.DownloadMissingToolsAsync(progress);
            await RefreshHealthReportAsync();
            ToolsUpdateStatus = "All tools updated successfully!";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating tools");
            ToolsUpdateStatus = $"Update failed: {ex.Message}";
        }
        finally
        {
            IsUpdatingTools = false;
        }
    }

    [RelayCommand]
    public async Task DownloadWhisperModelAsync()
    {
        var modelFile = Path.GetFileName(WhisperModelPath);
        if (string.IsNullOrWhiteSpace(modelFile) || !modelFile.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
        {
            modelFile = "ggml-base.en.bin";
        }

        IsDownloadingWhisperModel = true;
        WhisperDownloadProgress = 0;
        WhisperDownloadStatus = $"Downloading {modelFile}...";

        try
        {
            var progress = new Progress<double>(pct =>
            {
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    WhisperDownloadProgress = pct;
                    WhisperDownloadStatus = $"Downloading {modelFile} ({pct:F1}%)...";
                });
            });

            var savedPath = await _healthService.DownloadWhisperModelAsync(modelFile, progress);
            WhisperModelPath = $"models/{modelFile}";
            WhisperDownloadStatus = $"Downloaded to {savedPath}!";
            await RefreshHealthReportAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to download Whisper model");
            WhisperDownloadStatus = $"Download failed: {ex.Message}";
        }
        finally
        {
            IsDownloadingWhisperModel = false;
        }
    }

    [RelayCommand]
    public async Task BrowseVaultFolderAsync(nint windowHandle)
    {
        try
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                ViewMode = PickerViewMode.List
            };
            picker.FileTypeFilter.Add("*");

            if (windowHandle != 0)
            {
                InitializeWithWindow.Initialize(picker, windowHandle);
            }

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                VaultFolderPath = folder.Path;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to pick vault folder");
        }
    }

    [RelayCommand]
    public async Task BrowseWhisperModelFileAsync(nint windowHandle)
    {
        try
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                ViewMode = PickerViewMode.List
            };
            picker.FileTypeFilter.Add(".bin");

            if (windowHandle != 0)
            {
                InitializeWithWindow.Initialize(picker, windowHandle);
            }

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                WhisperModelPath = file.Path;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to pick whisper model file");
        }
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
                var finishedJob = await _orchestrator.RunJobAsync(job).ConfigureAwait(false);

                // Auto-open in Obsidian if enabled
                if (finishedJob.Status == PipelineJobStatus.Done && 
                    _settings.AutoOpenInObsidian && 
                    !string.IsNullOrWhiteSpace(finishedJob.ObsidianUri))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = finishedJob.ObsidianUri,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to auto-open note via Obsidian URI: {Uri}", finishedJob.ObsidianUri);
                    }
                }
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
    public async Task SaveSettingsAsync()
    {
        // Update in-memory settings
        _settings.VaultFolderPath = VaultFolderPath?.Trim() ?? string.Empty;
        _settings.OllamaModelName = OllamaModelName?.Trim() ?? "llama3.2:3b";
        _settings.OllamaEndpoint = OllamaEndpoint?.Trim() ?? "http://localhost:11434";
        _settings.WhisperModelPath = WhisperModelPath?.Trim() ?? "models/ggml-base.en.bin";
        _settings.WhisperThreadCount = WhisperThreadCount > 0 ? WhisperThreadCount : 4;
        _settings.WhisperLanguage = WhisperLanguage?.Trim() ?? "en";
        _settings.AutoOpenInObsidian = AutoOpenInObsidian;
        _settings.MaxConcurrentJobs = MaxConcurrentJobs > 0 ? MaxConcurrentJobs : 2;
        _settings.SceneChangeThreshold = SceneChangeThreshold > 0 ? SceneChangeThreshold : 0.3;
        _settings.AppendRawContentToNote = AppendRawContentToNote;

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
                ["WhisperLanguage"] = _settings.WhisperLanguage,
                ["AutoOpenInObsidian"] = _settings.AutoOpenInObsidian,
                ["MaxConcurrentJobs"] = _settings.MaxConcurrentJobs,
                ["SceneChangeThreshold"] = _settings.SceneChangeThreshold,
                ["AppendRawContentToNote"] = _settings.AppendRawContentToNote
            };

            jsonRoot[DistillSettings.SectionName] = distillNode;

            var options = new JsonSerializerOptions { WriteIndented = true };
            await File.WriteAllTextAsync(appSettingsPath, jsonRoot.ToJsonString(options));

            IsSettingsSavedVisible = true;
            _logger?.LogInformation("Settings saved successfully to {Path}", appSettingsPath);

            // Re-check health and models with new endpoint
            _ = RefreshHealthReportAsync();
            _ = RefreshOllamaModelsAsync();

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
