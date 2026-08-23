namespace Distill.Core.Exceptions;

/// <summary>
/// Base exception for Instagram download and media processing failures.
/// </summary>
public class DistillDownloadException : Exception
{
    public DistillDownloadException(string message) : base(message) { }
    public DistillDownloadException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when the Instagram post or reel requires login, is private, or is age-restricted.
/// </summary>
public class PrivateMediaException : DistillDownloadException
{
    public PrivateMediaException(string message) : base(message) { }
    public PrivateMediaException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when Instagram rate limits requests (HTTP 429 / temporary block).
/// </summary>
public class RateLimitException : DistillDownloadException
{
    public RateLimitException(string message) : base(message) { }
    public RateLimitException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Thrown when the specified Instagram post or reel does not exist or has been deleted.
/// </summary>
public class MediaNotFoundException : DistillDownloadException
{
    public MediaNotFoundException(string message) : base(message) { }
    public MediaNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}
