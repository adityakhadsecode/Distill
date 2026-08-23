using Distill.Core.Models;

namespace Distill.Core.Formatting;

/// <summary>
/// Formats and synthesizes extracted raw OCR/transcript content into structured Markdown using a local LLM.
/// </summary>
public interface INoteFormatter
{
    /// <summary>
    /// Distills raw extracted text into structured Markdown notes via local Ollama inference.
    /// </summary>
    /// <param name="content">Raw extracted OCR text and spoken transcript.</param>
    /// <param name="metadata">Note metadata and attributes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Clean, synthesized Markdown content.</returns>
    Task<string> FormatNoteAsync(ExtractedContent content, NoteMetadata metadata, CancellationToken cancellationToken = default);
}
