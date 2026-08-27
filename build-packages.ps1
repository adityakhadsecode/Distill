<#
.SYNOPSIS
    Builds and packages Distill for distribution:
    1. Portable Standalone Executable (publish/Distill-Portable-x64/)
    2. Installable MSIX Package (publish/Distill_1.0.0.0_x64.msix)
    3. Self-Signed Development Certificate (publish/Distill_DevCert.cer)
#>

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$publishDir = Join-Path $root "publish"
$portableDir = Join-Path $publishDir "Distill-Portable-x64"
$msixStagingDir = Join-Path $publishDir "msix_staging"
$msixOutPath = Join-Path $publishDir "Distill_1.0.0.0_x64.msix"
$certPfxPath = Join-Path $publishDir "Distill_DevCert.pfx"
$certCerPath = Join-Path $publishDir "Distill_DevCert.cer"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "   Distill EXE & MSIX Package Builder    " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# 1. Ensure dotnet.exe path
$dotnet = if (Test-Path "$HOME\.dotnet\dotnet.exe") { "$HOME\.dotnet\dotnet.exe" } else { "dotnet" }

# 2. Locate makeappx and signtool from BuildTools NuGet package
$buildToolsPkg = Get-ChildItem "$HOME\.nuget\packages\microsoft.windows.sdk.buildtools" -ErrorAction SilentlyContinue | Sort-Object Name -Descending | Select-Object -First 1
if (-not $buildToolsPkg) {
    throw "Microsoft.Windows.SDK.BuildTools NuGet package not found in cache."
}

$makeAppx = Get-ChildItem $buildToolsPkg.FullName -Filter "makeappx.exe" -Recurse | Where-Object { $_.FullName -match "x64" } | Select-Object -First 1 -ExpandProperty FullName
$signTool = Get-ChildItem $buildToolsPkg.FullName -Filter "signtool.exe" -Recurse | Where-Object { $_.FullName -match "x64" } | Select-Object -First 1 -ExpandProperty FullName

Write-Host "`n[1/5] Publishing self-contained Release binaries..." -ForegroundColor Yellow
& $dotnet publish "$root\Distill.App\Distill.App.csproj" -c Release -r win-x64 --self-contained true -p:Platform=x64 -o $portableDir

if (-not (Test-Path (Join-Path $portableDir "Distill.App.exe"))) {
    throw "Publish failed: Distill.App.exe not found."
}
Write-Host "  -> Published to $portableDir" -ForegroundColor Green

# 3. Create MSIX Staging Directory & Copy Binaries
Write-Host "`n[2/5] Creating MSIX package staging files..." -ForegroundColor Yellow
if (Test-Path $msixStagingDir) {
    Remove-Item $msixStagingDir -Recurse -Force
}
New-Item -ItemType Directory -Path $msixStagingDir -Force | Out-Null
Copy-Item -Path "$portableDir\*" -Destination $msixStagingDir -Recurse -Force

# 4. Generate App Assets / Icons
$assetsDir = Join-Path $msixStagingDir "Assets"
New-Item -ItemType Directory -Path $assetsDir -Force | Out-Null

# Generate clean branding PNG tiles using .NET System.Drawing
Add-Type -AssemblyName System.Drawing

function Create-LogoPng($width, $height, $outPath) {
    $bmp = New-Object System.Drawing.Bitmap($width, $height)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

    # Dark obsidian backdrop
    $rect = New-Object System.Drawing.Rectangle(0, 0, $width, $height)
    $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $rect,
        [System.Drawing.Color]::FromArgb(255, 15, 17, 23),   # #0F1117
        [System.Drawing.Color]::FromArgb(255, 26, 27, 46),  # #1A1B2E
        45.0
    )
    $g.FillRectangle($bgBrush, $rect)

    # Geometry scaling calculations
    $isWide = $width -gt ($height * 1.5)
    $iconSize = if ($isWide) { [int]($height * 0.65) } else { [int]($height * 0.72) }
    $iconCenterX = if ($isWide) { [int]($width * 0.28) } else { [int]($width * 0.5) }
    $iconCenterY = [int]($height * 0.5)
    $scale = $iconSize / 32.0

    # Helper for relative coordinate translation
    function Pt($x, $y) {
        $realX = $iconCenterX + (($x - 16) * $scale)
        $realY = $iconCenterY + (($y - 16) * $scale)
        return [System.Drawing.PointF]::new($realX, $realY)
    }

    # 1. Top Ingestion Prism
    $topPts = @( (Pt 16 4), (Pt 27 9), (Pt 16 14), (Pt 5 9) )
    $topBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 167, 139, 250)) # #A78BFA
    $g.FillPolygon($topBrush, $topPts)

    # 2. Left Funnel Facet
    $leftPts = @( (Pt 5 9), (Pt 16 14), (Pt 16 22), (Pt 13 24), (Pt 13 18), (Pt 5 9) )
    $leftBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 124, 58, 237)) # #7C3AED
    $g.FillPolygon($leftBrush, $leftPts)

    # 3. Right Funnel Facet
    $rightPts = @( (Pt 27 9), (Pt 16 14), (Pt 16 22), (Pt 19 20), (Pt 19 18), (Pt 27 9) )
    $rightBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 99, 102, 241)) # #6366F1
    $g.FillPolygon($rightBrush, $rightPts)

    # 4. Distilled Crystal Droplet
    $dropX = $iconCenterX - (2.5 * $scale)
    $dropY = $iconCenterY + (9 * $scale)
    $dropW = 5 * $scale
    $dropH = 6 * $scale
    $dropBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 167, 139, 250)) # #A78BFA
    $g.FillEllipse($dropBrush, [System.Drawing.RectangleF]::new($dropX, $dropY, $dropW, $dropH))

    # If Wide, draw 'Distill' wordmark
    if ($isWide) {
        $fontHeight = [Math]::Max(10, [int]($height * 0.28))
        $font = New-Object System.Drawing.Font("Segoe UI Variable Display", $fontHeight, [System.Drawing.FontStyle]::Bold)
        $textBrush = [System.Drawing.Brushes]::White
        $sf = New-Object System.Drawing.StringFormat
        $sf.Alignment = [System.Drawing.StringAlignment]::Near
        $sf.LineAlignment = [System.Drawing.StringAlignment]::Center
        $textRect = [System.Drawing.RectangleF]::new([int]($width * 0.46), 0, [int]($width * 0.54), $height)
        $g.DrawString("Distill", $font, $textBrush, $textRect, $sf)
    }

    $bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bmp.Dispose()
}

