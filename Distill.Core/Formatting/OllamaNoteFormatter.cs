using System.Text;
using System.Text.Json;
using Distill.Core.Configuration;
using Distill.Core.Exceptions;
using Distill.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Distill.Core.Formatting;

/// <summary>
/// Note formatter that synthesizes extracted multimodal content into clean Markdown via a local Ollama instance.
/// </summary>
public class OllamaNoteFormatter : INoteFormatter
{
    private readonly HttpClient _httpClient;
    private readonly DistillSettings _settings;
    private readonly ILogger<OllamaNoteFormatter>? _logger;

    public OllamaNoteFormatter(
        HttpClient httpClient,
        IOptions<DistillSettings> settings,
        ILogger<OllamaNoteFormatter>? logger = null)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> FormatAsync(RawExtractedContent content, CancellationToken cancellationToken = default)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        var endpoint = string.IsNullOrWhiteSpace(_settings.OllamaEndpoint)
            ? "http://localhost:11434"
            : _settings.OllamaEndpoint.TrimEnd('/');

        var generateUrl = $"{endpoint}/api/generate";
        var modelName = string.IsNullOrWhiteSpace(_settings.OllamaModelName) ? "llama3.2:3b" : _settings.OllamaModelName;

        var prompt = BuildDistillationPrompt(content);

        var requestBody = new
        {
            model = modelName,
            prompt,
            stream = false,
            options = new
            {
                temperature = 0.2
            }
        };

        var jsonPayload = JsonSerializer.Serialize(requestBody);
        _logger?.LogInformation("Sending distillation request to Ollama (Model: {Model}, Endpoint: {Url})", modelName, generateUrl);

        var maxAttempts = 2;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, generateUrl)
                {
                    Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                };

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(75));

                using var response = await _httpClient.SendAsync(request, timeoutCts.Token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    throw new DistillAiException($"Ollama API returned HTTP {response.StatusCode}: {errorContent}");
                }

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var formattedMarkdown = ParseOllamaResponse(responseJson);

                _logger?.LogInformation("Successfully synthesized note with Ollama ({Length} chars)", formattedMarkdown.Length);
                return formattedMarkdown;
            }
            catch (HttpRequestException ex) when (attempt < maxAttempts)
            {
                _logger?.LogWarning(ex, "Ollama request failed on attempt {Attempt}/{Max}. Retrying in 1s...", attempt, maxAttempts);
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (attempt == maxAttempts)
                {
                    _logger?.LogError(ex, "Failed to communicate with local Ollama at {Endpoint} after {Attempts} attempts", endpoint, maxAttempts);
                    throw new OllamaConnectionException(endpoint, ex.Message, ex);
                }
            }
        }

        throw new OllamaConnectionException(endpoint, "Failed to connect to local Ollama instance after retry.");
    }

    private static string BuildDistillationPrompt(RawExtractedContent content)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an expert technical writer and knowledge curator. Your task is to distill the following extracted raw content from an Instagram " +
                      (content.SourceType == SourceType.Reel ? "Reel" : "Post/Carousel") + 
                      " into a pristine, high-density, structured Markdown note for an Obsidian vault.");
        sb.AppendLine();
        sb.AppendLine("CRITICAL INSTRUCTIONS:");
        sb.AppendLine("1. Strip ALL social media boilerplate (e.g. 'follow for more', 'link in bio', engagement hooks, promo codes, hashtags).");
        sb.AppendLine("2. Deduplicate repeated OCR text and speech phrases across slides or video keyframes.");
        sb.AppendLine("3. Correct OCR and Speech-to-Text typos while preserving original domain technical terms and code snippets.");
        sb.AppendLine("4. Format with a clear Markdown hierarchy:");
        sb.AppendLine("   - # [Clear, Engaging Note Title]");
        sb.AppendLine("   - ## Summary (1-2 dense, informative sentences capturing the core takeaway)");
        sb.AppendLine("   - ## Key Takeaways & Concepts (Structured headings and bullet points covering all substantive information)");
        sb.AppendLine("   - ## Actionable Steps / Code / Workflow (If practical steps or code examples are present)");
        sb.AppendLine("5. Output ONLY the raw Markdown note text. Do NOT include conversational filler, introductory remarks, or closing pleasantries.");
        sb.AppendLine();
        sb.AppendLine("--- EXTRACTED RAW CONTENT ---");

        if (!string.IsNullOrWhiteSpace(content.Title))
        {
            sb.AppendLine($"Original Title: {content.Title}");
        }

        if (!string.IsNullOrWhiteSpace(content.Author))
        {
            sb.AppendLine($"Creator: {content.Author}");
        }

        if (!string.IsNullOrWhiteSpace(content.Caption))
        {
            sb.AppendLine($"Original Caption: {content.Caption}");
        }

        if (!string.IsNullOrWhiteSpace(content.TranscriptText))
        {
            sb.AppendLine("--- Spoken Transcript ---");
            sb.AppendLine(content.TranscriptText);
        }

        if (content.OcrTextSegments.Count > 0 || !string.IsNullOrWhiteSpace(content.OcrText))
        {
            sb.AppendLine("--- On-Screen Visual OCR Text ---");
            sb.AppendLine(content.OcrText);
        }

        sb.AppendLine("--- END OF EXTRACTED CONTENT ---");
        sb.AppendLine();
        sb.AppendLine("Provide the distilled Markdown note below:");

        return sb.ToString();
    }

    private static string ParseOllamaResponse(string jsonResponse)
    {
        using var doc = JsonDocument.Parse(jsonResponse);
        var root = doc.RootElement;

        if (!root.TryGetProperty("response", out var responseProp) || responseProp.GetString() is not { } markdownText)
        {
            throw new DistillAiException("Ollama response did not contain a valid 'response' field.");
        }

        var trimmed = markdownText.Trim();

        // Strip enclosing markdown code block if the LLM wrapped everything in ```markdown ... ```
        if (trimmed.StartsWith("```markdown", StringComparison.OrdinalIgnoreCase) && trimmed.EndsWith("```"))
        {
            var startIndex = trimmed.IndexOf('\n') + 1;
            var endIndex = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (startIndex > 0 && endIndex > startIndex)
            {
                trimmed = trimmed[startIndex..endIndex].Trim();
            }
        }

        return trimmed;
    }
}
