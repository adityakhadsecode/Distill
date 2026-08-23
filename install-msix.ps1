<#
.SYNOPSIS
    Installs the Distill MSIX package onto your Windows system.
#>

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$certPath = Join-Path $root "publish\Distill_DevCert.cer"
$msixPath = Join-Path $root "publish\Distill_1.0.0.0_x64.msix"

if (-not (Test-Path $msixPath)) {
    Write-Error "MSIX package not found at $msixPath. Run build-packages.ps1 first."
    exit 1
}

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "     Installing Distill (MSIX Package)   " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# 1. Trust certificate for CurrentUser
if (Test-Path $certPath) {
    Write-Host "`n[1/2] Adding development certificate to Trusted People..." -ForegroundColor Yellow
    Import-Certificate -FilePath $certPath -CertStoreLocation "Cert:\CurrentUser\TrustedPeople" | Out-Null
    Write-Host "  -> Certificate trusted." -ForegroundColor Green
}

# 2. Install App Package
Write-Host "`n[2/2] Installing Distill MSIX package..." -ForegroundColor Yellow
Add-AppxPackage -Path $msixPath

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host " [SUCCESS] Distill is installed on your Windows system!" -ForegroundColor Green
Write-Host " You can now search 'Distill' in your Start Menu." -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Green
