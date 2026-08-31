<#
.SYNOPSIS
Builds the Stream Deck plugin into dist/com.cgero.streamteamdeck.sdPlugin.

.DESCRIPTION
Publishes the plugin executable and assembles it with the manifest and images into a
ready-to-install .sdPlugin folder. With -Install, also stops Stream Deck, copies the
plugin into its plugins folder, and restarts it.
#>
param([switch]$Install)

$ErrorActionPreference = "Stop"
$pluginId = "com.cgero.streamteamdeck.sdPlugin"
$dist = Join-Path $PSScriptRoot "dist" | Join-Path -ChildPath $pluginId
$pluginProject = Join-Path $PSScriptRoot "StreamTeamDeck.Plugin"

Remove-Item -Recurse -Force $dist -ErrorAction SilentlyContinue
dotnet publish (Join-Path $pluginProject "StreamTeamDeck.Plugin.csproj") -c Release -o $dist -p:EnableWindowsTargeting=true
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed (exit code $LASTEXITCODE)."
}

$assetSource = Join-Path $pluginProject $pluginId
Copy-Item -Force (Join-Path $assetSource "manifest.json") $dist
Copy-Item -Recurse -Force (Join-Path $assetSource "images") (Join-Path $dist "images")

if ($Install) {
    $target = Join-Path $env:APPDATA "Elgato" | Join-Path -ChildPath "StreamDeck" |
        Join-Path -ChildPath "Plugins" | Join-Path -ChildPath $pluginId
    Get-Process StreamDeck -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 1
    Remove-Item -Recurse -Force $target -ErrorAction SilentlyContinue
    Copy-Item -Recurse -Force $dist $target
    Write-Host "Installed to $target."
    $streamDeckExe = Join-Path $env:ProgramFiles "Elgato" |
        Join-Path -ChildPath "StreamDeck" | Join-Path -ChildPath "StreamDeck.exe"
    if (Test-Path $streamDeckExe) {
        Start-Process $streamDeckExe
        Write-Host "Restarted Stream Deck."
    } else {
        Write-Host "Stream Deck app not found at $streamDeckExe - start it manually."
    }
} else {
    Write-Host "Plugin built at $dist. Re-run with -Install to copy it into Stream Deck."
}
