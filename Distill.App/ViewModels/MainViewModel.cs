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
    private bool _hasUrlError;

    [ObservableProperty]
    private string _urlValidationMessage = string.Empty;

    [ObservableProperty]
    private bool _hasCompletedOnboarding;

    [ObservableProperty]
    private bool _isOnboardingActive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsExtractViewActive))]
    [NotifyPropertyChangedFor(nameof(IsOnboardingViewActive))]
    [NotifyPropertyChangedFor(nameof(IsSettingsViewActive))]
    private string _currentViewTag = "Extract";

    [ObservableProperty]
    private ObservableCollection<PipelineJobItemViewModel> _jobs = [];

    // Diagnostics & System Health
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWhisperReady))]
    [NotifyPropertyChangedFor(nameof(IsOcrReady))]
    [NotifyPropertyChangedFor(nameof(IsAllSetupReady))]
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
    [NotifyPropertyChangedFor(nameof(IsVaultConfigured))]
    [NotifyPropertyChangedFor(nameof(IsAllSetupReady))]
    private string _vaultFolderPath;

    // Ollama Synthesis & Discovery
    [ObservableProperty]
    private string _ollamaModelName;

    [ObservableProperty]
    private string _ollamaEndpoint;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOllamaReady))]
    [NotifyPropertyChangedFor(nameof(IsAllSetupReady))]
    private ObservableCollection<string> _installedOllamaModels = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOllamaReady))]
    [NotifyPropertyChangedFor(nameof(IsAllSetupReady))]
    private bool _isOllamaOnline;

    [ObservableProperty]
    private string _ollamaStatusText = "Connecting...";

    [ObservableProperty]
    private bool _isFetchingOllamaModels;

    // Binary Executable Custom Paths
    [ObservableProperty]
    private string _ytDlpBinaryPath;

    [ObservableProperty]
    private string _ffmpegBinaryPath;

    [ObservableProperty]
    private string _whisperBinaryPath;

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

    // Theme & Appearance
    [ObservableProperty]
    private string _selectedTheme = "Default";

    // Drawer State
    [ObservableProperty]
    private bool _isSettingsDrawerOpen;

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
    public string JobsCountText => Jobs.Count.ToString();

    public bool IsExtractViewActive => CurrentViewTag == "Extract";
    public bool IsOnboardingViewActive => CurrentViewTag == "Onboarding";
    public bool IsSettingsViewActive => CurrentViewTag == "Settings";

    public bool IsVaultConfigured => !string.IsNullOrWhiteSpace(VaultFolderPath);
    public bool IsOllamaReady => IsOllamaOnline && InstalledOllamaModels.Count > 0;
    public bool IsWhisperReady => HealthReport.Whisper.IsReady;
    public bool IsOcrReady => HealthReport.WindowsOcr.IsReady;
    public bool IsAllSetupReady => IsVaultConfigured && IsOllamaReady && IsWhisperReady && IsOcrReady;

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
        _hasCompletedOnboarding = _settings.HasCompletedOnboarding;
        _isOnboardingActive = !_settings.HasCompletedOnboarding;
        _currentViewTag = _settings.HasCompletedOnboarding ? "Extract" : "Onboarding";

        _vaultFolderPath = _settings.VaultFolderPath;
        _ollamaModelName = _settings.OllamaModelName;
        _ollamaEndpoint = _settings.OllamaEndpoint;
        _ytDlpBinaryPath = _settings.YtDlpBinaryPath;
        _ffmpegBinaryPath = _settings.FfmpegBinaryPath;
        _whisperBinaryPath = _settings.WhisperBinaryPath;
        _whisperModelPath = _settings.WhisperModelPath;
        _whisperThreadCount = _settings.WhisperThreadCount;
        _whisperLanguage = _settings.WhisperLanguage;
        _autoOpenInObsidian = _settings.AutoOpenInObsidian;
        _maxConcurrentJobs = _settings.MaxConcurrentJobs;
        _sceneChangeThreshold = _settings.SceneChangeThreshold;
        _appendRawContentToNote = _settings.AppendRawContentToNote;
        _selectedTheme = string.IsNullOrWhiteSpace(_settings.SelectedTheme) ? "Default" : _settings.SelectedTheme;

        // Subscribe to pipeline orchestrator job changes
        _orchestrator.JobChanged += OnPipelineJobChanged;

        // Run initial diagnostics & Ollama discovery on load
        _ = RunInitialHealthCheckAsync();
    }

    partial void OnInstagramUrlChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            HasUrlError = false;
            UrlValidationMessage = string.Empty;
        }
        else if (!IsValidInstagramUrl(value))
        {
            HasUrlError = true;
            UrlValidationMessage = "Enter a valid Instagram link (e.g. instagram.com/reel/... or instagram.com/p/...)";
        }
        else
        {
            HasUrlError = false;
            UrlValidationMessage = string.Empty;
        }
    }

    public static bool IsValidInstagramUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        var trimmed = url.Trim();
        return trimmed.Contains("instagram.com/", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Contains("instagr.am/", StringComparison.OrdinalIgnoreCase);
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
    public async Task DownloadYtDlpAsync()
    {
        IsUpdatingTools = true;
        ToolsUpdateStatus = "Downloading yt-dlp...";

        try
        {
            var progress = new Progress<string>(msg =>
            {
                _dispatcherQueue?.TryEnqueue(() => ToolsUpdateStatus = msg);
            });

            await _healthService.DownloadOrUpdateYtDlpAsync(progress);
            await RefreshHealthReportAsync();
            ToolsUpdateStatus = "yt-dlp updated successfully!";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error downloading yt-dlp");
            ToolsUpdateStatus = $"yt-dlp update failed: {ex.Message}";
        }
        finally
        {
            IsUpdatingTools = false;
        }
    }

    [RelayCommand]
    public async Task DownloadFfmpegAsync()
    {
        IsUpdatingTools = true;
        ToolsUpdateStatus = "Downloading ffmpeg...";

        try
        {
            var progress = new Progress<string>(msg =>
            {
                _dispatcherQueue?.TryEnqueue(() => ToolsUpdateStatus = msg);
            });

            await _healthService.DownloadOrUpdateFfmpegAsync(progress);
            await RefreshHealthReportAsync();
            ToolsUpdateStatus = "ffmpeg installed successfully!";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error downloading ffmpeg");
            ToolsUpdateStatus = $"ffmpeg download failed: {ex.Message}";
        }
        finally
        {
            IsUpdatingTools = false;
        }
    }

    [RelayCommand]
    public async Task DownloadWhisperAsync()
    {
        IsUpdatingTools = true;
        ToolsUpdateStatus = "Downloading whisper.cpp binaries...";

        try
        {
            var progress = new Progress<string>(msg =>
            {
                _dispatcherQueue?.TryEnqueue(() => ToolsUpdateStatus = msg);
            });

            await _healthService.DownloadOrUpdateWhisperAsync(progress);
            await RefreshHealthReportAsync();
            ToolsUpdateStatus = "whisper.cpp installed successfully!";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error downloading whisper.cpp");
            ToolsUpdateStatus = $"whisper.cpp download failed: {ex.Message}";
        }
        finally
        {
            IsUpdatingTools = false;
        }
    }

    [RelayCommand]
    public void OpenOcrSettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:regionlanguage",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to launch Windows language settings URI");
        }
    }

    [RelayCommand]
    public void OpenOllamaWebsite()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://ollama.com",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to open Ollama website");
        }
    }

    [RelayCommand]
    public async Task DownloadWhisperModelAsync(string? modelName = null)
    {
        var modelFile = !string.IsNullOrWhiteSpace(modelName) ? modelName : Path.GetFileName(WhisperModelPath);
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
    public async Task BrowseYtDlpBinaryFileAsync(nint windowHandle)
    {
        try
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
                ViewMode = PickerViewMode.List
            };
            picker.FileTypeFilter.Add(".exe");

            if (windowHandle != 0)
            {
                InitializeWithWindow.Initialize(picker, windowHandle);
            }

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                YtDlpBinaryPath = file.Path;
                _settings.YtDlpBinaryPath = file.Path;
                _ = RefreshHealthReportAsync();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to pick yt-dlp executable");
        }
    }

    [RelayCommand]
    public void ClearYtDlpBinary()
    {
        YtDlpBinaryPath = string.Empty;
        _settings.YtDlpBinaryPath = string.Empty;
        _ = RefreshHealthReportAsync();
    }

    [RelayCommand]
    public async Task BrowseFfmpegBinaryFileAsync(nint windowHandle)
    {
        try
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
                ViewMode = PickerViewMode.List
            };
            picker.FileTypeFilter.Add(".exe");

            if (windowHandle != 0)
            {
                InitializeWithWindow.Initialize(picker, windowHandle);
            }

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                FfmpegBinaryPath = file.Path;
                _settings.FfmpegBinaryPath = file.Path;
                _ = RefreshHealthReportAsync();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to pick ffmpeg executable");
        }
    }

    [RelayCommand]
    public void ClearFfmpegBinary()
    {
        FfmpegBinaryPath = string.Empty;
        _settings.FfmpegBinaryPath = string.Empty;
        _ = RefreshHealthReportAsync();
    }

    [RelayCommand]
    public async Task BrowseWhisperBinaryFileAsync(nint windowHandle)
    {
        try
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
                ViewMode = PickerViewMode.List
            };
            picker.FileTypeFilter.Add(".exe");

            if (windowHandle != 0)
            {
                InitializeWithWindow.Initialize(picker, windowHandle);
            }

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                WhisperBinaryPath = file.Path;
                _settings.WhisperBinaryPath = file.Path;
                _ = RefreshHealthReportAsync();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to pick whisper executable");
        }
    }

    [RelayCommand]
    public void ClearWhisperBinary()
    {
        WhisperBinaryPath = string.Empty;
        _settings.WhisperBinaryPath = string.Empty;
        _ = RefreshHealthReportAsync();
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

    private bool CanAddJob => !string.IsNullOrWhiteSpace(InstagramUrl) && !HasUrlError;

    [RelayCommand]
    public async Task CompleteOnboardingAsync()
    {
        HasCompletedOnboarding = true;
        IsOnboardingActive = false;
        CurrentViewTag = "Extract";
        await SaveSettingsAsync();
    }

    [RelayCommand]
    public void ShowOnboardingAgain()
    {
        IsOnboardingActive = true;
        CurrentViewTag = "Onboarding";
    }

    [RelayCommand]
    public void NavigateTo(string? tag)
    {
        var target = tag ?? "Extract";
        CurrentViewTag = target;
        IsOnboardingActive = target == "Onboarding";
    }

    [RelayCommand]
    public void NavigateToExtract()
    {
        NavigateTo("Extract");
    }

    [RelayCommand]
    public void NavigateToSettings()
    {
        NavigateTo("Settings");
    }

    [RelayCommand]
    public void NavigateToOnboarding()
    {
        NavigateTo("Onboarding");
    }

    [RelayCommand]
    public async Task PasteFromClipboardAsync()
    {
        try
        {
            var dataPackageView = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
            if (dataPackageView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text))
            {
                var text = await dataPackageView.GetTextAsync();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    InstagramUrl = text.Trim();
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to read clipboard text");
        }
    }

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
        HasUrlError = false;
        UrlValidationMessage = string.Empty;

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
    public void ToggleSettingsDrawer()
    {
        IsSettingsDrawerOpen = !IsSettingsDrawerOpen;
    }

    [RelayCommand]
    public void OpenSettingsDrawer()
    {
        IsSettingsDrawerOpen = true;
    }

    [RelayCommand]
    public void CloseSettingsDrawer()
    {
        IsSettingsDrawerOpen = false;
    }

    [RelayCommand]
    public async Task CycleThemeAsync()
    {
        SelectedTheme = SelectedTheme switch
        {
            "Default" => "Dark",
            "Dark" => "Light",
            _ => "Default"
        };
        await SaveSettingsAsync();
    }

    [RelayCommand]
    public async Task SaveSettingsAsync()
    {
        // Update in-memory settings
        _settings.HasCompletedOnboarding = HasCompletedOnboarding;
        _settings.VaultFolderPath = VaultFolderPath?.Trim() ?? string.Empty;
        _settings.OllamaModelName = OllamaModelName?.Trim() ?? "llama3.2:3b";
        _settings.OllamaEndpoint = OllamaEndpoint?.Trim() ?? "http://localhost:11434";
        _settings.YtDlpBinaryPath = YtDlpBinaryPath?.Trim() ?? string.Empty;
        _settings.FfmpegBinaryPath = FfmpegBinaryPath?.Trim() ?? string.Empty;
        _settings.WhisperBinaryPath = WhisperBinaryPath?.Trim() ?? string.Empty;
        _settings.WhisperModelPath = WhisperModelPath?.Trim() ?? "models/ggml-base.en.bin";
        _settings.WhisperThreadCount = WhisperThreadCount > 0 ? WhisperThreadCount : 4;
        _settings.WhisperLanguage = WhisperLanguage?.Trim() ?? "en";
        _settings.AutoOpenInObsidian = AutoOpenInObsidian;
        _settings.MaxConcurrentJobs = MaxConcurrentJobs > 0 ? MaxConcurrentJobs : 2;
        _settings.SceneChangeThreshold = SceneChangeThreshold > 0 ? SceneChangeThreshold : 0.3;
        _settings.AppendRawContentToNote = AppendRawContentToNote;
        _settings.SelectedTheme = SelectedTheme;

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
                ["HasCompletedOnboarding"] = _settings.HasCompletedOnboarding,
                ["VaultFolderPath"] = _settings.VaultFolderPath,
                ["OllamaModelName"] = _settings.OllamaModelName,
                ["OllamaEndpoint"] = _settings.OllamaEndpoint,
                ["YtDlpBinaryPath"] = _settings.YtDlpBinaryPath,
                ["FfmpegBinaryPath"] = _settings.FfmpegBinaryPath,
                ["WhisperBinaryPath"] = _settings.WhisperBinaryPath,
                ["WhisperModelPath"] = _settings.WhisperModelPath,
                ["WhisperThreadCount"] = _settings.WhisperThreadCount,
                ["WhisperLanguage"] = _settings.WhisperLanguage,
                ["AutoOpenInObsidian"] = _settings.AutoOpenInObsidian,
                ["MaxConcurrentJobs"] = _settings.MaxConcurrentJobs,
                ["SceneChangeThreshold"] = _settings.SceneChangeThreshold,
                ["AppendRawContentToNote"] = _settings.AppendRawContentToNote,
                ["SelectedTheme"] = _settings.SelectedTheme
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
