using Distill.Core.Configuration;
using Distill.Core.Process;
using Distill.Core.SpeechToText;
using Distill.Tests.Mocks;
using Microsoft.Extensions.Options;
using Xunit;

namespace Distill.Tests;

public class WhisperCppTranscriberTests : IDisposable
{
    private readonly FakeProcessRunner _fakeRunner;
    private readonly FakeToolLocator _fakeLocator;
    private readonly string _testDir;

    public WhisperCppTranscriberTests()
    {
        _fakeRunner = new FakeProcessRunner();
        _fakeLocator = new FakeToolLocator();
        _testDir = Path.Combine(Path.GetTempPath(), "Distill_WhisperTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    [Fact]
    public async Task TranscribeAsync_WhenAudioFileDoesNotExist_ReturnsEmptyStringWithoutThrowing()
    {
        // Arrange
        var settings = Options.Create(new DistillSettings());
        var transcriber = new WhisperCppTranscriber(_fakeRunner, _fakeLocator, settings);
        var nonExistentPath = Path.Combine(_testDir, "missing.wav");

        // Act
        var result = await transcriber.TranscribeAsync(nonExistentPath);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task TranscribeAsync_WhenAudioFileIsTooSmall_ReturnsEmptyStringWithoutThrowing()
    {
        // Arrange
        var settings = Options.Create(new DistillSettings());
        var transcriber = new WhisperCppTranscriber(_fakeRunner, _fakeLocator, settings);
        var tinyAudioPath = Path.Combine(_testDir, "tiny.wav");
        File.WriteAllBytes(tinyAudioPath, new byte[50]); // 50 bytes is too small

        // Act
        var result = await transcriber.TranscribeAsync(tinyAudioPath);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task TranscribeAsync_WhenSuccessful_ParsesAndDenoisesTranscript()
    {
        // Arrange
        var modelPath = Path.Combine(_testDir, "ggml-base.en.bin");
        File.WriteAllText(modelPath, "fake model");

        var audioPath = Path.Combine(_testDir, "speech.wav");
        File.WriteAllBytes(audioPath, new byte[2048]); // valid size

        var settings = Options.Create(new DistillSettings
        {
            WhisperModelPath = modelPath,
            WhisperThreadCount = 6,
            WhisperLanguage = "en"
        });

        _fakeRunner.CustomHandler = (exe, args, dir) =>
        {
            const string mockOutput = @"
whisper_init_from_file_with_params: loading model...
system_info: n_threads = 6 / 8 | AVX = 1 | AVX2 = 1 |
main: processing 'speech.wav' (16000 samples, 1.0 sec), 6 threads...
[00:00:00.000 --> 00:00:03.500]   Welcome to this quick tutorial on clean architecture.
[00:00:03.500 --> 00:00:07.000]   Make sure to decouple your business logic.
whisper_print_timings:     load time =    52.12 ms
";
            return new ProcessResult(0, mockOutput, string.Empty);
        };

        var transcriber = new WhisperCppTranscriber(_fakeRunner, _fakeLocator, settings);

        // Act
        var result = await transcriber.TranscribeAsync(audioPath);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.Contains("Welcome to this quick tutorial on clean architecture.", result);
        Assert.Contains("Make sure to decouple your business logic.", result);
        Assert.DoesNotContain("whisper_", result);
        Assert.DoesNotContain("system_info:", result);
        Assert.DoesNotContain("-->", result); // Timestamps stripped

        // Verify thread and model arguments passed to whisper CLI
        Assert.Contains(_fakeRunner.Executions, e => e.Arguments.Contains("-t 6") && e.Arguments.Contains("ggml-base.en.bin"));
    }

    [Fact]
    public async Task TranscribeAsync_WhenWhisperReportsSilentAudio_ReturnsEmptyStringWithoutThrowing()
    {
        // Arrange
        var modelPath = Path.Combine(_testDir, "ggml-base.en.bin");
        File.WriteAllText(modelPath, "fake model");

        var audioPath = Path.Combine(_testDir, "silent.wav");
        File.WriteAllBytes(audioPath, new byte[2048]);

        var settings = Options.Create(new DistillSettings
        {
            WhisperModelPath = modelPath
        });

        _fakeRunner.CustomHandler = (_, _, _) =>
            new ProcessResult(0, "[BLANK_AUDIO]\nwhisper_print_timings: load time = 10ms", string.Empty);

        var transcriber = new WhisperCppTranscriber(_fakeRunner, _fakeLocator, settings);

        // Act
        var result = await transcriber.TranscribeAsync(audioPath);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch
        {
            // Ignore test cleanup errors
        }
    }
}
