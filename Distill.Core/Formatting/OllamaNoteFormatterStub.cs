using System.Text;
using Distill.Core.Configuration;
using Distill.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Distill.Core.Formatting;

/// <summary>
/// Stub implementation of <see cref="INoteFormatter"/> representing local Ollama inference.
/// </summary>
public class OllamaNoteFormatterStub : INoteFormatter
{
    private readonly DistillSettings _settings;
    private readonly ILogger<OllamaNoteFormatterStub>? _logger;

    public OllamaNoteFormatterStub(IOptions<DistillSettings> settings, ILogger<OllamaNoteFormatterStub>? logger = null)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public Task<string> FormatNoteAsync(ExtractedContent content, NoteMetadata metadata, CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Formatting note via Ollama Stub (Model: {Model}, Endpoint: {Endpoint})", _settings.OllamaModelName, _settings.OllamaEndpoint);

        var sb = new StringBuilder();
        sb.AppendLine($"# {metadata.Title}");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine(metadata.Summary ?? "Distilled overview of the captured content.");
        sb.AppendLine();
        sb.AppendLine("## Key Takeaways");
        sb.AppendLine("- Point 1: Essential concept extracted from the post.");
        sb.AppendLine("- Point 2: Actionable technique or workflow detail.");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(content.SpokenTranscript))
        {
            sb.AppendLine("## Spoken Transcript");
            sb.AppendLine(content.SpokenTranscript);
            sb.AppendLine();
        }

        if (content.OcrTextSegments.Count > 0)
        {
            sb.AppendLine("## On-Screen Text (OCR)");
            sb.AppendLine(content.CombinedOcrText);
            sb.AppendLine();
        }

        return Task.FromResult(sb.ToString());
    }
}
