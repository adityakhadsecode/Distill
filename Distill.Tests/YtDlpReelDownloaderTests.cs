using System.Text.Json;
using Distill.Core.Downloaders;
using Distill.Core.Exceptions;
using Distill.Core.Models;
using Distill.Core.Process;
using Distill.Tests.Mocks;
using Xunit;

namespace Distill.Tests;

public class YtDlpReelDownloaderTests
{
    private readonly FakeProcessRunner _fakeRunner;
    private readonly FakeToolLocator _fakeLocator;
    private readonly YtDlpReelDownloader _downloader;

    public YtDlpReelDownloaderTests()
    {
        _fakeRunner = new FakeProcessRunner();
        _fakeLocator = new FakeToolLocator();
        _downloader = new YtDlpReelDownloader(_fakeRunner, _fakeLocator);
    }

    [Fact]
    public async Task DownloadAsync_WithPostUrl_DownloadsCarouselImagesAndReturnsPostResult()
    {
        // Arrange
        const string postUrl = "https://www.instagram.com/p/C987654321/";
        _fakeRunner.CustomHandler = (exe, args, dir) =>
        {
            if (dir != null)
            {
                // Simulate yt-dlp downloading 3 carousel slide images and writing info json
                File.WriteAllText(Path.Combine(dir, "slide_001.jpg"), "slide 1 content");
                File.WriteAllText(Path.Combine(dir, "slide_002.jpg"), "slide 2 content");
                File.WriteAllText(Path.Combine(dir, "slide_003.png"), "slide 3 content");

                var infoJson = JsonSerializer.Serialize(new
                {
                    title = "Awesome Architecture Post",
                    uploader = "design_daily",
                    description = "Key principles of modern UI design."
                });
                File.WriteAllText(Path.Combine(dir, "metadata.info.json"), infoJson);
            }

            return new ProcessResult(0, "yt-dlp success", string.Empty);
        };

        // Act
        var result = await _downloader.DownloadAsync(postUrl);

        // Assert
        Assert.NotNull(result);
        var postResult = Assert.IsType<PostDownloadResult>(result);
        Assert.Equal(postUrl, postResult.SourceUrl);
        Assert.Equal("Awesome Architecture Post", postResult.Title);
        Assert.Equal("@design_daily", postResult.Author);
        Assert.Equal(3, postResult.ImageFilePaths.Count);
        Assert.Contains(postResult.ImageFilePaths, p => p.EndsWith("slide_001.jpg"));
        Assert.Contains(postResult.ImageFilePaths, p => p.EndsWith("slide_002.jpg"));
        Assert.Contains(postResult.ImageFilePaths, p => p.EndsWith("slide_003.png"));

        // Cleanup
        result.Cleanup();
        Assert.False(Directory.Exists(result.WorkingDirectory));
    }

    [Fact]
    public async Task DownloadAsync_WithReelUrl_DownloadsVideoAndExtractsAudioAndFrames()
    {
        // Arrange
        const string reelUrl = "https://www.instagram.com/reel/C123456789/";
        _fakeRunner.CustomHandler = (exe, args, dir) =>
        {
            if (dir == null) return new ProcessResult(0, "OK", string.Empty);

            if (exe.Contains("yt-dlp"))
            {
                File.WriteAllText(Path.Combine(dir, "video.mp4"), "fake video data");
                var infoJson = JsonSerializer.Serialize(new
                {
                    title = "Clean Architecture Reel",
                    uploader = "code_coach",
                    description = "How to decouple your business logic."
                });
                File.WriteAllText(Path.Combine(dir, "video.info.json"), infoJson);
            }
            else if (exe.Contains("ffmpeg") && args.Contains("pcm_s16le"))
            {
                File.WriteAllText(Path.Combine(dir, "audio.wav"), "fake audio wav data");
            }
            else if (exe.Contains("ffmpeg") && args.Contains("frames"))
            {
                var framesDir = Path.Combine(dir, "frames");
                Directory.CreateDirectory(framesDir);
                File.WriteAllText(Path.Combine(framesDir, "frame_001.jpg"), "frame 1");
                File.WriteAllText(Path.Combine(framesDir, "frame_002.jpg"), "frame 2");
            }

            return new ProcessResult(0, "ffmpeg/yt-dlp success", string.Empty);
        };

        // Act
        var result = await _downloader.DownloadAsync(reelUrl);

        // Assert
        Assert.NotNull(result);
        var reelResult = Assert.IsType<ReelDownloadResult>(result);
        Assert.Equal(reelUrl, reelResult.SourceUrl);
        Assert.Equal("Clean Architecture Reel", reelResult.Title);
        Assert.Equal("@code_coach", reelResult.Author);
        Assert.True(File.Exists(reelResult.VideoFilePath));
        Assert.True(File.Exists(reelResult.AudioFilePath));
        Assert.Equal(2, reelResult.FrameFilePaths.Count);

        // Verify ffmpeg audio args used 16kHz mono WAV
        Assert.Contains(_fakeRunner.Executions, e => e.Arguments.Contains("-ar 16000") && e.Arguments.Contains("-ac 1"));

        // Cleanup
        result.Cleanup();
        Assert.False(Directory.Exists(result.WorkingDirectory));
    }

