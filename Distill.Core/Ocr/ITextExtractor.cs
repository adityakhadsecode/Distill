namespace Distill.Core.Ocr;

/// <summary>
/// Extracts text from an image using Optical Character Recognition (OCR).
/// </summary>
public interface ITextExtractor
{
    /// <summary>
    /// Extracts on-screen text from an image file.
    /// </summary>
    /// <param name="imagePath">Absolute path to the image file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The raw extracted text.</returns>
    Task<string> ExtractTextAsync(string imagePath, CancellationToken cancellationToken = default);
}