Create-LogoPng 44 44 (Join-Path $assetsDir "Square44x44Logo.png")
Create-LogoPng 150 150 (Join-Path $assetsDir "Square150x150Logo.png")
Create-LogoPng 310 150 (Join-Path $assetsDir "Wide310x150Logo.png")
Create-LogoPng 50 50 (Join-Path $assetsDir "StoreLogo.png")
Create-LogoPng 620 300 (Join-Path $assetsDir "SplashScreen.png")
Write-Host "  -> Generated application assets in $assetsDir" -ForegroundColor Green

# 5. Write AppxManifest.xml
$manifestContent = @"
<?xml version="1.0" encoding="utf-8"?>
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
  IgnorableNamespaces="uap rescap">

  <Identity
    Name="Distill.App"
    Publisher="CN=DistillDeveloper, O=Distill"
    Version="1.0.0.0"
    ProcessorArchitecture="x64" />

  <Properties>
    <DisplayName>Distill</DisplayName>
    <PublisherDisplayName>Distill</PublisherDisplayName>
    <Logo>Assets\StoreLogo.png</Logo>
    <Description>Local-First Instagram to Obsidian Knowledge Extraction</Description>
  </Properties>

  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.19041.0" MaxVersionTested="10.0.26100.0" />
  </Dependencies>

  <Resources>
    <Resource Language="x-generate"/>
  </Resources>

  <Applications>
    <Application Id="App"
      Executable="Distill.App.exe"
      EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements
        DisplayName="Distill"
        Description="Distill — Instagram to Obsidian"
        BackgroundColor="#0F1117"
        Square150x150Logo="Assets\Square150x150Logo.png"
        Square44x44Logo="Assets\Square44x44Logo.png">
        <uap:DefaultTile Wide310x150Logo="Assets\Wide310x150Logo.png" Square310x310Logo="Assets\Square150x150Logo.png" />
        <uap:SplashScreen Image="Assets\SplashScreen.png" BackgroundColor="#0F1117"/>
      </uap:VisualElements>
    </Application>
  </Applications>

  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>
</Package>
"@

$manifestPath = Join-Path $msixStagingDir "AppxManifest.xml"
Set-Content -Path $manifestPath -Value $manifestContent -Encoding UTF8

# 6. Pack MSIX with MakeAppx.exe
Write-Host "`n[3/5] Packing MSIX package with makeappx.exe..." -ForegroundColor Yellow
if (Test-Path $msixOutPath) {
    Remove-Item $msixOutPath -Force
}
& $makeAppx pack /d $msixStagingDir /p $msixOutPath /nv /o
Write-Host "  -> Created package: $msixOutPath" -ForegroundColor Green

# 7. Create & Export Self-Signed Code Signing Certificate
Write-Host "`n[4/5] Creating self-signed signing certificate..." -ForegroundColor Yellow
$certPassword = ConvertTo-SecureString -String "Distill123!" -Force -AsPlainText
$existingCert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq "CN=DistillDeveloper, O=Distill" } | Select-Object -First 1

if (-not $existingCert) {
    $cert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject "CN=DistillDeveloper, O=Distill" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -KeyExportPolicy Exportable `
        -KeySpec Signature `
        -HashAlgorithm SHA256 `
        -KeyLength 2048 `
        -NotAfter (Get-Date).AddYears(5)
} else {
    $cert = $existingCert
}

Export-PfxCertificate -Cert $cert -FilePath $certPfxPath -Password $certPassword -Force | Out-Null
Export-Certificate -Cert $cert -FilePath $certCerPath -Force | Out-Null
Write-Host "  -> Exported Certificate: $certCerPath" -ForegroundColor Green

# 8. Sign MSIX with signtool.exe
Write-Host "`n[5/5] Signing MSIX package with signtool.exe..." -ForegroundColor Yellow
& $signTool sign /fd SHA256 /a /f $certPfxPath /p "Distill123!" $msixOutPath
Write-Host "  -> Signed MSIX package successfully!" -ForegroundColor Green

# Cleanup staging
Remove-Item $msixStagingDir -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host " [SUCCESS] Distill builds created successfully!" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
Write-Host " 1. Portable Standalone EXE : $portableDir\Distill.App.exe" -ForegroundColor Cyan
Write-Host " 2. Installable MSIX Package: $msixOutPath" -ForegroundColor Cyan
Write-Host " 3. Public Certificate (.cer): $certCerPath" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Green
