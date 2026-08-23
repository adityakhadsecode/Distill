namespace Distill.Core.Models;

/// <summary>
/// Aggregates raw text extracted from OCR (frames/images) and Speech-to-Text (audio transcript).
/// </summary>
public record ExtractedContent
{
    public string SpokenTranscript { get; init; } = string.Empty;
    public IReadOnlyList<string> OcrTextSegments { get; init; } = [];
    public string CombinedOcrText => string.Join(Environment.NewLine, OcrTextSegments);
    public string RawCaption { get; init; } = string.Empty;
}
