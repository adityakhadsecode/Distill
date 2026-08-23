using Microsoft.Extensions.Logging;

namespace Distill.Core.Ocr;

/// <summary>
/// Stub implementation of <see cref="ITextExtractor"/> representing Windows.Media.Ocr engine.
/// </summary>
public class WindowsMediaOcrExtractorStub : ITextExtractor
{
    private readonly ILogger<WindowsMediaOcrExtractorStub>? _logger;

    public WindowsMediaOcrExtractorStub(ILogger<WindowsMediaOcrExtractorStub>? logger = null)
    {
        _logger = logger;
    }

    public Task<string> ExtractTextAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Executing WindowsMediaOcrExtractorStub on image: {ImagePath}", imagePath);
        return Task.FromResult($"[OCR Extracted Text Stub for: {Path.GetFileName(imagePath)}]");
    }

    public async Task<IReadOnlyDictionary<string, string>> ExtractTextFromMultipleAsync(
        IEnumerable<string> imagePaths,
        CancellationToken cancellationToken = default)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in imagePaths)
        {
            dict[path] = await ExtractTextAsync(path, cancellationToken).ConfigureAwait(false);
        }

        return dict;
    }
}
