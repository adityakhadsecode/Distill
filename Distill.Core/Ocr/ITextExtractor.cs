namespace Distill.Core.Ocr;

/// <summary>
/// Extracts text from images using Optical Character Recognition (OCR).
/// </summary>
public interface ITextExtractor
{
    /// <summary>
    /// Extracts on-screen text from a single image file, preserving rough reading order.
    /// </summary>
    /// <param name="imagePath">Absolute path to the image file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The recognized text.</returns>
    Task<string> ExtractTextAsync(string imagePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts on-screen text from a batch of images concurrently and returns results keyed by file path.
    /// </summary>
    /// <param name="imagePaths">Collection of image file paths.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary mapping image file path to recognized text.</returns>
    Task<IReadOnlyDictionary<string, string>> ExtractTextFromMultipleAsync(
        IEnumerable<string> imagePaths,
        CancellationToken cancellationToken = default);
}
