# generate_resw.ps1 — Scaffolds 18 culture-specific Resources.resw files
# Usage: .\generate_resw.ps1  (run from the LaunchMe.UI project root)
# Translations are intentionally left as English placeholders; replace values per culture.

$cultures = @(
    "en-US", "fr-FR", "de-DE", "es-ES", "it-IT",
    "pt-BR", "ja-JP", "zh-CN", "zh-TW", "ko-KR",
    "ar-SA", "ru-RU", "hi-IN", "nl-NL", "pl-PL",
    "tr-TR", "sv-SE", "da-DK"
)

$baseDir = Join-Path $PSScriptRoot "Strings"

$baseStrings = [ordered]@{
    "AppName"               = "LaunchMe"
    "LauncherTitle"         = "Launcher"
    "DockTitle"             = "Dock"
    "MusicPlayerTitle"      = "Player"
    "SearchPlaceholder"     = "Search apps, files, and more…"
    "WidgetClock"           = "Clock"
    "WidgetWeather"         = "Weather"
    "WidgetCalendar"        = "Calendar"
    "WidgetMusic"           = "Music"
    "WidgetPomodoro"        = "Focus Timer"
    "WidgetPhoto"           = "Photo"
    "WidgetAffirmation"     = "Affirmation"
    "WidgetSystemStats"     = "System Stats"
    "WidgetPixelArt"        = "Pixel Art"
    "WidgetShapePhoto"      = "Shape Photo"
    "WidgetGif"             = "GIF"
    "SpaceDefault"          = "Default Space"
    "SpaceNewName"          = "New Space"
    "WorkflowTitle"         = "Workflows"
    "WorkflowNew"           = "New Workflow"
    "WorkflowRun"           = "Run"
    "AgentApproveTitle"     = "Agent Request"
    "AgentApprove"          = "Allow"
    "AgentDeny"             = "Deny"
    "SettingsTitle"         = "Settings"
    "SettingsGeneral"       = "General"
    "SettingsAppearance"    = "Appearance"
    "SettingsNotifications" = "Notifications"
    "PhoneLinkTitle"        = "Phone Link"
    "PhoneLinkPair"         = "Pair New Device"
    "PhoneLinkPairPin"      = "PIN: {0}"
    "HyprWinTitle"          = "HyprWin"
    "HyprWinLayout"         = "Layout"
    "CloseButton"           = "Close"
    "OkButton"              = "OK"
    "CancelButton"          = "Cancel"
}

foreach ($culture in $cultures) {
    $dir = Join-Path $baseDir $culture
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    $path = Join-Path $dir "Resources.resw"

    $xml = [System.Xml.XmlDocument]::new()
    $xml.AppendChild($xml.CreateXmlDeclaration("1.0", "utf-8", $null)) | Out-Null
    $root = $xml.AppendChild($xml.CreateElement("root"))

    # Schema comment
    $root.AppendChild($xml.CreateComment(" Microsoft ResX Schema — this file is auto-generated. Translate values only. ")) | Out-Null

    foreach ($key in $baseStrings.Keys) {
        $data = $root.AppendChild($xml.CreateElement("data"))
        $data.SetAttribute("name", $key)
        $data.SetAttribute("xml:space", "preserve")
        $value = $data.AppendChild($xml.CreateElement("value"))
        $value.InnerText = $baseStrings[$key]
    }

    $xml.Save($path)
    Write-Host "  Generated: $path"
}

Write-Host "`nDone — $($cultures.Count) cultures scaffolded in $baseDir"
