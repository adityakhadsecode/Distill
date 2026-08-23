namespace Distill.Core.Process;

/// <summary>
/// Locates bundled or system PATH executable binaries for tools such as yt-dlp, ffmpeg, and whisper.cpp.
/// </summary>
public interface IToolLocator
{
    /// <summary>
    /// Resolves the absolute path or system command name for a given tool binary.
    /// </summary>
    /// <param name="toolFileName">The tool file name (e.g., "yt-dlp.exe", "ffmpeg.exe").</param>
    /// <returns>The resolved executable path.</returns>
    string ResolveToolPath(string toolFileName);
}
