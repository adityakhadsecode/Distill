using Microsoft.Extensions.Logging;

namespace Distill.Core.Process;

/// <summary>
/// Default tool locator looking for binaries in a "tools" folder adjacent to the application,
/// then falling back to system PATH.
/// </summary>
public class ToolLocator : IToolLocator
{
    private readonly string _toolsDirectory;
    private readonly ILogger<ToolLocator>? _logger;

    public ToolLocator(string? customToolsDirectory = null, ILogger<ToolLocator>? logger = null)
    {
        _toolsDirectory = customToolsDirectory ?? Path.Combine(AppContext.BaseDirectory, "tools");
        _logger = logger;
    }

    public string ResolveToolPath(string toolFileName)
    {
        // 1. Check in configured/bundled /tools directory
        var bundledPath = Path.Combine(_toolsDirectory, toolFileName);
        if (File.Exists(bundledPath))
        {
            _logger?.LogDebug("Resolved bundled tool at: {Path}", bundledPath);
            return bundledPath;
        }

        // 2. Check in app base directory directly
        var directPath = Path.Combine(AppContext.BaseDirectory, toolFileName);
        if (File.Exists(directPath))
        {
            _logger?.LogDebug("Resolved tool in base dir at: {Path}", directPath);
            return directPath;
        }

        // 3. Fallback to system PATH resolution
        _logger?.LogDebug("Tool {Tool} not found in tools folder; using system command fallback", toolFileName);
        return toolFileName;
    }
}
