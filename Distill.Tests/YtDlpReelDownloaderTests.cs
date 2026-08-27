using System.Net;
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
    private readonly FakeHttpMessageHandler _fakeHttpHandler;
    private readonly HttpClient _fakeHttpClient;
    private readonly YtDlpReelDownloader _downloader;

    public YtDlpReelDownloaderTests()
    {
        _fakeRunner = new FakeProcessRunner();
        _fakeLocator = new FakeToolLocator();
        _fakeHttpHandler = new FakeHttpMessageHandler();
        _fakeHttpClient = new HttpClient(_fakeHttpHandler);
        _downloader = new YtDlpReelDownloader(_fakeRunner, _fakeLocator, _fakeHttpClient);
    }

    #region Fixture JSON Classification Unit Tests

    [Fact]
    public void DetectMediaType_WithReelJsonFixture_ClassifiesAsReel()
    {
        // Fixture: Reel with single video formats (vcodec != "none")
        var reelJson = """
        {
            "id": "reel_123",
            "title": "Architecture Masterclass Reel",
            "uploader": "tech_creator",
            "description": "5 tips for clean architecture",
            "formats": [
                { "format_id": "dash-video", "vcodec": "avc1.64001f", "width": 1080, "height": 1920 },
                { "format_id": "audio-only", "vcodec": "none", "acodec": "mp4a.40.2" }
            ],
            "thumbnails": [
                { "url": "https://instagram.com/reel_thumb.jpg", "width": 1080, "height": 1920 }
            ]
        }
        """;

        using var doc = JsonDocument.Parse(reelJson);
        var mediaType = YtDlpReelDownloader.DetectMediaType(doc.RootElement);

        Assert.Equal(InstagramMediaType.Reel, mediaType);
    }

    [Fact]
    public void DetectMediaType_WithSingleImagePostJsonFixture_ClassifiesAsSingleImagePost()
    {
        // Fixture: Single photo post with only thumbnails and empty/none video formats
        var singleImageJson = """
        {
            "id": "photo_456",
            "title": "System Design Infographic",
            "uploader": "architect_daily",
            "description": "Database indexing visual summary.",
            "formats": [],
            "thumbnails": [
                { "url": "https://instagram.com/thumb_small.jpg", "width": 320, "height": 320 },
                { "url": "https://instagram.com/thumb_large.jpg", "width": 1440, "height": 1440 }
            ]
        }
        """;

        using var doc = JsonDocument.Parse(singleImageJson);
        var mediaType = YtDlpReelDownloader.DetectMediaType(doc.RootElement);

        Assert.Equal(InstagramMediaType.SingleImagePost, mediaType);
    }

    [Fact]
    public void DetectMediaType_WithMultiImageCarouselJsonFixture_ClassifiesAsCarouselPost()
    {
        // Fixture: Multi-slide carousel containing 3 image entries
        var multiImageCarouselJson = """
        {
            "id": "carousel_789",
            "title": "Clean Code Rules Carousel",
            "uploader": "senior_dev",
            "description": "Swipe through for SOLID principles.",
            "entries": [
                {
                    "id": "slide_1",
                    "formats": [],
                    "thumbnails": [{ "url": "https://instagram.com/s1.jpg", "width": 1080, "height": 1080 }]
                },
                {
                    "id": "slide_2",
                    "formats": [{ "vcodec": "none", "acodec": "none" }],
                    "thumbnails": [{ "url": "https://instagram.com/s2.jpg", "width": 1080, "height": 1080 }]
                },
                {
                    "id": "slide_3",
                    "formats": [],
                    "thumbnails": [{ "url": "https://instagram.com/s3.jpg", "width": 1080, "height": 1080 }]
                }
            ]
        }
        """;

        using var doc = JsonDocument.Parse(multiImageCarouselJson);
        var mediaType = YtDlpReelDownloader.DetectMediaType(doc.RootElement);

        Assert.Equal(InstagramMediaType.CarouselPost, mediaType);
    }

    [Fact]
    public void DetectMediaType_WithMixedCarouselJsonFixture_ClassifiesAsCarouselPost()
    {
        // Fixture: Mixed carousel containing slide 1 (image) and slide 2 (video clip)
        var mixedCarouselJson = """
        {
            "id": "mixed_carousel_999",
            "title": "Tutorial & Demo",
            "uploader": "fullstack_pro",
            "description": "Slide 1 is steps, slide 2 is live video recording.",
            "entries": [
                {
                    "id": "slide_1_image",
                    "formats": [],
                    "thumbnails": [{ "url": "https://instagram.com/slide1.jpg", "width": 1080, "height": 1080 }]
                },
                {
                    "id": "slide_2_video",
                    "webpage_url": "https://instagram.com/p/mixed_carousel_999#slide2",
                    "formats": [
                        { "format_id": "mp4-hd", "vcodec": "h264", "acodec": "aac" }
                    ]
                }
            ]
        }
        """;

        using var doc = JsonDocument.Parse(mixedCarouselJson);
        var mediaType = YtDlpReelDownloader.DetectMediaType(doc.RootElement);

        Assert.Equal(InstagramMediaType.CarouselPost, mediaType);

        // Verify per-slide detection logic
        var entries = doc.RootElement.GetProperty("entries");
        Assert.False(YtDlpReelDownloader.IsVideoEntry(entries[0]));
        Assert.True(YtDlpReelDownloader.IsVideoEntry(entries[1]));
    }

    [Fact]
    public void DetectMediaType_WithSingleEntryArray_UnwrapsAndClassifiesCorrectly()
    {
        // Fixture: Array with exactly 1 entry containing video
        var singleEntryReelJson = """
        {
            "entries": [
                {
                    "id": "single_reel_inside_entry",
                    "formats": [{ "vcodec": "h264", "acodec": "aac" }]
                }
            ]
        }
        """;

        using var docReel = JsonDocument.Parse(singleEntryReelJson);
        Assert.Equal(InstagramMediaType.Reel, YtDlpReelDownloader.DetectMediaType(docReel.RootElement));

        // Fixture: Array with exactly 1 entry containing photo
        var singleEntryPhotoJson = """
        {
            "entries": [
                {
                    "id": "single_photo_inside_entry",
                    "formats": []
                }
            ]
        }
        """;

        using var docPhoto = JsonDocument.Parse(singleEntryPhotoJson);
        Assert.Equal(InstagramMediaType.SingleImagePost, YtDlpReelDownloader.DetectMediaType(docPhoto.RootElement));
    }

    #endregion

    #region Download Pipeline Execution Tests

    [Fact]
    public async Task DownloadAsync_WithAllImageCarousel_DownloadsThumbnailsViaHttp()
    {
        // Arrange
        const string postUrl = "https://www.instagram.com/p/carousel_images_123/";

        var metadataJson = JsonSerializer.Serialize(new
        {
            title = "Top 3 Visual Design Rules",
            uploader = "designer_guru",
            description = "Here are 3 tips for contrast and hierarchy.",
            entries = new[]
            {
                new
                {
                    id = "slide_1",
                    formats = Array.Empty<object>(),
                    thumbnails = new[]
                    {
                        new { url = "https://instagram.com/img1_small.jpg", width = 300, height = 300 },
                        new { url = "https://instagram.com/img1_large.jpg", width = 1080, height = 1080 }
                    }
                },
                new
                {
                    id = "slide_2",
                    formats = Array.Empty<object>(),
                    thumbnails = new[]
                    {
                        new { url = "https://instagram.com/img2_large.jpg", width = 1080, height = 1080 }
                    }
                },
                new
                {
                    id = "slide_3",
                    formats = Array.Empty<object>(),
                    thumbnails = new[]
                    {
                        new { url = "https://instagram.com/img3_large.jpg", width = 1080, height = 1080 }
                    }
                }
            }
        });

        _fakeRunner.CustomHandler = (exe, args, _) =>
        {
            if (args.Contains("--dump-single-json"))
            {
                return new ProcessResult(0, metadataJson, string.Empty);
            }
            return new ProcessResult(0, "OK", string.Empty);
        };

        _fakeHttpHandler.Handler = request =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x01, 0x02 }) // valid mock JPEG bytes
            };
        };

        // Act
        var result = await _downloader.DownloadAsync(postUrl);

        // Assert
        Assert.NotNull(result);
        var postResult = Assert.IsType<PostDownloadResult>(result);
        Assert.Equal(postUrl, postResult.SourceUrl);
        Assert.Equal("Top 3 Visual Design Rules", postResult.Title);
        Assert.Equal("@designer_guru", postResult.Author);
        Assert.Equal(3, postResult.ImageFilePaths.Count);

        // Verify 3 HTTP GET requests made for highest-resolution images
        Assert.Equal(3, _fakeHttpHandler.Requests.Count);
        Assert.Equal("https://instagram.com/img1_large.jpg", _fakeHttpHandler.Requests[0].RequestUri?.ToString());
        Assert.Equal("https://instagram.com/img2_large.jpg", _fakeHttpHandler.Requests[1].RequestUri?.ToString());
        Assert.Equal("https://instagram.com/img3_large.jpg", _fakeHttpHandler.Requests[2].RequestUri?.ToString());

        // Verify all 3 image files exist in working directory
        foreach (var imgPath in postResult.ImageFilePaths)
        {
            Assert.True(File.Exists(imgPath));
        }

        // Cleanup
        result.Cleanup();
        Assert.False(Directory.Exists(result.WorkingDirectory));
    }

    [Fact]
    public async Task DownloadAsync_WithMixedCarousel_DownloadsImageViaHttpAndVideoViaYtDlp()
    {
        // Arrange
        const string postUrl = "https://www.instagram.com/p/mixed_carousel_456/";

        var metadataJson = JsonSerializer.Serialize(new
        {
            title = "Mixed Media Post",
            uploader = "tech_lead",
            description = "Slide 1 is an infographic, Slide 2 is a demo video.",
            entries = new object[]
            {
                new
                {
                    id = "slide_1",
                    formats = Array.Empty<object>(),
                    thumbnails = new[]
                    {
                        new { url = "https://instagram.com/slide1_hd.jpg", width = 1080, height = 1080 }
                    }
                },
                new
                {
                    id = "slide_2",
                    webpage_url = "https://instagram.com/p/mixed_carousel_456#slide2",
                    formats = new[]
                    {
                        new { format_id = "mp4-hd", vcodec = "h264", acodec = "aac" }
                    }
                }
            }
        });

        _fakeRunner.CustomHandler = (exe, args, dir) =>
        {
            if (args.Contains("--dump-single-json"))
            {
                return new ProcessResult(0, metadataJson, string.Empty);
            }

            if (exe.Contains("yt-dlp") && dir != null)
            {
                // Simulate yt-dlp downloading slide_002.mp4
                File.WriteAllBytes(Path.Combine(dir, "slide_002.mp4"), new byte[] { 0x00, 0x01 });
            }
            else if (exe.Contains("ffmpeg") && args.Contains("-vframes 1") && dir != null)
            {
                // Simulate ffmpeg frame extraction for slide_002.jpg
                File.WriteAllBytes(Path.Combine(dir, "slide_002.jpg"), new byte[] { 0xFF, 0xD8 });
            }

            return new ProcessResult(0, "OK", string.Empty);
        };

        _fakeHttpHandler.Handler = request =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0x01 })
            };
        };

        // Act
        var result = await _downloader.DownloadAsync(postUrl);

        // Assert
        Assert.NotNull(result);
        var postResult = Assert.IsType<PostDownloadResult>(result);
        Assert.Equal(2, postResult.ImageFilePaths.Count);

        // 1 HTTP request made for slide 1
        Assert.Single(_fakeHttpHandler.Requests);
        Assert.Equal("https://instagram.com/slide1_hd.jpg", _fakeHttpHandler.Requests[0].RequestUri?.ToString());

        // 1 yt-dlp video download + 1 ffmpeg frame extraction for slide 2
        Assert.Contains(_fakeRunner.Executions, e => e.Arguments.Contains("slide_002.mp4"));
        Assert.Contains(_fakeRunner.Executions, e => e.Arguments.Contains("-vframes 1"));

        result.Cleanup();
    }

    [Fact]
    public async Task DownloadAsync_WithSingleImagePost_DownloadsDirectImage()
    {
        // Arrange
        const string postUrl = "https://www.instagram.com/p/single_image_789/";

        var metadataJson = JsonSerializer.Serialize(new
        {
            id = "single_image_post",
            title = "Single Infographic Image",
            uploader = "data_viz",
            description = "Standalone infographic poster.",
            formats = Array.Empty<object>(),
            thumbnails = new[]
            {
                new { url = "https://instagram.com/single_poster_full.jpg", width = 1440, height = 1440 }
            }
        });

        _fakeRunner.CustomHandler = (exe, args, _) =>
        {
            if (args.Contains("--dump-single-json"))
            {
                return new ProcessResult(0, metadataJson, string.Empty);
            }
            return new ProcessResult(0, "OK", string.Empty);
        };

        _fakeHttpHandler.Handler = request =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF })
            };
        };

        // Act
        var result = await _downloader.DownloadAsync(postUrl);

        // Assert
        Assert.NotNull(result);
        var postResult = Assert.IsType<PostDownloadResult>(result);
        Assert.Equal("Single Infographic Image", postResult.Title);
        Assert.Equal("@data_viz", postResult.Author);
        Assert.Single(postResult.ImageFilePaths);
        Assert.Single(_fakeHttpHandler.Requests);
        Assert.Equal("https://instagram.com/single_poster_full.jpg", _fakeHttpHandler.Requests[0].RequestUri?.ToString());

        result.Cleanup();
    }

    [Fact]
    public async Task DownloadAsync_WithReelUrl_DownloadsVideoAndExtractsAudioAndFrames()
    {
        // Arrange
        const string reelUrl = "https://www.instagram.com/reel/C123456789/";

        var reelMetadataJson = JsonSerializer.Serialize(new
        {
            id = "C123456789",
            title = "Clean Architecture Reel",
            uploader = "code_coach",
            description = "How to decouple your business logic.",
            formats = new[]
            {
                new { format_id = "hd-video", vcodec = "h264", acodec = "aac" }
            }
        });

        _fakeRunner.CustomHandler = (exe, args, dir) =>
        {
            if (args.Contains("--dump-single-json"))
            {
                return new ProcessResult(0, reelMetadataJson, string.Empty);
            }

            if (dir == null) return new ProcessResult(0, "OK", string.Empty);

            if (exe.Contains("yt-dlp"))
            {
                File.WriteAllText(Path.Combine(dir, "video.mp4"), "fake video data");
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

        var reelMetadataJson = JsonSerializer.Serialize(new
        {
            id = "Cfallback123",
            title = "Static Reel",
            uploader = "creator",
            formats = new[]
            {
                new { format_id = "video-only", vcodec = "avc1", acodec = "none" }
            }
        });

        _fakeRunner.CustomHandler = (exe, args, dir) =>
        {
            if (args.Contains("--dump-single-json"))
            {
                return new ProcessResult(0, reelMetadataJson, string.Empty);
            }

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

    [Fact]
    public async Task DownloadAsync_PassesIgnoreNoFormatsError_ToYtDlp()
    {
        // Arrange
        const string url = "https://www.instagram.com/p/image_post_123/";
        var mockJson = JsonSerializer.Serialize(new
        {
            title = "Test Post",
            display_url = "https://instagram.com/direct_image.jpg",
            formats = Array.Empty<object>()
        });

        _fakeRunner.CustomHandler = (exe, args, _) =>
        {
            if (args.Contains("--dump-single-json"))
            {
                return new ProcessResult(0, mockJson, string.Empty);
            }
            return new ProcessResult(0, "OK", string.Empty);
        };

        _fakeHttpHandler.Handler = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 })
        };

        // Act
        var result = await _downloader.DownloadAsync(url);

        // Assert
        Assert.Contains(_fakeRunner.Executions, h => h.Arguments.Contains("--ignore-no-formats-error"));
        Assert.IsType<PostDownloadResult>(result);
    }

    [Theory]
    [InlineData("{\"thumbnail\": \"https://instagram.com/thumb_prop.jpg\"}", "https://instagram.com/thumb_prop.jpg")]
    [InlineData("{\"display_url\": \"https://instagram.com/display_prop.jpg\"}", "https://instagram.com/display_prop.jpg")]
    [InlineData("{\"url\": \"https://instagram.com/direct_url.jpg\"}", "https://instagram.com/direct_url.jpg")]
    public void GetBestImageUrl_WithVariousPropertyNames_ResolvesCorrectly(string json, string expectedUrl)
    {
        using var doc = JsonDocument.Parse(json);
        var resolvedUrl = YtDlpReelDownloader.GetBestImageUrl(doc.RootElement);
        Assert.Equal(expectedUrl, resolvedUrl);
    }

    [Fact]
    public async Task DownloadAsync_WithCarousel_WhenSingleSlideFails_BestEffortSucceedsWithRemainingSlides()
    {
        // Arrange
        const string postUrl = "https://www.instagram.com/p/carousel_partial_fail/";

        var metadataJson = JsonSerializer.Serialize(new
        {
            title = "Partial Carousel",
            uploader = "test_creator",
            entries = new[]
            {
                new
                {
                    id = "slide_1_ok",
                    thumbnail = "https://instagram.com/slide1_ok.jpg",
                    formats = Array.Empty<object>()
                },
                new
                {
                    id = "slide_2_broken",
                    thumbnail = "https://instagram.com/slide2_broken.jpg",
                    formats = Array.Empty<object>()
                },
                new
                {
                    id = "slide_3_ok",
                    thumbnail = "https://instagram.com/slide3_ok.jpg",
                    formats = Array.Empty<object>()
                }
            }
        });

        _fakeRunner.CustomHandler = (exe, args, _) =>
        {
            if (args.Contains("--dump-single-json"))
            {
                return new ProcessResult(0, metadataJson, string.Empty);
            }
            return new ProcessResult(0, "OK", string.Empty);
        };

        _fakeHttpHandler.Handler = request =>
        {
            if (request.RequestUri?.AbsoluteUri.Contains("slide2_broken") == true)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 })
            };
        };

        // Act
        var result = await _downloader.DownloadAsync(postUrl);

        // Assert
        var postResult = Assert.IsType<PostDownloadResult>(result);
        Assert.Equal(2, postResult.ImageFilePaths.Count);
    }

    #endregion
}
