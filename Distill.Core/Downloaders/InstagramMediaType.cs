namespace Distill.Core.Downloaders;

/// <summary>
/// Classified media type detected from yt-dlp metadata JSON.
/// </summary>
public enum InstagramMediaType
{
    /// <summary>
    /// Single video Reel.
    /// </summary>
    Reel,

    /// <summary>
    /// Single photo post.
    /// </summary>
    SingleImagePost,

    /// <summary>
    /// Multi-slide carousel containing multiple images and/or video clips.
    /// </summary>
    CarouselPost
}
