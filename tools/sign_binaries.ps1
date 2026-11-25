<#
.SYNOPSIS
    Signs all LaunchMe OS executables, assemblies, and installers with SHA256 Authenticode and DigiCert timestamping.
.PARAMETER CertThumbprint
    Optional thumbprint of the signing certificate. Defaults to the generated LaunchMe certificate.
#>
param(
    [string]$CertThumbprint = "54D9D9EEDA71249E438486DB1AA1FE54B34EF24D"
)

$ErrorActionPreference = "Stop"

Write-Host "=== LaunchMe OS Authenticode Code Signing Pipeline ===" -ForegroundColor Cyan

# Locate certificate
$cert = Get-Item "Cert:\CurrentUser\My\$CertThumbprint" -ErrorAction SilentlyContinue
if (-not $cert) {
    $cert = (Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | Where-Object { $_.Subject -like "*LaunchMe*" } | Select-Object -First 1)
}

if (-not $cert) {
    Write-Error "Code signing certificate not found. Run dist/certificates/install_cert.ps1 first."
}

Write-Host "Using Certificate: $($cert.Subject)" -ForegroundColor Green
Write-Host "Thumbprint:        $($cert.Thumbprint)" -ForegroundColor DarkGray
Write-Host "Timestamp Server:  http://timestamp.digicert.com" -ForegroundColor DarkGray

$targets = @(
    "dist\LaunchMe-Win-x64\LaunchMe.UI.exe",
    "dist\LaunchMe-Win-x64\LaunchMe.Contracts.dll",
    "dist\LaunchMe-Win-x64\LaunchMe.Core.dll",
    "dist\LaunchMe-Win-x64\LaunchMe.Platform.Win32.dll",
    "dist\LaunchMe-Win-x64\LaunchMe.UI.dll",
    "dist\installer\LaunchMe-Win-Setup-v1.0.0.exe"
)

$successCount = 0
foreach ($target in $targets) {
    if (Test-Path $target) {
        Write-Host "Signing $target..." -NoNewline
        $sig = Set-AuthenticodeSignature -FilePath $target -Certificate $cert -HashAlgorithm SHA256 -TimestampServer "http://timestamp.digicert.com"
        if ($sig.SignerCertificate) {
            Write-Host " [OK]" -ForegroundColor Green
            Write-Host "   -> Signer: $($sig.SignerCertificate.Subject)" -ForegroundColor Gray
            Write-Host "   -> Timestamp: $($sig.TimeStamperCertificate.Subject)" -ForegroundColor Gray
            $successCount++
        } else {
            Write-Host " [FAILED]" -ForegroundColor Red
            Write-Host "   -> $($sig.StatusMessage)" -ForegroundColor Yellow
        }
    } else {
        Write-Warning "Skipped (not found): $target"
    }
}

Write-Host "`nSuccessfully signed and timestamped $successCount / $($targets.Count) artifacts." -ForegroundColor Cyan
