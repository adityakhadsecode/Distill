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
}
