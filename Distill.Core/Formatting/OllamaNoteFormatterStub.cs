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

    public Task<string> FormatAsync(RawExtractedContent content, CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Formatting note via Ollama Stub (Model: {Model}, Endpoint: {Endpoint})", _settings.OllamaModelName, _settings.OllamaEndpoint);

        var sb = new StringBuilder();
        sb.AppendLine($"# {content.Title ?? "Distilled Instagram Note"}");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine("A distilled summary of key insights captured from Instagram.");
        sb.AppendLine();
        sb.AppendLine("## Key Takeaways");
        sb.AppendLine("- Core insight extracted from the media content.");
        sb.AppendLine("- Actionable strategy or principle.");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(content.TranscriptText))
        {
            sb.AppendLine("## Spoken Transcript");
            sb.AppendLine(content.TranscriptText);
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(content.OcrText))
        {
            sb.AppendLine("## On-Screen Text (OCR)");
            sb.AppendLine(content.OcrText);
            sb.AppendLine();
        }

        return Task.FromResult(sb.ToString());
    }
}
