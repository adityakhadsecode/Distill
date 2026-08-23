namespace Distill.Core.SpeechToText;

/// <summary>
/// Transcribes spoken audio into text.
/// </summary>
public interface ITranscriber
{
    /// <summary>
    /// Transcribes an audio file into text.
    /// </summary>
    /// <param name="audioFilePath">Absolute path to the audio file (e.g. 16kHz WAV).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transcribed speech text.</returns>
    Task<string> TranscribeAsync(string audioFilePath, CancellationToken cancellationToken = default);
}
