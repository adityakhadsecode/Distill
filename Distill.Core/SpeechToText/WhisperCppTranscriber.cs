using System.Text;
using System.Text.RegularExpressions;
using Distill.Core.Configuration;
using Distill.Core.Process;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Distill.Core.SpeechToText;

/// <summary>
/// Speech-to-Text transcriber that executes the standalone whisper.cpp binary (<c>whisper-cli.exe</c>).
/// </summary>
public partial class WhisperCppTranscriber : ITranscriber
{
    private readonly IProcessRunner _processRunner;
    private readonly IToolLocator _toolLocator;
    private readonly DistillSettings _settings;
    private readonly ILogger<WhisperCppTranscriber>? _logger;

    [GeneratedRegex(@"^\[\d{2}:\d{2}:\d{2}\.\d{3}\s*-->\s*\d{2}:\d{2}:\d{2}\.\d{3}\]\s*", RegexOptions.Compiled)]
    private static partial Regex TimestampRegex();

    public WhisperCppTranscriber(
        IProcessRunner processRunner,
        IToolLocator toolLocator,
        IOptions<DistillSettings> settings,
        ILogger<WhisperCppTranscriber>? logger = null)
    {
        _processRunner = processRunner;
        _toolLocator = toolLocator;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> TranscribeAsync(string audioFilePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(audioFilePath) || !File.Exists(audioFilePath))
        {
            _logger?.LogWarning("Audio file '{Path}' does not exist or path is empty; returning empty transcript.", audioFilePath);
            return string.Empty;
        }

        // Check if audio file is virtually empty or too small (< 500 bytes)
        var fileInfo = new FileInfo(audioFilePath);
        if (fileInfo.Length < 500)
        {
            _logger?.LogDebug("Audio file '{Path}' is too small ({Bytes} bytes) for speech transcription.", audioFilePath, fileInfo.Length);
            return string.Empty;
        }

        // 1. Locate whisper-cli.exe binary
        var whisperBin = ResolveWhisperBinary();
        if (string.IsNullOrWhiteSpace(whisperBin))
        {
            _logger?.LogWarning("whisper.cpp binary was not found. Skipping speech-to-text transcription.");
            return string.Empty;
        }

        // 2. Locate model file
        var modelPath = ResolveModelPath();
        if (string.IsNullOrWhiteSpace(modelPath))
        {
            _logger?.LogWarning("Whisper model file was not found at configured path: '{ConfiguredPath}'. Skipping transcription.", _settings.WhisperModelPath);
            return string.Empty;
        }

        var threads = _settings.WhisperThreadCount > 0 ? _settings.WhisperThreadCount : 4;
        var language = string.IsNullOrWhiteSpace(_settings.WhisperLanguage) ? "en" : _settings.WhisperLanguage;

        // Construct arguments for whisper-cli
        var arguments = $"-m \"{modelPath}\" -f \"{audioFilePath}\" -t {threads} -l {language} --no-timestamps --print-special false";
        var workingDir = Path.GetDirectoryName(audioFilePath);

        _logger?.LogInformation("Running whisper.cpp on '{AudioPath}' (Model: '{Model}', Threads: {Threads}, Lang: '{Lang}')",
            Path.GetFileName(audioFilePath), Path.GetFileName(modelPath), threads, language);

        var result = await _processRunner.RunAsync(whisperBin, arguments, workingDir, cancellationToken).ConfigureAwait(false);

        if (!result.Success)
        {
            var combinedError = $"{result.StandardError} {result.StandardOutput}".ToLowerInvariant();
            if (combinedError.Contains("too short") || combinedError.Contains("blank") || combinedError.Contains("no audio"))
            {
                _logger?.LogDebug("whisper.cpp reported silent or short audio: {Error}", result.StandardError.Trim());
                return string.Empty;
            }

            _logger?.LogWarning("whisper.cpp exited with code {Code}: {Error}", result.ExitCode, result.StandardError.Trim());
            return string.Empty;
        }

        var parsedTranscript = ParseWhisperOutput(result.StandardOutput);
        _logger?.LogInformation("Extracted transcript ({Length} characters) from '{AudioPath}'",
            parsedTranscript.Length, Path.GetFileName(audioFilePath));

        return parsedTranscript;
    }

    private string? ResolveWhisperBinary()
    {
        if (!string.IsNullOrWhiteSpace(_settings.WhisperBinaryPath))
        {
            var fullPath = Path.GetFullPath(_settings.WhisperBinaryPath);
            if (File.Exists(fullPath)) return fullPath;
        }

        var resolved = _toolLocator.ResolveToolPath("whisper-cli.exe");
        if (File.Exists(resolved)) return resolved;

        var fallbackResolved = _toolLocator.ResolveToolPath("whisper.exe");
        if (File.Exists(fallbackResolved)) return fallbackResolved;

        // If tool locator returns binary name for PATH execution, verify or allow
        return resolved;
    }

    private string? ResolveModelPath()
    {
        if (string.IsNullOrWhiteSpace(_settings.WhisperModelPath))
        {
            return null;
        }

        // Direct / absolute path check
        if (File.Exists(_settings.WhisperModelPath))
        {
            return Path.GetFullPath(_settings.WhisperModelPath);
        }

        // Check relative to base directory
        var inBaseDir = Path.Combine(AppContext.BaseDirectory, _settings.WhisperModelPath);
        if (File.Exists(inBaseDir))
        {
            return inBaseDir;
        }

        // Check in /tools/
        var inToolsDir = Path.Combine(AppContext.BaseDirectory, "tools", _settings.WhisperModelPath);
        if (File.Exists(inToolsDir))
        {
            return inToolsDir;
        }

        // Check in /tools/models/
        var inToolsModels = Path.Combine(AppContext.BaseDirectory, "tools", "models", Path.GetFileName(_settings.WhisperModelPath));
        if (File.Exists(inToolsModels))
        {
            return inToolsModels;
        }

        return _settings.WhisperModelPath;
    }

    private static string ParseWhisperOutput(string stdout)
    {
        if (string.IsNullOrWhiteSpace(stdout)) return string.Empty;

        var sb = new StringBuilder();
        var lines = stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // Filter out system diagnostic messages from whisper.cpp
            if (line.StartsWith("whisper_", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("system_info:", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("main:", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("cuda:", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("metal:", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("ggml_", StringComparison.OrdinalIgnoreCase) ||
                line.Equals("[BLANK_AUDIO]", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Strip any timestamp brackets if present: [00:00:00.000 --> 00:00:04.000]
            var cleanLine = TimestampRegex().Replace(line, string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(cleanLine))
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(cleanLine);
            }
        }

        return sb.ToString();
    }
}
