<#
.SYNOPSIS
Builds the Stream Deck plugin into dist\com.cgero.streamteamdeck.sdPlugin.

.DESCRIPTION
Publishes the plugin executable and assembles it with the manifest and images into a
ready-to-install .sdPlugin folder. With -Install, also stops Stream Deck, copies the
plugin into its plugins folder, and restarts it.
#>
param([switch]$Install)

$ErrorActionPreference = "Stop"
$pluginId = "com.cgero.streamteamdeck.sdPlugin"
$dist = Join-Path $PSScriptRoot "dist\$pluginId"

dotnet publish (Join-Path $PSScriptRoot "StreamTeamDeck.Plugin\StreamTeamDeck.Plugin.csproj") -c Release -o $dist
Copy-Item -Recurse -Force (Join-Path $PSScriptRoot "StreamTeamDeck.Plugin\$pluginId\*") $dist

if ($Install) {
    $target = Join-Path $env:APPDATA "Elgato\StreamDeck\Plugins\$pluginId"
    Get-Process StreamDeck -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 1
    Remove-Item -Recurse -Force $target -ErrorAction SilentlyContinue
    Copy-Item -Recurse -Force $dist $target
    Start-Process (Join-Path $env:ProgramFiles "Elgato\StreamDeck\StreamDeck.exe")
    Write-Host "Installed to $target and restarted Stream Deck."
} else {
    Write-Host "Plugin built at $dist. Re-run with -Install to copy it into Stream Deck."
}