    [Fact]
    public async Task DownloadAsync_WithReelUrl_WhenSceneDetectionYieldsNoFrames_FallsBackToIntervalSampling()
    {
        // Arrange
        const string reelUrl = "https://www.instagram.com/reel/Cfallback123/";
        _fakeRunner.CustomHandler = (exe, args, dir) =>
        {
            if (dir == null) return new ProcessResult(0, "OK", string.Empty);

            if (exe.Contains("yt-dlp"))
            {
                File.WriteAllText(Path.Combine(dir, "video.mp4"), "fake video data");
            }
            else if (exe.Contains("ffmpeg") && args.Contains("pcm_s16le"))
            {
                File.WriteAllText(Path.Combine(dir, "audio.wav"), "audio");
            }
            else if (exe.Contains("ffmpeg") && args.Contains("fps=1/2"))
            {
                // Fallback interval sampling triggered!
                var framesDir = Path.Combine(dir, "frames");
                Directory.CreateDirectory(framesDir);
                File.WriteAllText(Path.Combine(framesDir, "frame_001.jpg"), "sampled frame 1");
                File.WriteAllText(Path.Combine(framesDir, "frame_002.jpg"), "sampled frame 2");
            }

            return new ProcessResult(0, "success", string.Empty);
        };

        // Act
        var result = await _downloader.DownloadAsync(reelUrl);

        // Assert
        var reelResult = Assert.IsType<ReelDownloadResult>(result);
        Assert.Equal(2, reelResult.FrameFilePaths.Count);

        // Assert both scene detection and fallback interval ffmpeg were called
        Assert.Contains(_fakeRunner.Executions, e => e.Arguments.Contains("gt(scene,0.3)"));
        Assert.Contains(_fakeRunner.Executions, e => e.Arguments.Contains("fps=1/2"));

        result.Cleanup();
    }

    [Fact]
    public async Task DownloadAsync_WhenMediaIsPrivate_ThrowsPrivateMediaException()
    {
        // Arrange
        _fakeRunner.CustomHandler = (_, _, _) =>
            new ProcessResult(1, string.Empty, "ERROR: [Instagram] Private account, login required to view content");

        // Act & Assert
        await Assert.ThrowsAsync<PrivateMediaException>(() =>
            _downloader.DownloadAsync("https://www.instagram.com/p/private123/"));
    }

    [Fact]
    public async Task DownloadAsync_WhenRateLimited_ThrowsRateLimitException()
    {
        // Arrange
        _fakeRunner.CustomHandler = (_, _, _) =>
            new ProcessResult(1, string.Empty, "ERROR: HTTP Error 429: Too Many Requests / temporarily blocked");

        // Act & Assert
        await Assert.ThrowsAsync<RateLimitException>(() =>
            _downloader.DownloadAsync("https://www.instagram.com/reel/ratelimited123/"));
    }

    [Fact]
    public async Task DownloadAsync_WhenMediaNotFound_ThrowsMediaNotFoundException()
    {
        // Arrange
        _fakeRunner.CustomHandler = (_, _, _) =>
            new ProcessResult(1, string.Empty, "ERROR: [Instagram] 404 Not Found: The link you followed may be broken or removed.");

        // Act & Assert
        await Assert.ThrowsAsync<MediaNotFoundException>(() =>
            _downloader.DownloadAsync("https://www.instagram.com/p/deleted123/"));
    }

    [Fact]
    public void DownloadResult_Cleanup_DeletesDirectorySafely()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "Distill_TestCleanup_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "temp.txt"), "hello");

        var result = new PostDownloadResult
        {
            SourceUrl = "https://instagram.com/p/1",
            WorkingDirectory = tempDir,
            ImageFilePaths = [Path.Combine(tempDir, "temp.txt")]
        };

        // Act
        result.Cleanup();

        // Assert
        Assert.False(Directory.Exists(tempDir));
    }
}
