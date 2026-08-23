namespace Distill.Core.Pipeline;

/// <summary>
/// Orchestrates the full Instagram-to-Obsidian pipeline for an individual job.
/// </summary>
public interface IPipelineOrchestrator
{
    /// <summary>
    /// Event fired whenever a job's status, progress, or state changes.
    /// </summary>
    event EventHandler<PipelineJobChangedEventArgs>? JobChanged;

    /// <summary>
    /// Executes the full distillation pipeline for a job:
    /// Download -> Extract (OCR / Speech) -> Format with Ollama -> Save to Obsidian.
    /// </summary>
    /// <param name="job">The pipeline job to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The processed job with updated results.</returns>
    Task<PipelineJob> RunJobAsync(PipelineJob job, CancellationToken cancellationToken = default);
}
