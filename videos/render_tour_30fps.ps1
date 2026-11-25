$ErrorActionPreference = "Stop"

$baseDir = "C:\Users\Redwan Ahmmed\.gemini\antigravity\brain\89c20678-0592-4161-88c5-7b793ab68dac"
$videoDir = "$baseDir\videos"
$tempDir = "$videoDir\temp"
$screenshotsDir = "$baseDir\screenshots"
$hyprReadme = "E:\LaunchMe OS\_analysis\HyprMac\docs\readme"
$coucouMedia = "E:\LaunchMe OS\_analysis\coucou\docs\media"

Write-Host "Rendering 30 FPS LaunchMe OS Video Tour Segments..." -ForegroundColor Cyan

# Common filter for 1280x720 30fps
$vf = "scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2:color=black,format=yuv420p"

# 1. Intro Card (Color filter + text or image)
$introImg = "$screenshotsDir\hero.webp"
ffmpeg -y -i "$introImg" -vf "$vf,fade=t=in:st=0:d=0.5" -r 30 -t 3 -c:v libx264 -preset fast "$tempDir\seg0_intro.mp4"

# 2. Surface 1: Launcher Grid & Unified Search (hero.webp)
ffmpeg -y -i "$introImg" -vf "$vf" -r 30 -ss 3 -t 6 -c:v libx264 -preset fast "$tempDir\seg1_launcher.mp4"

# 3. Surface 2: Dynamic Notch & Mochi Cockpit (demo.mp4)
$demoMp4 = "$coucouMedia\demo.mp4"
ffmpeg -y -i "$demoMp4" -vf "$vf" -an -r 30 -ss 5 -t 10 -c:v libx264 -preset fast "$tempDir\seg2_notch.mp4"

# 4. Surface 2: AI Agent Approval Card & Diff Reviewer (approval.png)
$approvalImg = "$screenshotsDir\approval.png"
ffmpeg -y -loop 1 -t 6 -i "$approvalImg" -vf "$vf" -r 30 -c:v libx264 -preset fast "$tempDir\seg3_approval.mp4"

# 5. Surface 3: Multi-Display Dock Spaces (workspaces.webp)
$workspacesWebp = "$hyprReadme\workspaces.webp"
ffmpeg -y -i "$workspacesWebp" -vf "$vf" -r 30 -t 8 -c:v libx264 -preset fast "$tempDir\seg4_dock.mp4"

# 6. Surface 5: HyprWin Dynamic Tiling Engine (tiling.webp)
$tilingWebp = "$hyprReadme\tiling.webp"
ffmpeg -y -i "$tilingWebp" -vf "$vf" -r 30 -t 8 -c:v libx264 -preset fast "$tempDir\seg5_tiling.mp4"

# 7. Surface 5: Floating Scratchpad Overlay (scratchpad.webp)
$scratchWebp = "$hyprReadme\scratchpad.webp"
ffmpeg -y -i "$scratchWebp" -vf "$vf" -r 30 -t 6 -c:v libx264 -preset fast "$tempDir\seg6_scratchpad.mp4"

# 8. Surface 6: Encrypted Phone Link Relay (settings.png)
$settingsImg = "$screenshotsDir\settings.png"
ffmpeg -y -loop 1 -t 6 -i "$settingsImg" -vf "$vf" -r 30 -c:v libx264 -preset fast "$tempDir\seg7_phonelink.mp4"

# 9. Outro Card (hero.webp with fade out)
ffmpeg -y -i "$introImg" -vf "$vf,fade=t=out:st=2:d=1" -r 30 -t 3 -c:v libx264 -preset fast "$tempDir\seg8_outro.mp4"

Write-Host "All segments rendered. Concatenating into final 30fps MP4..." -ForegroundColor Green

# Concat list
$concatList = "$tempDir\concat_list.txt"
$segments = @(
    "seg0_intro.mp4",
    "seg1_launcher.mp4",
    "seg2_notch.mp4",
    "seg3_approval.mp4",
    "seg4_dock.mp4",
    "seg5_tiling.mp4",
    "seg6_scratchpad.mp4",
    "seg7_phonelink.mp4",
    "seg8_outro.mp4"
)

$fileLines = $segments | ForEach-Object { "file '$tempDir\$_'" }
$fileLines | Out-File -FilePath $concatList -Encoding ascii

$finalMp4 = "$videoDir\launchme_os_full_tour_30fps.mp4"
ffmpeg -y -f concat -safe 0 -i "$concatList" -c copy "$finalMp4"

# Also create WebM for broad browser preview
$finalWebm = "$videoDir\launchme_os_full_tour_30fps.webm"
ffmpeg -y -i "$finalMp4" -c:v libvpx-vp9 -b:v 1500k -r 30 "$finalWebm"

Write-Host "Master 30 FPS video tour generated successfully!" -ForegroundColor Cyan
Write-Host "MP4:  $finalMp4"
Write-Host "WebM: $finalWebm"
