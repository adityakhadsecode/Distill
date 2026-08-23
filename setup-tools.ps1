<#
.SYNOPSIS
    Downloads and configures the required local binary tools for Distill:
    - yt-dlp.exe (Instagram post & reel downloader)
    - ffmpeg.exe (Audio demuxing and keyframe extractor)
    - whisper-cli.exe (Local speech-to-text engine)
    - ggml-base.en.bin (Whisper speech model)
#>

$ErrorActionPreference = "Stop"
$toolsDir = Join-Path $PSScriptRoot "tools"
$modelsDir = Join-Path $toolsDir "models"

if (-not (Test-Path $toolsDir)) {
    New-Item -ItemType Directory -Path $toolsDir -Force | Out-Null
}
if (-not (Test-Path $modelsDir)) {
    New-Item -ItemType Directory -Path $modelsDir -Force | Out-Null
}

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host " Distill Tools & Whisper Setup Assistant " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# 1. Download yt-dlp.exe
$ytdlpPath = Join-Path $toolsDir "yt-dlp.exe"
if (-not (Test-Path $ytdlpPath)) {
    Write-Host "`n[1/4] Downloading yt-dlp.exe..." -ForegroundColor Yellow
    Invoke-WebRequest -Uri "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe" -OutFile $ytdlpPath
    Write-Host "  -> Downloaded yt-dlp.exe successfully." -ForegroundColor Green
} else {
    Write-Host "[1/4] yt-dlp.exe already present." -ForegroundColor Green
}

# 2. Download ffmpeg.exe
$ffmpegPath = Join-Path $toolsDir "ffmpeg.exe"
if (-not (Test-Path $ffmpegPath)) {
    Write-Host "`n[2/4] Downloading ffmpeg..." -ForegroundColor Yellow
    $zipPath = Join-Path $env:TEMP "ffmpeg-release-essentials.zip"
    Invoke-WebRequest -Uri "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip" -OutFile $zipPath
    
    Write-Host "  -> Extracting ffmpeg.exe..." -ForegroundColor Yellow
    $extractPath = Join-Path $env:TEMP "ffmpeg_extract"
    Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force
    $foundFfmpeg = Get-ChildItem -Path $extractPath -Filter "ffmpeg.exe" -Recurse | Select-Object -First 1
    if ($foundFfmpeg) {
        Copy-Item -Path $foundFfmpeg.FullName -Destination $ffmpegPath -Force
        Write-Host "  -> Copied ffmpeg.exe to tools/" -ForegroundColor Green
    }
    Remove-Item $zipPath -Force -ErrorAction SilentlyContinue
    Remove-Item $extractPath -Recurse -Force -ErrorAction SilentlyContinue
} else {
    Write-Host "[2/4] ffmpeg.exe already present." -ForegroundColor Green
}

# 3. Download whisper.cpp binary (whisper-cli.exe)
$whisperPath = Join-Path $toolsDir "whisper-cli.exe"
if (-not (Test-Path $whisperPath)) {
    Write-Host "`n[3/4] Downloading whisper.cpp binary..." -ForegroundColor Yellow
    $whisperZip = Join-Path $env:TEMP "whisper-bin-x64.zip"
    $whisperUrl = "https://github.com/ggerganov/whisper.cpp/releases/latest/download/whisper-bin-x64.zip"
    
    try {
        Invoke-WebRequest -Uri $whisperUrl -OutFile $whisperZip
        $extractPath = Join-Path $env:TEMP "whisper_extract"
        Expand-Archive -Path $whisperZip -DestinationPath $extractPath -Force
        
        $foundWhisper = Get-ChildItem -Path $extractPath -Filter "whisper-cli.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
        if (-not $foundWhisper) {
            $foundWhisper = Get-ChildItem -Path $extractPath -Filter "main.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
        }
        
        if ($foundWhisper) {
            Copy-Item -Path $foundWhisper.FullName -Destination $whisperPath -Force
            Write-Host "  -> Copied whisper-cli.exe to tools/" -ForegroundColor Green
        }
        
        # Copy any required companion DLLs (e.g., ggml.dll, openvino, etc.)
        Get-ChildItem -Path $extractPath -Filter "*.dll" -Recurse | ForEach-Object {
            Copy-Item -Path $_.FullName -Destination $toolsDir -Force
        }
        
        Remove-Item $whisperZip -Force -ErrorAction SilentlyContinue
        Remove-Item $extractPath -Recurse -Force -ErrorAction SilentlyContinue
    } catch {
        Write-Host "  -> Could not automatically download whisper binary. Please place whisper-cli.exe in the 'tools/' folder." -ForegroundColor DarkYellow
    }
} else {
    Write-Host "[3/4] whisper-cli.exe already present." -ForegroundColor Green
}

# 4. Download Whisper model (ggml-base.en.bin)
$modelPath = Join-Path $modelsDir "ggml-base.en.bin"
if (-not (Test-Path $modelPath)) {
    Write-Host "`n[4/4] Downloading Whisper GGML Base English Model (~148 MB)..." -ForegroundColor Yellow
    $modelUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.en.bin"
    Invoke-WebRequest -Uri $modelUrl -OutFile $modelPath
    Write-Host "  -> Downloaded ggml-base.en.bin to tools/models/" -ForegroundColor Green
} else {
    Write-Host "[4/4] ggml-base.en.bin already present." -ForegroundColor Green
}

# Copy tools to output build directory if it exists
$buildOutTools = Join-Path $PSScriptRoot "Distill.App\bin\x64\Debug\net8.0-windows10.0.19041.0\tools"
if (Test-Path (Split-Path $buildOutTools -Parent)) {
    Copy-Item -Path $toolsDir -Destination (Split-Path $buildOutTools -Parent) -Recurse -Force
    Write-Host "`n[✓] Synced tools to application build output folder." -ForegroundColor Green
}

Write-Host "`n=========================================" -ForegroundColor Green
Write-Host " Setup Complete! All tools are configured." -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
