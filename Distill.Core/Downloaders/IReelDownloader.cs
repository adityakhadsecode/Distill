using Distill.Core.Models;

namespace Distill.Core.Downloaders;

/// <summary>
/// Downloads an Instagram reel or post given a URL and returns local file paths.
/// </summary>
public interface IReelDownloader
{
    /// <summary>
    /// Downloads media content for the specified Instagram URL.
    /// </summary>
    /// <param name="url">The Instagram post or reel URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="DownloadResult"/> containing downloaded media file paths and metadata.</returns>
    Task<DownloadResult> DownloadAsync(string url, CancellationToken cancellationToken = default);
}
