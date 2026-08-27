using Distill.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Distill.Core.Process;

/// <summary>
/// Default tool locator looking for custom user-configured binary paths, then in a "tools" folder adjacent to the application,
/// and falling back to system PATH.
/// </summary>
public class ToolLocator : IToolLocator
{
    private readonly string _toolsDirectory;
    private readonly IOptions<DistillSettings>? _settingsOptions;
    private readonly ILogger<ToolLocator>? _logger;

    public ToolLocator(
        IOptions<DistillSettings>? settingsOptions = null,
        string? customToolsDirectory = null,
        ILogger<ToolLocator>? logger = null)
    {
        _settingsOptions = settingsOptions;
        _toolsDirectory = customToolsDirectory ?? Path.Combine(AppContext.BaseDirectory, "tools");
        _logger = logger;
    }

    public string ResolveToolPath(string toolFileName)
    {
        var settings = _settingsOptions?.Value;

        // 1. Check user-configured custom paths in settings
        if (settings != null)
        {
            var customPath = GetCustomPathForTool(toolFileName, settings);
            if (!string.IsNullOrWhiteSpace(customPath))
            {
                if (File.Exists(customPath))
                {
                    _logger?.LogDebug("Resolved custom tool path for {Tool} at: {Path}", toolFileName, customPath);
                    return customPath;
                }

                _logger?.LogWarning("Configured custom path for {Tool} does not exist: {Path}. Falling back to auto-detection.", toolFileName, customPath);
            }
        }

        // 2. Check in configured/bundled /tools directory
        var bundledPath = Path.Combine(_toolsDirectory, toolFileName);
        if (File.Exists(bundledPath))
        {
            _logger?.LogDebug("Resolved bundled tool at: {Path}", bundledPath);
            return bundledPath;
        }

        // 3. Check in app base directory directly
        var directPath = Path.Combine(AppContext.BaseDirectory, toolFileName);
        if (File.Exists(directPath))
        {
            _logger?.LogDebug("Resolved tool in base dir at: {Path}", directPath);
            return directPath;
        }

        // 4. Fallback to system PATH resolution
        _logger?.LogDebug("Tool {Tool} not found in tools folder; using system command fallback", toolFileName);
        return toolFileName;
    }

    private static string? GetCustomPathForTool(string toolFileName, DistillSettings settings)
    {
        var name = Path.GetFileNameWithoutExtension(toolFileName).ToLowerInvariant();
        return name switch
        {
            "yt-dlp" => settings.YtDlpBinaryPath,
            "ffmpeg" => settings.FfmpegBinaryPath,
            "whisper-cli" or "whisper" or "main" => settings.WhisperBinaryPath,
            _ => null
        };
    }
}
