namespace Distill.Core.Exceptions;

/// <summary>
/// Base exception for Optical Character Recognition (OCR) errors.
/// </summary>
public class OcrException : Exception
{
    public OcrException(string message) : base(message) { }
    public OcrException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when no Windows OCR language pack is installed on the system for the user's profile languages.
/// </summary>
public class OcrLanguageNotInstalledException : OcrException
{
    public OcrLanguageNotInstalledException(string message) : base(message) { }
    public OcrLanguageNotInstalledException(string message, Exception innerException) : base(message, innerException) { }
}
