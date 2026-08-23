namespace Distill.Core.Pipeline;

/// <summary>
/// Status of a distillation pipeline job.
/// </summary>
public enum PipelineJobStatus
{
    Queued,
    Downloading,
    Extracting,
    Formatting,
    Done,
    Failed
}

/// <summary>
/// Represents a distillation pipeline job from URL ingestion to vault storage.
/// </summary>
public class PipelineJob
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Url { get; init; } = string.Empty;
    public string? Title { get; set; }
    public PipelineJobStatus Status { get; set; } = PipelineJobStatus.Queued;
    public int ProgressPercent { get; set; } = 0;
    public string StatusMessage { get; set; } = "Queued in distillation pipeline...";
    public string? ErrorMessage { get; set; }
    public string? GeneratedNotePath { get; set; }
    public string? ObsidianUri { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Event arguments fired when a pipeline job updates its progress or stage.
/// </summary>
public class PipelineJobChangedEventArgs : EventArgs
{
    public PipelineJob Job { get; }

    public PipelineJobChangedEventArgs(PipelineJob job)
    {
        Job = job;
    }
}
