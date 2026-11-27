$targets = @(
    "dist\installer\LaunchMe-Win-Setup-v1.0.0.exe",
    "dist\LaunchMe-Win-v1.0.0-Portable-x64.zip",
    "dist\LaunchMe-PhoneLink-Companion-Android.apk",
    "dist\LaunchMe-PhoneLink-Companion-Android.zip",
    "dist\certificates\LaunchMe_CodeSigning.cer"
)

$results = @()
foreach ($item in $targets) {
    if (Test-Path $item) {
        $hash = (Get-FileHash -Path $item -Algorithm SHA256).Hash
        $name = Split-Path $item -Leaf
        $results += "$hash  $name"
    }
}

$results | Out-File -FilePath "dist\SHA256SUMS.txt" -Encoding utf8
Get-Content "dist\SHA256SUMS.txt"
