namespace Distill.Core.Models;

/// <summary>
/// Base class representing downloaded Instagram media in a temporary working directory.
/// </summary>
public abstract record DownloadResult : IDisposable, IAsyncDisposable
{
    public required string SourceUrl { get; init; }
    public required string WorkingDirectory { get; init; }
    public string? Title { get; init; }
    public string? Author { get; init; }
    public string? Caption { get; init; }
    public DateTime DownloadedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Explicitly cleans up and deletes all temporary files and directories associated with this download.
    /// Does not delete automatically so pipeline stages can complete their work.
    /// </summary>
    public virtual void Cleanup()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(WorkingDirectory) && Directory.Exists(WorkingDirectory))
            {
                Directory.Delete(WorkingDirectory, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup
        }
    }

    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync()
    {
        Cleanup();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
