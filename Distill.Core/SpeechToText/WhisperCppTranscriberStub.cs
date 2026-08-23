using Microsoft.Extensions.Logging;

namespace Distill.Core.SpeechToText;

/// <summary>
/// Stub implementation of <see cref="ITranscriber"/> representing whisper.cpp engine.
/// </summary>
public class WhisperCppTranscriberStub : ITranscriber
{
    private readonly ILogger<WhisperCppTranscriberStub>? _logger;

    public WhisperCppTranscriberStub(ILogger<WhisperCppTranscriberStub>? logger = null)
    {
        _logger = logger;
    }

    public Task<string> TranscribeAsync(string audioFilePath, CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Executing WhisperCppTranscriberStub on audio: {AudioFilePath}", audioFilePath);
        return Task.FromResult($"[whisper.cpp Transcribed Audio Stub for: {Path.GetFileName(audioFilePath)}]");
    }
}
