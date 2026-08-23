using System.Collections.Concurrent;
using Distill.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;

namespace Distill.Core.Ocr;

/// <summary>
/// Native Windows OCR text extractor using <see cref="Windows.Media.Ocr.OcrEngine"/>.
/// </summary>
public class WindowsMediaOcrExtractor : ITextExtractor
{
    private readonly ILogger<WindowsMediaOcrExtractor>? _logger;
    private readonly Lazy<OcrEngine> _ocrEngineLazy;

    public WindowsMediaOcrExtractor(ILogger<WindowsMediaOcrExtractor>? logger = null)
    {
        _logger = logger;
        _ocrEngineLazy = new Lazy<OcrEngine>(InitializeOcrEngine);
    }

    /// <summary>
    /// Extracts on-screen text from a single image file, preserving rough reading order.
    /// </summary>
    public async Task<string> ExtractTextAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            throw new ArgumentException("Image path cannot be null or empty.", nameof(imagePath));
        }

        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException($"Image file was not found for OCR: '{imagePath}'", imagePath);
        }

        _logger?.LogDebug("Running Windows.Media.Ocr on: {ImagePath}", imagePath);

        var engine = _ocrEngineLazy.Value;
        using var softwareBitmap = await LoadSoftwareBitmapAsync(imagePath, cancellationToken).ConfigureAwait(false);

        var ocrResult = await engine.RecognizeAsync(softwareBitmap).AsTask(cancellationToken).ConfigureAwait(false);
        if (ocrResult == null || ocrResult.Lines.Count == 0)
        {
            _logger?.LogDebug("No text detected in {ImagePath}", imagePath);
            return string.Empty;
        }

        // Preserve rough reading order: top-to-bottom, left-to-right
        // Group lines into roughly 12px vertical bands to handle slight tilts while maintaining line flow
        var orderedLines = ocrResult.Lines
            .Select(line =>
            {
                var top = line.Words.Count > 0 ? line.Words.Min(w => w.BoundingRect.Y) : 0.0;
                var left = line.Words.Count > 0 ? line.Words.Min(w => w.BoundingRect.X) : 0.0;
                return new { Text = line.Text.Trim(), Top = top, Left = left };
            })
            .Where(l => !string.IsNullOrWhiteSpace(l.Text))
            .OrderBy(l => Math.Round(l.Top / 12.0) * 12.0)
            .ThenBy(l => l.Left)
            .Select(l => l.Text);

        var combinedText = string.Join(Environment.NewLine, orderedLines);
        _logger?.LogDebug("Extracted {Length} characters from {ImagePath}", combinedText.Length, imagePath);

        return combinedText;
    }

    /// <summary>
    /// Extracts on-screen text from a batch of images concurrently and returns results keyed by file path.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, string>> ExtractTextFromMultipleAsync(
        IEnumerable<string> imagePaths,
        CancellationToken cancellationToken = default)
    {
        var pathsList = imagePaths?.ToList() ?? [];
        if (pathsList.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        _logger?.LogInformation("Starting batch OCR extraction for {Count} images", pathsList.Count);

        var results = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var maxConcurrency = Math.Clamp(Environment.ProcessorCount, 1, 4);

        await Parallel.ForEachAsync(
            pathsList,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = maxConcurrency,
                CancellationToken = cancellationToken
            },
            async (path, ct) =>
            {
                try
                {
                    var text = await ExtractTextAsync(path, ct).ConfigureAwait(false);
                    results[path] = text;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to extract OCR text from image: {Path}", path);
                    results[path] = string.Empty;
                }
            }).ConfigureAwait(false);

        return results;
    }

    private static OcrEngine InitializeOcrEngine()
    {
        // 1. Try to create engine from current user's profile languages
        var engine = OcrEngine.TryCreateFromUserProfileLanguages();

        // 2. Fallback to any installed recognizer language
        if (engine == null)
        {
            var availableLanguages = OcrEngine.AvailableRecognizerLanguages;
            if (availableLanguages.Count > 0)
            {
                engine = OcrEngine.TryCreateFromLanguage(availableLanguages[0]);
            }
        }

        // 3. If no OCR language is installed, throw a clear actionable exception
        if (engine == null)
        {
            throw new OcrLanguageNotInstalledException(
                "No Windows OCR language pack is installed or available on this machine.\n" +
                "To enable local OCR, please install a language pack with Optical Character Recognition support via:\n" +
                "Windows Settings > Time & Language > Language & region > Add a language (e.g., English (United States) with OCR).");
        }

        return engine;
    }

    private static async Task<SoftwareBitmap> LoadSoftwareBitmapAsync(string imagePath, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(imagePath);
        var storageFile = await StorageFile.GetFileFromPathAsync(fullPath).AsTask(cancellationToken).ConfigureAwait(false);

        using var stream = await storageFile.OpenAsync(FileAccessMode.Read).AsTask(cancellationToken).ConfigureAwait(false);
        var decoder = await BitmapDecoder.CreateAsync(stream).AsTask(cancellationToken).ConfigureAwait(false);

        var softwareBitmap = await decoder.GetSoftwareBitmapAsync().AsTask(cancellationToken).ConfigureAwait(false);

        // Windows.Media.Ocr requires Bgra8, Rgba8, or Gray8 format
        if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
            softwareBitmap.BitmapAlphaMode == BitmapAlphaMode.Straight)
        {
            var converted = SoftwareBitmap.Convert(
                softwareBitmap,
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied);

            softwareBitmap.Dispose();
            return converted;
        }

        return softwareBitmap;
    }
}
