# Removes the KOCH Send to PrusaSlicer add-on (KOCH Engineering).
# Your settings (%APPDATA%\Koch Engineering\PrusaSlicer) are kept.
$ErrorActionPreference = 'Stop'
$AddOnId    = '{CD41C50A-D5D1-4801-B9A1-785DEF849D54}'
$InstallDir = Join-Path $env:ProgramData 'Alibre AddOns\KochPrusaSlicer'

if (Get-Process -Name 'Alibre Design' -ErrorAction SilentlyContinue) { throw 'Close Alibre Design first.' }

$key = 'HKLM:\SOFTWARE\Alibre Design Add-Ons'
if (Get-ItemProperty -Path $key -Name $AddOnId -ErrorAction SilentlyContinue) {
    Remove-ItemProperty -Path $key -Name $AddOnId
    Write-Host "Unregistered $AddOnId"
}
if (Test-Path $InstallDir) {
    Remove-Item -Recurse -Force $InstallDir
    Write-Host "Removed $InstallDir"
}

Write-Host 'Done. Settings in %APPDATA%\Koch Engineering\PrusaSlicer were kept.'
