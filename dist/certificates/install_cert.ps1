<#
.SYNOPSIS
    Installs the LaunchMe OS development/release public code-signing certificate into the local CurrentUser store.
#>
$certPath = Join-Path $PSScriptRoot "LaunchMe_CodeSigning.cer"
if (-not (Test-Path $certPath)) {
    Write-Error "Certificate file not found at $certPath"
}

Write-Host "Importing LaunchMe OS Code Signing Certificate..." -ForegroundColor Cyan
$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($certPath)

# Import into TrustedPublisher
$store = New-Object System.Security.Cryptography.X509Certificates.X509Store([System.Security.Cryptography.X509Certificates.StoreName]::TrustedPublisher, [System.Security.Cryptography.X509Certificates.StoreLocation]::CurrentUser)
$store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
$store.Add($cert)
$store.Close()

Write-Host "Successfully trusted certificate in CurrentUser\TrustedPublisher store!" -ForegroundColor Green
Write-Host "Thumbprint: $($cert.Thumbprint)" -ForegroundColor DarkGray
